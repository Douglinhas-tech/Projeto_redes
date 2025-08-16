using Google.Protobuf;
using Projeto.Jolt;
using Projeto.Server;
using System.Diagnostics;

namespace Projeto.Game
{
    public class Syncronyzer
    {
        private Thread _sendThread;
        private readonly float _sendRateHz;
        private readonly long _targetSendMsPerFrame;

        private int _packetsSentThisSecond = 0;
        private long _lastSentLogTimestamp = 0;

        protected ClientManager ClientManager;

        Simulation _currentSimulation;

        public Syncronyzer(Simulation physics, ClientManager clientManager)
        {
            _currentSimulation = physics;
            _sendRateHz = 60.0f;
            _targetSendMsPerFrame = (long)((1.0f / _sendRateHz) * 1000);
            ClientManager = clientManager;
        }


        public async Task SendLoop()
        {
            Stopwatch cycleStopwatch = new Stopwatch();
            _lastSentLogTimestamp = Stopwatch.GetTimestamp();
            while (true)
            {
                cycleStopwatch.Restart();
                try
                {

                    List<GameObjectsState> currentStates = _currentSimulation.GetCurrentGameObjectsState();
                    GameUpdate packetGame = new GameUpdate
                    {
                        ServerTick = (ulong)_currentSimulation.CurrentSimulationStep,
                    };

                    foreach (var state in currentStates)
                    {
                        packetGame.ObjectStates.Add(new GameObjectsState
                        {
                            ObjectId = state.ObjectId,
                            Position = new Vector3Proto { X = state.Position.X, Y = state.Position.Y, Z = state.Position.Z },
                            Rotation = new QuaternionProto { X = state.Rotation.X, Y = state.Rotation.Y, Z = state.Rotation.Z, W = state.Rotation.W, },
                            LinearVelocity = new Vector3Proto { X = state.LinearVelocity.X, Y = state.LinearVelocity.Y, Z = state.LinearVelocity.Z },
                            AngularVelocity = new Vector3Proto { X = state.AngularVelocity.X, Y = state.AngularVelocity.Y, Z = state.AngularVelocity.Z },
                            ObjectType = state.ObjectType
                        });
                    }

                    Packet packet = new Packet
                    {
                        Type = PacketType.Gameupdate,
                        GameupdatePayload = packetGame
                    };

                    byte[] data = packet.ToByteArray();

                    ClientManager.Broadcast(data);
               
                    _packetsSentThisSecond++;

                    long currentTime = Stopwatch.GetTimestamp();

                    double elapsedSeconds = (currentTime - _lastSentLogTimestamp) / (double)Stopwatch.Frequency;


                    if (elapsedSeconds >= 1.0)
                    {
                        Console.WriteLine($"[ServerStats] ENVIADO: {_packetsSentThisSecond} pacotes em {elapsedSeconds:F2}s. Taxa: {_packetsSentThisSecond / elapsedSeconds:F2} packets/sec.");
                        _packetsSentThisSecond = 0;
                        _lastSentLogTimestamp = currentTime;
                    }
                }
                catch (ThreadInterruptedException)
                {
                    Console.WriteLine("GameSynchronizer: Loop de envio interrompido por solicitação.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"GameSynchronizer: ERRO na thread de envio de dados da simulação:\n{ex.ToString()}");
                    Console.ResetColor();
                    Thread.Sleep(100); // Pequena pausa para evitar loop de erro rápido.
                }

                cycleStopwatch.Stop();
                long elapsedTotalMs = cycleStopwatch.ElapsedMilliseconds;
                long sleepTimeMs = _targetSendMsPerFrame - elapsedTotalMs;

                if (sleepTimeMs > 0)
                {
                    long targetTicks = Stopwatch.GetTimestamp() + (long)(sleepTimeMs * Stopwatch.Frequency / 1000.0);
                    while (Stopwatch.GetTimestamp() < targetTicks)
                    {
                        Thread.Yield(); 
                    }
                }
                else
                {
                    Console.WriteLine($"AVISO: GameSynchronizer: Loop de envio atrasado em {Math.Abs(sleepTimeMs):F2} ms! Reduza a carga ou diminua o sendRateHz.");
                }
            }
            Console.WriteLine("GameSynchronizer: Loop de envio encerrado.");
        }
    }
}
