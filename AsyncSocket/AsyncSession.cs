using System.Net.Sockets;
using System.Threading;

namespace LPet.Communicate
{
    /// <summary>
    /// TCP异步远程客户端
    /// </summary>
    public class AsyncSession : AsyncSessionBase
    {
        private Thread thread;
        private bool isActive = true;
        public AsyncSession(Socket acceptSocket)
        {
            Initialize(acceptSocket);
        }
        public void Initialize(Socket acceptSocket)
        {
            socket = acceptSocket;
            //初始化接受者
            try
            {
                asynReceiver = new SocketAsyncEventArgs();
                asynReceiver.Completed += AsyncSessionOperationCompleted;
                asynReceiver.SetBuffer(new byte[PublicConfig.ReceiveCount], 0, PublicConfig.ReceiveCount);
                socket.ReceiveAsync(asynReceiver);
                //初始化发送者
                asynSender = new SocketAsyncEventArgs();
                asynSender.Completed += AsyncSessionOperationCompleted;
                //启动心跳包线程
                thread = new Thread(new ThreadStart(HartPacket));
                thread.Start();
            }
            catch { }
        }
       
        public override void Receive(NetCommand command)
        {
            //接受为系统设置信息
            if (command.CommandLevel == CommandLevel.SYSTEM)
            {
                if (SysCommandReceived != null)
                {
                    DataEventArgs args = new DataEventArgs();
                    args.Client = this.Name;
                    args.RemoteIpPortString = this.RemoteIpPortString;
                    args.Command = command;
                    SysCommandReceived(args);
                }
            }
            //接受为命令信息
            else
            {
                if (DataReceived != null)
                {
                    DataEventArgs args = new DataEventArgs();
                    args.Client = this.Name;
                    args.RemoteIpPortString = this.RemoteIpPortString;
                    args.Command = command;
                    DataReceived(args);
                }

            }
        }
        public override void Close()
        {
            if (isActive)
            {
                isActive = false;
                if (ClientDisconnected != null)
                {
                    ClientEventArgs args = new ClientEventArgs();
                    args.Client = this.Name;
                    args.RemoteIpPortString = this.RemoteIpPortString;
                    ClientDisconnected(args);
                }
                socket.Close(); 
               
            }
        }

        public event ClientEventHandle ClientDisconnected;
        public event DataEventHandle DataReceived;//普通信息
        public event DataEventHandle SysCommandReceived;//系统设置信息

    }
}
