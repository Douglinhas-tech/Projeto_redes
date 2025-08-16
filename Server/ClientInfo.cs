using Projeto.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Projeto.Server
{
    public class ClientInfo
    {
        public IPEndPoint EndPoint { get; }
        public DateTime NextAllowedUpdate { get; set; } = DateTime.UtcNow;
        public DateTime LastContact { get; set; } = DateTime.UtcNow;
        public PlayerGameObject? Player { get; set; } // Referência direta ao objeto do jogador.
                                                      // Use '?' para indicar que pode ser nulo.

        public ClientInfo(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }
    }
}
