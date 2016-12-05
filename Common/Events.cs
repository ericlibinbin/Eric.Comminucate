using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eric.Comminucate
{
    public delegate void ClientEventHandle(ClientEventArgs e);
    public delegate void DataEventHandle(DataEventArgs e);

    public class ClientEventArgs
    {
        public string Client { get; set; }
        public bool IsSuccess { get; set; }
        public string RemoteIpPortString { get; set; }
        public string EventMessage { get; set; }
    }
    public class DataEventArgs
    {
        public string Client { get; set; }
        public string RemoteIpPortString { get; set; }
        public string EventMessage { get; set; }
        public NetCommand Command { get; set; }
    }
}
