using JoltPhysicsSharp;
using System.Net;
using System.Numerics;

namespace Projeto.Game
{
   /* public class PlayerController
    {
        private readonly PlayerGameObject _playerGameObject;
        private readonly BodyInterface _bodyInterface; // Referência à BodyInterface do Jolt
        private readonly float _moveSpeed = 5.0f; // Velocidade de movimento do jogador
        private readonly float _jumpForce = 7.0f; // Força do pulo

        // Estado de entrada do jogador
        private bool _moveForward;
        private bool _moveBackward;
        private bool _moveLeft;
        private bool _moveRight;
        private bool _jump;
        private Vector2 _lookDirection; // Para rotação da câmera/personagem

        public PlayerController(PlayerGameObject playerGameObject, BodyInterface bodyInterface)
        {
            _playerGameObject = playerGameObject;
            _bodyInterface = bodyInterface;
        }

        // Método para atualizar o estado de entrada com base nos pacotes do cliente
        public void UpdateInput(InputPacket input) // Você precisará definir um 'InputPacket' no seu arquivo .proto
        {
            _moveForward = input.MoveForward;
            _moveBackward = input.MoveBackward;
            _moveLeft = input.MoveLeft;
            _moveRight = input.MoveRight;
            _jump = input.Jump;
            _lookDirection = new Vector2(input.LookX, input.LookY);
        }

        // Método principal para aplicar o movimento baseado na entrada
        public void FixedUpdate(float deltaTime)
        {
            // Certifique-se de que o corpo do jogador está ativo e pode ser manipulado
            if (!_bodyInterface.IsActive(_playerGameObject.PhysicsBodyID))
            {
                _bodyInterface.ActivateBody(_playerGameObject.PhysicsBodyID);
            }

            Vector3 currentVelocity = _bodyInterface.GetLinearVelocity(_playerGameObject.PhysicsBodyID);
            Vector3 desiredVelocity = new Vector3(0, currentVelocity.Y, 0); // Mantém a velocidade Y para gravidade/pulo

            // Lógica de movimento no plano XZ
            Vector3 forward = _bodyInterface.GetRotation(_playerGameObject.PhysicsBodyID) * Vector3.UnitZ;
            Vector3 right = _bodyInterface.GetRotation(_playerGameObject.PhysicsBodyID) * Vector3.UnitX;

            if (_moveForward)
            {
                desiredVelocity += forward * _moveSpeed;
            }
            if (_moveBackward)
            {
                desiredVelocity -= forward * _moveSpeed;
            }
            if (_moveLeft)
            {
                desiredVelocity -= right * _moveSpeed;
            }
            if (_moveRight)
            {
                desiredVelocity += right * _moveSpeed;
            }

            // Pulo
            // Você precisaria de uma lógica para verificar se o jogador está no chão antes de permitir o pulo.
            // Isso geralmente envolve raycasts ou detecção de contato.
            // Por simplicidade, vamos permitir o pulo sem verificação de chão por enquanto.
            if (_jump)
            {
                desiredVelocity.Y = _jumpForce;
                _jump = false; // Reseta o pulo para que não continue pulando
            }

            _bodyInterface.SetLinearVelocity(_playerGameObject.PhysicsBodyID, desiredVelocity);

            // Rotação (exemplo básico, você pode querer rotação mais suave ou baseada na câmera)
            // Se _lookDirection vem de um mouse delta, você precisará acumular e converter.
            // Para rotação simples do corpo no Y-axis (olhando para os lados):
            if (_lookDirection.X != 0)
            {
                float yawDelta = _lookDirection.X * deltaTime * 2.0f; // Ajuste o multiplicador conforme a sensibilidade
                Quaternion currentRotation = _bodyInterface.GetRotation(_playerGameObject.PhysicsBodyID);
                Quaternion yawRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yawDelta);
                _bodyInterface.SetRotation(_playerGameObject.PhysicsBodyID, currentRotation * yawRotation, Activation.Activate);
            }
            // Resetar _lookDirection se for um delta, ou manter se for um valor absoluto.
        }
    }*/
}