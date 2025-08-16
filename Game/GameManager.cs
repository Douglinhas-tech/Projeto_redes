using JoltPhysicsSharp;
using Projeto.Jolt;
using Projeto.Server;
using System.Collections.Concurrent;
using System.Net;

namespace Projeto.Game
{
    public class GameManager
    {
        protected Simulation _simulation;
        protected Syncronyzer _syncronyzer;
        protected Servidor _server;
        protected ClientManager _clientManager;

        private Thread _SimulationThread;
        private Thread _SyncThread;
        private Thread _ServerThread;

        private readonly ConcurrentDictionary<IPEndPoint, PlayerGameObject> _playersByEndPoint = new ConcurrentDictionary<IPEndPoint, PlayerGameObject>();
        private readonly ConcurrentDictionary<BodyID, PlayerGameObject> _playersByPhysicsBodyId = new ConcurrentDictionary<BodyID, PlayerGameObject>();

        public GameManager()
        {
            Console.WriteLine("[Game Manager] Construtor GameManager iniciado.");

            if (!Foundation.Init(false))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Game Manager] ERRO FATAL: Falha ao iniciar JoltPhysicsSharp. O GameManager não pode continuar. Verifique a instalação do Jolt.");
                Console.ResetColor();
                throw new InvalidOperationException("Falha ao inicializar JoltPhysicsSharp.");
            }
            Console.WriteLine("[Game Manager] JoltPhysicsSharp inicializado com sucesso.");

            _simulation = new Simulation(this);
            _server = new Servidor(this);
            _syncronyzer = new Syncronyzer(_simulation, _server.GetClientManager());

            _clientManager = _server.GetClientManager();
            _clientManager.ClientDisconnected += OnClientDisconnected;


            Console.WriteLine("[Game Manager] Inicializando simulação...");
            _simulation.Initialize();


            _SimulationThread = new Thread(async () => await _simulation.UpdateGame());
            _SimulationThread.IsBackground = true;


            _SyncThread = new Thread(async () => await _syncronyzer.SendLoop());
            _SyncThread.IsBackground = true;


            _ServerThread = new Thread(() => _server.Start());
            _ServerThread.IsBackground = true;

            Start();

            while (true)
            {

            }
        }

        public void Start()
        {
            _SimulationThread.Start();
            _ServerThread.Start();
            _SyncThread.Start();
        }

        public PlayerGameObject AddPlayer(IPEndPoint clientEndPoint)
        {
            try
            {
                string newLogicalId = Guid.NewGuid().ToString();

                if (_simulation == null)
                {
                    Console.WriteLine("[Game Manager] GameSimulationManager não está inicializado ao tentar adicionar jogador.");
                    throw new InvalidOperationException("GameSimulationManager não está disponível.");
                }

                Body playerBody = _simulation.PlayerCreate();

                PlayerGameObject player = new PlayerGameObject(newLogicalId, clientEndPoint, playerBody.ID);

                if (!_playersByEndPoint.TryAdd(clientEndPoint, player))
                {
                    Console.WriteLine($"[Game Manager] Tentativa de adicionar jogador com EndPoint {clientEndPoint} que já existe.");
                    _simulation.RemoveBody(playerBody.ID);
                    return null;
                }
                _playersByPhysicsBodyId.TryAdd(playerBody.ID, player);

                Console.WriteLine($"[Game Manager] Jogador {clientEndPoint} (Lógico: {newLogicalId}, Físico: {playerBody.ID}) adicionado ao jogo.");
                return player;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Game Manager] ERRO ao adicionar jogador para {clientEndPoint}:\n{ex.ToString()}");
                Console.ResetColor();
                return null;
            }
        }

        public ClientManager GetClientManager ()=> _clientManager;
        private void OnClientDisconnected(IPEndPoint endPoint, bool isTimeout)
        {
            try
            {
                if (_playersByEndPoint.TryRemove(endPoint, out PlayerGameObject player))
                {
                    _playersByPhysicsBodyId.TryRemove(player.PhysicsBodyID, out _);

                    if (_simulation != null)
                    {
                        _simulation.RemoveBody(player.PhysicsBodyID);
                        Console.WriteLine($"GameManager: Corpo físico {player.PhysicsBodyID} removido da simulação.");
                    }
                    else
                    {
                        Console.WriteLine($"AVISO: GameSimulationManager não está disponível para remover o corpo físico {player.PhysicsBodyID}.");
                    }
                    Console.WriteLine($"GameManager: Jogador {endPoint} (Lógico: {player.LogicalId}, Físico: {player.PhysicsBodyID}) removido do jogo.");
                }
                else
                {
                    Console.WriteLine($"AVISO: GameManager: Tentativa de remover cliente {endPoint} que não foi encontrado.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"GameManager: ERRO ao remover jogador {endPoint}:\n{ex.ToString()}");
                Console.ResetColor();
            }
        }
    }
}