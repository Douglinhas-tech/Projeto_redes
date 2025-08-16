using Projeto.Game;
using System.Net;

namespace Projeto.Server
{
    public class Servidor
    {
        ClientManager ClientManager;
        UdpServerHandler UdpServerHandler;
        PacketProcessor PacktProcessor;

        public Servidor(GameManager game)
        {

            UdpServerHandler = new UdpServerHandler(11000);
            ClientManager = new ClientManager(TimeSpan.FromSeconds(3), UdpServerHandler);
            PacktProcessor = new PacketProcessor(game, UdpServerHandler, ClientManager);
            UdpServerHandler.PacketReceived += PacktProcessor.ProcessPacketAsync;
            ClientManager.ClientDisconnected += HandleClientDisconnected;
        }

        public async Task Start()
        {
            UdpServerHandler.StartListening();
            ClientManager.StartMonitoring();
        }

        private void HandleClientDisconnected(IPEndPoint endPoint, bool isTimeout)
        {
            Console.WriteLine($"Servidor: Evento ClientDisconnected capturado para {endPoint}. IsTimeout: {isTimeout}. Clientes ativos: {ClientManager.ActiveClientCount}");
        }

        public ClientManager GetClientManager()
        {
            return ClientManager;
        }
    }
}
