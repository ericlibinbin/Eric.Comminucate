using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPet.Communicate
{
    /// <summary>
    /// 全局属性信息
    /// </summary>
    public class PublicConfig
    {
        public static int Port { get { return 43239; } }
        public static int ReceiveCount { get { return 35000; } } 
    }
}
