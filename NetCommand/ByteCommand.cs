using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Eric.Comminucate
{
    public class ByteCommand
    {
        private NetCommand netCommand;
        private byte[] objectBytes;
        private BinaryFormatter formatter = new BinaryFormatter(); 
        public int Length { get { return BitConverter.ToInt32(objectBytes, 0); } }
        public byte[] Bytes { get { return objectBytes; } set { objectBytes = value; } }
        public NetCommand NetCommand { get { return netCommand; } set { netCommand = value; } } 

        public ByteCommand(byte[] b)
        {
            objectBytes = b;
            netCommand = BytesToCommand(b);
        }
        public ByteCommand(NetCommand command)
        {
            netCommand = command;
            objectBytes = PacketBytes(CommandToBytes(command));
        }
        private byte[] CommandToBytes(NetCommand command)
        {
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, command);
            byte[] b = stream.ToArray();
            return b;
        }
        private NetCommand BytesToCommand(byte[] b)
        {
            Stream stream = new MemoryStream(b);
            NetCommand command = formatter.Deserialize(stream) as NetCommand;
            return command;
        }

        private byte[] UnPacketBytes(byte[] b)
        {
            byte[] lenBytes = new byte[4];
            byte[] buffer = new byte[b.Length - 4];
            System.Buffer.BlockCopy(b, 0, lenBytes, 0, 4);
            System.Buffer.BlockCopy(b, 4, buffer, 0, buffer.Length);
            return buffer;
        } 
        private byte[] PacketBytes(byte[] b)
        {
            byte[] lenBytes = BitConverter.GetBytes(b.Length);
            byte[] buffer = new byte[lenBytes.Length + b.Length];
            System.Buffer.BlockCopy(lenBytes, 0, buffer, 0, lenBytes.Length);
            System.Buffer.BlockCopy(b, 0, buffer, lenBytes.Length, b.Length);
            return buffer;
        }

    }
}
