using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Projeto.Server
{
    public class UdpServerHandler
    {
        private UdpClient _udpClient;
        private int _port;
        private volatile bool _listening;

        public event Func<IPEndPoint, byte[], Task> PacketReceived;

        public UdpServerHandler(int port)
        {
            _port = port;
        }

        public void StartListening()
        {
            _udpClient?.Dispose();
            _udpClient = null;

            try
            {
                _udpClient = new UdpClient(_port);
                Console.WriteLine($"Iniciando UdpServerHandler na porta: {_port}");
                _listening = true;

                ReceiveLoopAsync();
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Erro ao iniciar UdpClient na porta {_port}: {ex.Message}");
                _listening = false;
            }
        }

        private async void ReceiveLoopAsync()
        {
            while (_listening)
            {
                try
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync();

                    if (!_listening)
                    {
                        Console.WriteLine("UdpClient foi descartado enquanto aguardava um pacote, ReceiveLoopAsync parando.");
                        break;
                    }

                    _ = PacketReceived?.Invoke(result.RemoteEndPoint, result.Buffer);
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("UdpClient foi descartado, ReceiveLoopAsync parando.");
                    _listening = false;
                    break;
                }
                catch (SocketException ex)
                {
                    switch (ex.SocketErrorCode)
                    {
                        case SocketError.Interrupted:
                        case SocketError.OperationAborted:
                        case SocketError.NotSocket:
                        case SocketError.Fault:
                            Console.WriteLine($"Loop de recebimento do servidor interrompido devido a erro fatal do Socket ({ex.SocketErrorCode}): {ex.Message}");
                            _listening = false;
                            break;

                        case SocketError.ConnectionReset:
                            break;

                        default:
                            Console.WriteLine($"Erro inesperado de Socket em ReceiveLoopAsync ({ex.SocketErrorCode}): {ex.Message}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro inesperado em ReceiveLoopAsync: {ex.Message}");
                }
            }
            Console.WriteLine("ReceiveLoopAsync terminou.");
        }

        public async Task SendPacketAsync(IPEndPoint endPoint, byte[] data)
        {
            if (_udpClient == null || !_listening)
            {
                return;
            }

            try
            {
                await _udpClient.SendAsync(data, data.Length, endPoint);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Erro ao enviar pacote para {endPoint} (SocketException: {ex.SocketErrorCode}): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado ao enviar pacote para {endPoint}: {ex.Message}");
            }
        }
    }
}
