using ConsoleApp1.Utils;
using JoltPhysicsSharp;
using Projeto.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Projeto.Jolt
{
    public class Simulation : Jolt
    {
        public int CurrentSimulationStep = 1;
        protected Dictionary<string, Body> _dynamicBodys;
        protected bool _isRunning;

        private Body _floor;
        private Body _sphere;

        public Simulation(GameManager game)
        {
            _dynamicBodys = new Dictionary<string, Body>();
            _isRunning = true;
        }

        public void Initialize()
        {
            base.Initialize();

            System!.Gravity = new Vector3(0.0f, -9.81f, 0.0f);

            _floor = CreateFloor(100, Layers.NonMoving);
            _sphere = CreateSphere(1f, new Vector3(-3f, 2.0f, 0.0f), Quaternion.Identity, MotionType.Dynamic, Layers.Moving);  

            BodyInterface.SetPosition(_sphere.ID, new Vector3(0.0f, 20.0f, 0.0f), Activation.Activate);
            if (ADDbody(_sphere)) { }
        }

        public bool ADDbody(Body body)
        {
            if (_dynamicBodys.TryAdd(body.ID.ToString(), body))
                return true;
            else return false;
        }

        public void RemoveBody(BodyID bodyID)
        {
            if(_dynamicBodys.Remove(bodyID.ToString()))
                BodyInterface.RemoveAndDestroyBody(bodyID);
            else Console.WriteLine($"[Simulation] Body não encontrado");
        }

        public Body PlayerCreate()
        {
            var body = base.PlayerCreate();
            if (ADDbody(body)) 
                Console.WriteLine($"[Simulation] Novo player adicionado a simulação simulação ID:{body.ID.ToString()}");
            else
            {
                Console.WriteLine($"[Simullation] Falha em adicionar player a simulação.");
                return null;
            }
           return body;
        }

        public List<GameObjectsState> GetCurrentGameObjectsState()
        {
            var states = new List<GameObjectsState>();
            foreach (var body in _dynamicBodys.Values)
            {
                states.Add(new GameObjectsState
                {
                    ObjectId = body.ID.ID,
                    Position = ProtobufConversions.ToProto(BodyInterface.GetPosition(body.ID)),
                    Rotation = ProtobufConversions.ToProto(BodyInterface.GetRotation(body.ID)),
                    LinearVelocity = ProtobufConversions.ToProto(BodyInterface.GetLinearVelocity(body.ID)),
                    AngularVelocity = ProtobufConversions.ToProto(BodyInterface.GetAngularVelocity(body.ID)),
                    ObjectType = (uint)BodyInterface.GetUserData(body.ID)
                });
            }
            return states;
        }

        public override async Task UpdateGame()
        {
            // Define o passo de tempo fixo para a atualização do motor de física.
            // Isso é crucial para a estabilidade e determinismo da simulação.
            const float cFixedDeltaTime = 1.0f / 120.0f; // 120 ticks de física por segundo (aprox. 8.33 ms por tick)
            const int cCollisionSteps = 1; // Número de sub-passos de colisão por atualização de física.    

            // Variáveis para controlar o tempo do loop e garantir o Fixed Time Step.
            long lastFrameTicks = Stopwatch.GetTimestamp(); // Marca o tempo do último quadro real processado.
            long accumulatedTimeTicks = 0; // Acumula o tempo real que se passou e precisa ser simulado pela física.

            // Variáveis para calcular e logar o FPS real do loop da simulação.
            long logLastTime = Stopwatch.GetTimestamp();
            int logFrameCount = 0;
            const double logIntervalSeconds = 1.0; // Logar o FPS a cada 1 segundo.

            // Define a duração alvo de cada frame do loop principal em ticks.
            // Isso é o que queremos que cada iteração do loop externo demore para alcançar 120 FPS.
            long targetLoopFrameTicks = (long)(cFixedDeltaTime * Stopwatch.Frequency); // Aproximadamente 8.33 ms em ticks

            try
            {
                System!.OptimizeBroadPhase();

                while (_isRunning)
                {
                    long currentTicks = Stopwatch.GetTimestamp();
                    long elapsedTicks = currentTicks - lastFrameTicks;
                    lastFrameTicks = currentTicks; // Atualiza lastFrameTicks para o início desta iteração.

                    // Adiciona o tempo real que passou ao acumulador de tempo para a física.
                    accumulatedTimeTicks += elapsedTicks;

                    // Lógica para calcular e logar o FPS real do *loop* da simulação.
                    logFrameCount++;
                    if ((currentTicks - logLastTime) * 1000.0 / Stopwatch.Frequency >= logIntervalSeconds * 1000)
                    {
                        double actualLoopFps = (double)logFrameCount * Stopwatch.Frequency / (currentTicks - logLastTime);
                        Console.WriteLine($"FPS Real da Simulação (Loop): {actualLoopFps:F2}");
                        logFrameCount = 0;
                        logLastTime = currentTicks;
                    }

                    // --- Núcleo do Fixed Time Step: Atualiza a física ---
                    // Este loop garante que a simulação avance o tempo correto (cFixedDeltaTime)
                    // para cada passo de física, mesmo que o tempo real do seu loop varie.
                    while (accumulatedTimeTicks >= targetLoopFrameTicks) // Usa targetLoopFrameTicks para a condição
                    {
                        PhysicsUpdateError error = System!.Update(cFixedDeltaTime, cCollisionSteps, JobSystem);
                        global::System.Diagnostics.Debug.Assert(error == PhysicsUpdateError.None);
                            
                       
                        CurrentSimulationStep++; // Incrementa o contador de passos da simulação.
                        accumulatedTimeTicks -= targetLoopFrameTicks; // Deduz o tempo que foi simulado.
                    }

                    // --- Controle de Consumo de CPU: Pausa a thread se houver tempo livre ---
                    // Calcula o tempo que *ainda resta* para completar a duração alvo do frame (8.33 ms).
                    // O `lastFrameTicks` já representa o *início* do frame atual.
                    long timePassedInThisFrame = Stopwatch.GetTimestamp() - (lastFrameTicks - elapsedTicks); // Tempo desde o *início* do frame atual
                    long remainingTicksForThisFrame = targetLoopFrameTicks - timePassedInThisFrame;

                    if (remainingTicksForThisFrame > 0)
                    {
                        // Converte os ticks restantes para milissegundos.
                        long sleepMs = remainingTicksForThisFrame * 1000 / Stopwatch.Frequency;

                        if (sleepMs > 1) // Usamos > 1 para dar uma margem ao Thread.Sleep (que é granular)
                        {
                            Thread.Sleep((int)sleepMs);
                        }
                        else
                        {
                            // Para tempos muito curtos, SpinWait é mais preciso.
                            // Espera até que o tempo atual atinja o tempo alvo do fim do frame.
                            SpinWait.SpinUntil(() => Stopwatch.GetTimestamp() >= (lastFrameTicks - elapsedTicks) + targetLoopFrameTicks);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Tratamento de erros críticos.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERRO CRÍTICO NA THREAD DE SIMULAÇÃO (RunSimulationLoop):");
                Console.WriteLine($"Mensagem: {ex.Message}");
                Console.WriteLine($"Tipo: {ex.GetType().Name}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.ResetColor();

                if (ex.InnerException != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                    Console.WriteLine($"InnerException StackTrace: {ex.InnerException.StackTrace}");
                    Console.ResetColor();
                }
            }
            finally
            {
                // Garante o desligamento limpo do Foundation do JoltPhysicsSharp.
                JoltPhysicsSharp.Foundation.Shutdown();
                Console.WriteLine("Simulação Jolt desligada (loop terminou ou erro ocorreu).");
            }
        }
    }
}
