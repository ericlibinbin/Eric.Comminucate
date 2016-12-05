using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Eric.Comminucate
{
    /// <summary>
    /// TCP异步服务端
    /// </summary>
    public class AsyncServer
    {
        #region 内部变量
        private IPEndPoint ipEndPoint;
        private Socket acceptSocket;
        private SocketAsyncEventArgs acceptArgs;
        private bool IsActive;
        private Dictionary<string, string> clients = new Dictionary<string, string>();
        private Dictionary<string, AsyncSession> sessions = new Dictionary<string, AsyncSession>();
        #endregion

        /// <summary>
        /// 转发模式
        /// </summary>
        public Model Model { get; set; }

        public AsyncServer()
            : this(PublicConfig.Port)
        {

        }

        public AsyncServer(int port)
        {
            ipEndPoint = new IPEndPoint(IPAddress.Any, port);
            IsActive = false;
            //DataReceived += SendTo;
        }

        /// <summary>
        /// 事件接受器
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">Socket异步参数</param>
        private void SocketAsyncEventArgs_Completed(object sender,SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    if (e.SocketError == SocketError.Success)
                    {
                        Accept(e);//接受客户端
                        //继续接受
                        try
                        {
                            acceptArgs = new SocketAsyncEventArgs();
                            acceptArgs.Completed += SocketAsyncEventArgs_Completed;
                            if (!acceptSocket.AcceptAsync(acceptArgs))
                                SocketAsyncEventArgs_Completed(this, acceptArgs);
                        }
                        catch { }
                    }
                    break;
            }
        }

        private void Accept(SocketAsyncEventArgs args)
        {
            AsyncSession session = new AsyncSession(args.AcceptSocket);
            sessions.Add(session.RemoteIpPortString, session);
            session.ClientDisconnected += DisconnectedClient;
            session.DataReceived += DataReceived;
            session.SysCommandReceived += Sys_CommandReceived;
            if (ClientConnected != null)
            {
                ClientEventArgs clientArgs = new ClientEventArgs();
                clientArgs.RemoteIpPortString = session.RemoteIpPortString;
                //连接成功后执行事件
                ClientConnected(clientArgs);
            }
        }

        void DisconnectedClient(ClientEventArgs e)
        {
            StopClient(e.RemoteIpPortString);
        }

        private void Sys_CommandReceived(DataEventArgs e)
        {
            AsyncSession session = sessions[e.RemoteIpPortString];

            if (e.Command.CommandType == CommandType.Set_ClientName)
            {
                if (clients.ContainsValue(e.Command.Command))
                {
                    SendTo(new DataEventArgs()
                    { 
                        Command = new NetCommand()
                        { 
                            Target = e.Command.Command,
                            CommandLevel = CommandLevel.NORMAL,
                            CommandType = CommandType.Client_OtherConnect,
                            Sender = e.RemoteIpPortString
                        }
                    });
                }
                if (!clients.ContainsKey(e.RemoteIpPortString))
                {
                    session.Name = e.Command.Command; 
                    clients.Add(e.RemoteIpPortString, e.Command.Command);
                    SendToOtherClients(new NetCommand()
                    {
                        CommandLevel = CommandLevel.SYSTEM,
                        CommandType = CommandType.Client_Connect,
                        Command = e.Command.Command
                    }, e.Command.Command);
                }
                else
                {
                    session.Name = e.Command.Command;
                    SendToOtherClients(new NetCommand()
                        {
                            CommandLevel = CommandLevel.SYSTEM,
                            CommandType = CommandType.Client_Change,
                            Command = e.Command.Command,
                            Sender = e.RemoteIpPortString
                        }, e.Command.Command);
                    clients[e.RemoteIpPortString] = e.Command.Command;
                }
            }
            if (e.Command.CommandType == CommandType.Get_OtherClients)
            {
                sessions[e.RemoteIpPortString].Send(new NetCommand()
                {
                    CommandLevel = CommandLevel.SYSTEM,
                    CommandType = CommandType.Get_OtherClients,
                    Data = clients
                });
            }
        }

        /// <summary>
        /// 停止客户端
        /// </summary>
        /// <param name="IpPortString">客户端IP和Port信息字符串</param>
        private void StopClient(string IpPortString)
        {
            if (sessions.ContainsKey(IpPortString))
            {
                if (clients.ContainsKey(IpPortString))
                    SendToOtherClients(new NetCommand() { CommandLevel = CommandLevel.SYSTEM, CommandType = CommandType.Client_Disconnect, Command = clients[IpPortString] }, clients[IpPortString]);
                var clientargs = new ClientEventArgs() { RemoteIpPortString = IpPortString };
                if (clients.ContainsKey(IpPortString))
                { clientargs.Client = clients[IpPortString]; clients.Remove(IpPortString); }

                if(ClientDisconnected!=null)
                ClientDisconnected(clientargs);

                sessions.Remove(IpPortString);
            }
        }

        #region 转发
        public void SendToAllClients(NetCommand command)
        {
            foreach (var item in sessions)
            {
                item.Value.Send(command);
            }
        }
        public void SendToOtherClients(NetCommand command, string except)
        {
            if (clients.ContainsValue(except))
            {
                foreach (var item in clients)
                {
                    if (item.Value == except)
                    { except = item.Key; break; }
                }
            }
            foreach (var item in sessions)
            {
                if (item.Key != except)
                    item.Value.Send(command);
            }
        }
        public void SendToClient(NetCommand command, string client)
        {
            if (clients.ContainsValue(client))
            {
                foreach (var item in clients)
                {
                    if (item.Value == client)
                    {
                        client = item.Key;
                        break;
                    }
                }
            }
            if (sessions.ContainsKey(client))
                sessions[client].Send(command);
        }
        public void SendTo(DataEventArgs e)
        {
            if (e.Command.Target != null && e.Command.Target.Trim() != "")
            {
                if (clients.ContainsValue(e.Command.Target))
                {
                    SendToClient(e.Command, e.Command.Target);
                }
                return;
            }
            switch (Model)
            {
                case Model.OtherClients:
                    SendToOtherClients(e.Command, e.Client);
                    break;
                case Model.AllClients:
                    SendToAllClients(e.Command);
                    break;
                case Model.None:
                    break;
            }
        }
        #endregion

        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            try
            {
                if (!IsActive)
                {
                    acceptSocket = new System.Net.Sockets.Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    acceptSocket.Bind(ipEndPoint);
                    acceptSocket.Listen(100);
                    acceptArgs = new SocketAsyncEventArgs();
                    acceptArgs.Completed += SocketAsyncEventArgs_Completed;
                    IsActive = true;
                    try
                    {
                        if (!acceptSocket.AcceptAsync(acceptArgs))
                            SocketAsyncEventArgs_Completed(this, acceptArgs);
                    }
                    catch { }
                }
                else
                    Console.WriteLine("此服务已经启动");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
           
        }
        public void Stop()
        {
            if (IsActive)
            {
                IsActive = false;
                foreach (var item in sessions)
                {
                    item.Value.socket.Close();
                }
                clients.Clear();
                acceptSocket.Close();
                sessions.Clear();
            }
        }


        public event ClientEventHandle ClientConnected;
        public event ClientEventHandle ClientDisconnected;
        public event DataEventHandle DataReceived;
    }
}
