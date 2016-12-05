using System;
using System.Net.Sockets;
using System.Threading;

namespace Eric.Comminucate
{
    /// <summary>
    /// TCP异步通讯Base
    /// </summary>
    public abstract class AsyncSessionBase
    {
        private string name;
        private object _syncObj = new object();
        public string Name { get { return name; } set { name = value; } }
        public string LocalIpPortString { get { return this.socket.LocalEndPoint.ToString(); } }
        public string RemoteIpPortString { get { return this.socket.RemoteEndPoint.ToString(); } }

        #region 
        internal byte[] bufferZone;
        internal Socket socket;
        internal SocketAsyncEventArgs asynReceiver;
        internal SocketAsyncEventArgs asynSender;
        internal ThreadQueue receivethreadqueue = new ThreadQueue();
        internal ThreadQueue sendthreadqueue = new ThreadQueue();
        #endregion

        private void Send(object o)
        {
            if (socket.Connected)
            {
                byte[] b = o as byte[];
                asynSender.SetBuffer(b, 0, b.Length);
                asynSender.RemoteEndPoint = socket.RemoteEndPoint;
                    socket.SendAsync(asynSender);
            }
        }
        public void Send(NetCommand command)
        {
           
            if (!socket.Connected) return;
            if (command.Sender == null || command.Sender == "")
            {
                if (this.Name != null && this.Name != "")
                { command.Sender = Name; }
                else
                { command.Sender = LocalIpPortString; }
                if (asynSender == null)
                    asynSender = new SocketAsyncEventArgs();
            }
            ByteCommand byteCommand = new ByteCommand(command);
            if (byteCommand.Bytes.Length > PublicConfig.ReceiveCount)
            {
                byte[] packetPart;
                int byteCount = byteCommand.Bytes.Length / PublicConfig.ReceiveCount;
                for (int i = 0; i <= byteCount; i++)
                {
                    if (i == byteCount)
                    {
                        packetPart = new byte[byteCommand.Bytes.Length - PublicConfig.ReceiveCount * i];
                        Array.Copy(byteCommand.Bytes, i * PublicConfig.ReceiveCount, packetPart, 0, byteCommand.Bytes.Length - PublicConfig.ReceiveCount * i);
                    }
                    else
                    {
                        packetPart = new byte[PublicConfig.ReceiveCount];
                        Array.Copy(byteCommand.Bytes, i * PublicConfig.ReceiveCount, packetPart, 0, PublicConfig.ReceiveCount);
                    }
                    sendthreadqueue.PutInNewItem(Send, packetPart);
                }
            }
            else
            {
                sendthreadqueue.PutInNewItem(Send, byteCommand.Bytes);
            }
        } 
        #region 解析接受的数据流，返回对象
        private void Receive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0) return;
            byte[] receiveBytes = new byte[e.BytesTransferred];
            Array.Copy(e.Buffer, 0, receiveBytes, 0, receiveBytes.Length);
            Receive(receiveBytes);
        }
        private void Receive(byte[] receiveBytes)
        {
            if (bufferZone == null)//缓冲区内没有信息
            {
                //分析报头，得到其长度
                int length = BitConverter.ToInt32(receiveBytes, 0);
                //if (length == 0)
                //{ }
                //如果长度不大于零，报头出错
                if (length <= 0) return;
                //报头接受正常
                if (length <= receiveBytes.Length - 4)//对象接受完全
                {
                    byte[] objectBytes = new byte[length];//对象缓冲区
                    Array.Copy(receiveBytes, 4, objectBytes, 0, objectBytes.Length);
                    ByteCommand byteCommand = new ByteCommand(objectBytes);
                    Receive(byteCommand.NetCommand);
                    //收集剩下的，递归解析
                    if (receiveBytes.Length - length - 4 > 0)
                    {
                        byte[] remain = new byte[receiveBytes.Length - length - 4];
                        Array.Copy(receiveBytes, length + 4, remain, 0, remain.Length);
                        Receive(remain);
                    }
                }
                else if (length > receiveBytes.Length - 4)//对象未接受完
                {
                    bufferZone = new byte[receiveBytes.Length];
                    Array.Copy(receiveBytes, 0, bufferZone, 0, receiveBytes.Length);
                }
            }
            else
            {
                //组合
                byte[] combination = new byte[receiveBytes.Length + bufferZone.Length];
                Array.Copy(bufferZone, 0, combination, 0, bufferZone.Length);
                Array.Copy(receiveBytes, 0, combination, bufferZone.Length, receiveBytes.Length);
                //清除缓冲区
                bufferZone = null;
                //递归分析
                Receive(combination);
            } 
        }

        /// <summary>
        /// 心跳包（侦听客户端是否断开） 
        /// </summary>
        internal void HartPacket()
        {
            if (socket.Poll(-1, SelectMode.SelectRead))
            {
                Close();
            }
        }

        public abstract void Receive(NetCommand command);
        #endregion
        internal void ThreadAction(object o)
        {
            Receive(o as SocketAsyncEventArgs);
        }
        internal void AsyncSessionOperationCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Send:
                    if (e.SocketError == SocketError.Success)
                    {
                        //发送成功
                    }
                    break;
                case SocketAsyncOperation.Receive:
                    if (e.SocketError == SocketError.Success)
                    {
                        try
                        {
                            ReceiveAsync(e);
                        }
                        catch
                        {
                            Close();
                        }
                    }
                    break;
            }
        }
        public void ReceiveAsync(SocketAsyncEventArgs e)
        { 
            receivethreadqueue.PutInNewItem(ThreadAction, e);
            if (!socket.ReceiveAsync(e))
                ReceiveAsync(e);
        }
        public abstract void Close();


    }
}
