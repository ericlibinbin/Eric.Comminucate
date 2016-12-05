using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace LPet.Communicate
{
    /// <summary>
    /// IP地址
    /// </summary>
    public class IPHelper
    {
        /// <summary>
        /// 获取当前主机的IP4地址
        /// </summary> 
        public static IPAddress HostIP()
        {
            IPAddress result = null;
            IPHostEntry ipe = Dns.GetHostEntry(Dns.GetHostName());
            int ipv4Count = 0;
            foreach (IPAddress item in ipe.AddressList)
            {
                if (item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                { result = item; ipv4Count++; }
            }
            if (ipv4Count > 1) result = null;
            return result;
        }
    }
}
