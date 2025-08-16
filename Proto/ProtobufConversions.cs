// ProtobufExtensions.cs
using System.Numerics; // Para System.Numerics.Vector3 e Quaternion


namespace ConsoleApp1.Utils // Ou o namespace que você preferir para utilitários
{
    public static class ProtobufConversions
    {
        // Conversão de System.Numerics.Vector3 para Projeto.Vector3Proto
        public static Vector3Proto ToProto(this Vector3 vector)
        {
            return new Vector3Proto { X = vector.X, Y = vector.Y, Z = vector.Z };
        }

        // Conversão de Projeto.Vector3Proto para System.Numerics.Vector3
        public static Vector3 ToNumerics(this Vector3Proto protoVector)
        {
            return new Vector3(protoVector.X, protoVector.Y, protoVector.Z);
        }

        // Conversão de System.Numerics.Quaternion para Projeto.QuaternionProto
        public static QuaternionProto ToProto(this Quaternion quaternion)
        {
            return new QuaternionProto { X = quaternion.X, Y = quaternion.Y, Z = quaternion.Z, W = quaternion.W };
        }

        // Conversão de Projeto.QuaternionProto para System.Numerics.Quaternion
        public static Quaternion ToNumerics(this QuaternionProto protoQuaternion)
        {
            return new Quaternion(protoQuaternion.X, protoQuaternion.Y, protoQuaternion.Z, protoQuaternion.W);
        }
    }
}