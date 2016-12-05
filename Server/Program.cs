using Eric.Comminucate;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static AsyncServer  server;
        static void Main(string[] args)
        {
           
             server= new AsyncServer();
            server.Start();
            server.ClientConnected += Server_ClientConnected;
            server.DataReceived += Server_DataReceived;
            Console.ReadKey();
        }

        private static void Server_ClientConnected(ClientEventArgs e)
        {
            Console.WriteLine("一个新客户端接入");
        }

        private static void Server_DataReceived(DataEventArgs e)
        {
            Person persons= e.Command.Data as Person;
                Console.WriteLine(persons.Name);
            server.SendToClient(e.Command, e.RemoteIpPortString);
        }
    }
    
}
