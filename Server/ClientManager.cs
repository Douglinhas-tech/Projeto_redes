using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Projeto.Server
{
    public class ClientManager
    {
        private readonly ConcurrentDictionary<IPEndPoint, ClientInfo> _connectedClients = new ConcurrentDictionary<IPEndPoint, ClientInfo>();
        public event Action<IPEndPoint, bool> ClientDisconnected;

        private Thread _inactiveClientsCheckThread;
        private readonly TimeSpan _clientTimeout;
        private volatile bool _isRunningMonitoring;
        public int ActiveClientCount = 0;

        private readonly UdpServerHandler _udpServerHandler;

        public ClientManager(TimeSpan clientTimeout, UdpServerHandler udp)
        {
            _clientTimeout = clientTimeout;
            _isRunningMonitoring = false; // Inicializa a flag de monitoramento como false.
            _udpServerHandler = udp;
        }

        public ClientInfo GetClientInfo(IPEndPoint endPoint)
        {
            _connectedClients.TryGetValue(endPoint, out var clientInfo);
            return clientInfo; // Retorna null se não encontrar
        }

        public void StartMonitoring()
        {
            // Evita iniciar múltiplas threads de monitoramento.
            if (_isRunningMonitoring) return;

            _isRunningMonitoring = true;
            _inactiveClientsCheckThread = new Thread(InactiveClientsCheckLoop);
            // Define a thread como background para que ela não impeça o processo de terminar.
            _inactiveClientsCheckThread.IsBackground = true;
            _inactiveClientsCheckThread.Start();
            Console.WriteLine("ClientManager: Monitoramento de clientes inativos iniciado.");
        }

        private void InactiveClientsCheckLoop()
        {
            while (_isRunningMonitoring) // Continua rodando enquanto a flag _isRunningMonitoring for verdadeira.
            {
                try
                {
                    // Obtém uma lista de clientes que excederam o tempo limite.
                    var clientsToRemove = GetInactiveClients();
                    foreach (var endPoint in clientsToRemove)
                    {
                        // Remove cada cliente inativo, indicando que foi por timeout.
                        RemoveClient(endPoint, true);
                    }
                    //Console.WriteLine($"ClientManager: {ActiveClientCount} clientes ativos."); // Opcional: logar a contagem de clientes

                    // Pausa a thread por 500ms antes da próxima verificação.
                    Thread.Sleep(500);
                }
                catch (ThreadInterruptedException)
                {
                    Console.WriteLine("ClientManager: Thread de verificação de clientes inativos interrompida.");
                    break; // Sai do loop se a thread for interrompida.
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ClientManager: Erro na thread de verificação de clientes inativos: {ex.Message}");
                    Thread.Sleep(1000); // Espera um pouco antes de tentar novamente em caso de erro.
                }
            }
        }

        public IEnumerable<IPEndPoint> GetAllClientEndPoints()
        {
            // Retorna as chaves da ConcurrentDictionary, que é thread-safe para iteração.
            // Não é necessário .ToList() aqui, a menos que uma cópia estática seja estritamente exigida pelo chamador.
            return _connectedClients.Keys;
        }

        public IEnumerable<IPEndPoint> GetInactiveClients()
        {
            var now = DateTime.UtcNow;
            // Filtra e seleciona clientes inativos. Materializa a lista para evitar problemas de concorrência.
            return _connectedClients.Where(c => now - c.Value.LastContact > _clientTimeout)
                                     .Select(c => c.Key)
                                     .ToList();
        }

        public void AddClient(IPEndPoint endPoint)
        {
            var newClient = new ClientInfo(endPoint);
            // Tenta adicionar o cliente. Se já existir, atualiza o LastContact.
            if (_connectedClients.TryAdd(endPoint, newClient))
            {
                Console.WriteLine($"ClientManager: Novo cliente adicionado: {endPoint}.");
                ActiveClientCount++;
            }
            else
            {
                // Se o cliente já existia, apenas atualiza o LastContact
                if (_connectedClients.TryGetValue(endPoint, out var existingClientInfo))
                {
                    existingClientInfo.LastContact = DateTime.UtcNow;
                }
            }
        }

        public void UpdateClientLastContact(IPEndPoint endPoint)
        {
            if (_connectedClients.TryGetValue(endPoint, out var clientInfo))
            {
                clientInfo.LastContact = DateTime.UtcNow;
            }
        } 

        public void RemoveClient(IPEndPoint endPoint, bool isTimeout=false)
        {
            // Tenta remover o cliente.
            if (_connectedClients.TryRemove(endPoint, out ClientInfo clientInfo))
            {
                string reason = isTimeout ? "timeout" : "desconexão explícita";
                Console.WriteLine($"ClientManager: Cliente {endPoint} (PlayerID: {clientInfo.Player.LogicalId ?? "N/A"}) removido por {reason}."); //
                ClientDisconnected?.Invoke(endPoint, isTimeout);
                ActiveClientCount--;
            }
        }

        public void Broadcast(byte[] data)
        {
            foreach (var clientEndPoint in GetAllClientEndPoints())
            {
                _udpServerHandler.SendPacketAsync(clientEndPoint, data);
            }
        }

        public void Broadcast(byte[] data, IEnumerable<IPEndPoint> targetClients)
        {
            foreach (var clientEndPoint in targetClients)
            {
                _udpServerHandler.SendPacketAsync(clientEndPoint, data);
            }
        }
    }
}
