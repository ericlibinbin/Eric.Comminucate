using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPet.Communicate
{
    public enum Model
    {
        OtherClients = 0, 
        AllClients = 1,
        None = 2
    }
    /// <summary>
    /// 协议类型
    /// </summary>
    public enum TcpOrUdp
    {
        TCP = 1,
        UDP = 2,
    }
    /// <summary>
    /// 连接类型
    /// </summary>
    public enum SyncOrAsync
    {
        synchronous = 1,  //同步
        asynchronous = 2, //异步
    }
    /// <summary>
    /// 传输内容类型
    /// </summary>
    public enum ObjectType
    {
        Object = 1,
        String = 2,
        Command = 3,
    }
}
