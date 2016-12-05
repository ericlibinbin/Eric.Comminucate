using System.Collections.Generic; 
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LPet.Communicate
{
    /// <summary>
    /// TCP异步客户端
    /// </summary>
    public class AsyncClient : AsyncSessionBase
    {
        #region 局部变量
        private Thread thread;
        private IPEndPoint ipEndPoint;
        private List<string> otherClients = new List<string>();
        private SocketAsyncEventArgs connectArgs;
        private bool isActive = false;
        #endregion
        /// <summary>
        /// 已经登录的其他用户
        /// </summary>
        public List<string> OtherClients
        {
            get { return otherClients; }
            set { otherClients = value; }
        }
        /// <summary>
        /// 指示客户端是否正在运行
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }
        /// <summary>
        /// 构造函数
        /// </summary> 
        public AsyncClient(string ip, int port)
        {
            isActive = false;
            ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }
        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            asynReceiver = new SocketAsyncEventArgs();
            asynReceiver.Completed += AsyncSessionOperationCompleted;
            asynReceiver.SetBuffer(new byte[PublicConfig.ReceiveCount], 0, PublicConfig.ReceiveCount);
            socket.ReceiveAsync(asynReceiver);
            //初始化发送者
            asynSender = new SocketAsyncEventArgs();
            asynSender.Completed += AsyncSessionOperationCompleted;
        }

        /// <summary>
        /// 异步连接结果接受
        /// </summary> 
        private void Args_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    if (e.SocketError == SocketError.Success)
                    {
                        isActive = true;
                        if (Name != null && Name != "") SetClientName(Name);
                        Initialize();
                        GetOtherClients();
                    }
                    else
                    {
                        if(ClientConnected!=null)
                        ClientConnected(new ClientEventArgs() { Client = Name, IsSuccess = false });
                    }
                    break;
            }
        }

        /// <summary>
        /// 开始异步连接
        /// </summary>
        public void Connect()
        {
            if (!isActive)
            {
                connectArgs = new SocketAsyncEventArgs();
                socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                connectArgs.RemoteEndPoint = ipEndPoint;
                connectArgs.Completed += Args_Completed;
                socket.ConnectAsync(connectArgs);
                thread = new Thread(new ThreadStart(HartPacket));
                thread.Start();
            }
        }

       
        /// <summary>
        /// 接受到信息处理
        /// </summary> 
        public override void Receive(NetCommand command)
        {
            if (command.CommandLevel != CommandLevel.SYSTEM)
            {
                if (DataReceived != null)
                {
                    DataEventArgs args = new DataEventArgs();
                    args.Client = Name;
                    args.RemoteIpPortString = this.RemoteIpPortString;
                    args.Command = command;
                    DataReceived(args);
                }
            }
            else
            {
                if (command.CommandType == CommandType.Get_OtherClients)
                {
                    OtherClients.Clear();
                    foreach (var item in (command.Data as Dictionary<string, string>))
                    {
                        if (item.Key != this.LocalIpPortString)
                            OtherClients.Add(item.Value);
                    }
                    if (ClientConnected != null)
                        ClientConnected(new ClientEventArgs() { Client = Name, RemoteIpPortString = this.RemoteIpPortString, IsSuccess = true });
                }
                if (command.CommandType == CommandType.Client_Connect)
                {  
                    OtherClients.Add(command.Command);
                    if (OtherClientConnected != null)
                        OtherClientConnected(new DataEventArgs() { Client = command.Command });
                }
                if (command.CommandType == CommandType.Client_Disconnect)
                {
                    OtherClients.Remove(command.Command);
                    if (OtherClientDisconnected != null)
                        OtherClientDisconnected(new DataEventArgs() { Client = command.Command });
                }
                if (command.CommandType == CommandType.Client_Change)
                {
                    OtherClients.Remove(command.Sender);
                    OtherClients.Add(command.Command);
                    if (OtherClientChanged != null)
                        OtherClientChanged(new DataEventArgs() { Command = command });
                }
                if (SysCommandReceived != null)
                {
                    DataEventArgs args = new DataEventArgs();
                    args.Client = this.Name;
                    args.RemoteIpPortString = this.RemoteIpPortString;
                    args.Command = command;
                    SysCommandReceived(args);
                }
            }
        }
        /// <summary>
        /// 关闭客户端
        /// </summary>
        public override void Close()
        {
            if (isActive)
            {
                isActive = false;
                socket.Close();
                socket.Dispose();
                if (ClientDisconnected != null)
                {
                    ClientEventArgs args = new ClientEventArgs();
                    args.Client = this.Name;
                    ClientDisconnected(args);
                }
            }
        }
        /// <summary>
        /// 获取已经登录的用户列表
        /// </summary>
        private void GetOtherClients()
        {
            Send(new NetCommand()
            {
                CommandLevel = CommandLevel.SYSTEM,
                CommandType = CommandType.Get_OtherClients
            });
        }
        /// <summary>
        /// 设置用户ID
        /// </summary> 
        private void SetClientName(string name)
        {
            Send(new NetCommand()
            {
                CommandLevel = CommandLevel.SYSTEM,
                CommandType = CommandType.Set_ClientName,
                Command = name
            });
        }

        /// <summary>
        /// 心跳包
        /// </summary>
        //private void HartPacket()
        //{
        //    if (socket.Poll(-1, SelectMode.SelectRead))
        //    {
        //        Close();
        //    }
        //}

        #region 事件
        public event ClientEventHandle ClientConnected;
        public event ClientEventHandle ClientDisconnected;
        public event DataEventHandle OtherClientChanged;
        public event DataEventHandle OtherClientDisconnected;
        public event DataEventHandle OtherClientConnected;
        public event DataEventHandle DataReceived;
        private event DataEventHandle SysCommandReceived;
        #endregion
    }
}
