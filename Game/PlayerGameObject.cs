using JoltPhysicsSharp;
using Projeto.Jolt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Projeto.Game
{
    public class PlayerGameObject
    {
        public string LogicalId { get; }
        public IPEndPoint ClientEndPoint { get; }
        public BodyID PhysicsBodyID { get; } // ID do corpo físico do Jolt
        public string PlayerName { get; set; }
        public int Health { get; set; } = 100; // Exemplo de propriedade de jogo
        public GameObjectType Type => GameObjectType.Player; // Define o tipo para este objeto

        public PlayerGameObject(string logicalId, IPEndPoint clientEndPoint, BodyID physicsBodyID)
        {
            LogicalId = logicalId;
            ClientEndPoint = clientEndPoint;
            PhysicsBodyID = physicsBodyID;
            PlayerName = "Player_" + logicalId.Substring(0, 4); // Nome de exemplo
        }
    }
}
