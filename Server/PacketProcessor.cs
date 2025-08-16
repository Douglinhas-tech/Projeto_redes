using Google.Protobuf;
using Projeto.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Projeto.Server
{
    public class PacketProcessor
    {

        protected GameManager _gameManager;
        protected UdpServerHandler _udpServerHandler;
        protected ClientManager _clientManager;
        protected String _clientVersion = "1.0";

        public PacketProcessor(GameManager gameManager, UdpServerHandler udpServerHandler, ClientManager clientManager) 
        {
            _gameManager = gameManager;
            _udpServerHandler = udpServerHandler;
            _clientManager = clientManager;
        }

        public async Task ProcessPacketAsync(IPEndPoint remoteEndPoint, byte[] receivedData)
        {
            if (receivedData == null || receivedData.Length == 0)
            {
                Console.WriteLine($"AVISO: Pacote vazio ou nulo recebido de {remoteEndPoint}.");
                return;
            }
            Packet packet = Packet.Parser.ParseFrom(receivedData);
            _clientManager.UpdateClientLastContact(remoteEndPoint);

            switch (packet.Type)
            {
                case PacketType.Handshake:
                    if (packet.HandshakePayload == null || packet.HandshakePayload.ClientVersion != _clientVersion)
                    {
                        Console.WriteLine($"Handshake falhou de {remoteEndPoint}: Versão incompatível ou payload nulo. Cliente: {packet.HandshakePayload?.ClientVersion ?? "N/A"}, Servidor: {_clientVersion}");
                        return;
                    }

                    ClientInfo clientInfo = _clientManager.GetClientInfo(remoteEndPoint);
                    if (clientInfo == null)
                    {
                        _clientManager.AddClient(remoteEndPoint);
                        clientInfo = _clientManager.GetClientInfo(remoteEndPoint); // Re-obtem ClientInfo para garantir que 'Player' pode ser setado.
                        if (clientInfo == null)
                        {
                            Console.WriteLine($"ERRO: Falha ao obter ClientInfo para {remoteEndPoint} após adição. Handshake interrompido.");
                            return;
                        }
                    }

                    if (clientInfo.Player == null)
                    {
                        // A criação do jogador pode falhar, então verificamos o retorno.
                        PlayerGameObject player = _gameManager.AddPlayer(remoteEndPoint);
                        if (player != null)
                        {
                            clientInfo.Player = player;
                            await SendHandshakeAck(remoteEndPoint, clientInfo.Player);
                            Console.WriteLine($"Handshake bem-sucedido com {remoteEndPoint}. Player ID: {clientInfo.Player.LogicalId}");
                        }
                        else
                        {
                            Console.WriteLine($"ERRO: GameManager falhou ao adicionar jogador para {remoteEndPoint}. Handshake interrompido.");
                            // Opcional: Remover o cliente adicionado se a criação do jogador falhou.
                            _clientManager.RemoveClient(remoteEndPoint, false);
                        }
                    }
                    else
                    {
                        // Cliente já conectado e com PlayerGameObject associado (ex: re-handshake)
                        Console.WriteLine($"AVISO: Cliente {remoteEndPoint} já conectado. Re-handshake. Player ID: {clientInfo.Player.LogicalId}");
                        await SendHandshakeAck(remoteEndPoint, clientInfo.Player);
                    }
                    break;
                case PacketType.Heartbeat:
                    // fazer callback
                    break;
                default:
                    Console.WriteLine($"AVISO: Tipo de pacote desconhecido: {packet.Type} de {remoteEndPoint}.");
                    break;
            }
        }
        private async Task SendHandshakeAck(IPEndPoint endPoint, PlayerGameObject player)
        {
            if (player == null)
            {
                Console.WriteLine($"ERRO: Tentativa de enviar HandshakeAck para {endPoint} com PlayerGameObject nulo.");
                return;
            }

            try
            {
                var ackPacket = new Packet
                {
                    Type = PacketType.HandshakeAck,
                    HandshakeAckPayload = new HandshakeAckPacket
                    {
                        ConnectionSuccessful = true,
                        PlayerId = player.LogicalId,
                        // Certifique-se de que BodyID.ID é uma ulong, então ToString() é seguro aqui.
                        PlayerObj = player.PhysicsBodyID.ID.ToString()
                    }
                };
                await _udpServerHandler.SendPacketAsync(endPoint, ackPacket.ToByteArray());
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"PacketProcessor: ERRO ao enviar HandshakeAck para {endPoint}:\n{ex.ToString()}");
                Console.ResetColor();
            }
        }
    }
}
