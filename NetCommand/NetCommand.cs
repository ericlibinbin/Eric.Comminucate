using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPet.Communicate
{
    [Serializable]
    public class NetCommand
    {
        public string Target { get; set; }
        public string Sender { get; set; }
        public string Command { get; set; }
        public object Data { get; set; }
        public CommandLevel CommandLevel { get; set; }
        public CommandType CommandType { get; set; }
    }
    public enum CommandType
    {
        NoType = 0,
        Set_ClientName = 1,              //设置客户端用户名
        Get_OtherClients = 2,            //获取其他客户端用户名 
        Client_Disconnect = 3,           //客户端断开连接 
        Client_Connect = 4,              //客户端连接
        Client_Change = 5,               //客户端用户名更改
        Client_OtherConnect = 6,         //其他客户端的连接
    }
    /// <summary>
    /// 消息设置级别
    /// </summary>
    public enum CommandLevel
    {
        NORMAL=0,
        SYSTEM=1
    }
}
