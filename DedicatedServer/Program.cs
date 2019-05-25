using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Frame;

namespace DedicatedServer
{
    class Program
    {
        static void Main(string[] args)
        {
            NetworkObject.Factory = new NetworkObjectFactory();
            UDPServer udpserver = new UDPServer(32);
            udpserver.textMessageReceived += Program.ReadTextFrame;
            udpserver.playerAccepted += Program.PlayerAccepted;
            udpserver.objectCreated += Program.NetworkObjectCreated;
            udpserver.Connect("0.0.0.0", 15937, "", 15941);
            while (!(Console.ReadLine().ToLower() == "exit"))
            {
            }
            udpserver.Disconnect(false);
        }

        private static void NetworkObjectCreated(NetworkObject target)
        {

        }

        private static void PlayerAccepted(NetworkingPlayer player, NetWorker sender)
        {
            Console.WriteLine(string.Format("New player accepted with id {0}", player.NetworkId));
        }

        private static void ReadTextFrame(NetworkingPlayer player, Text frame, NetWorker sender)
        {
            Console.WriteLine("Read: " + frame.ToString());
        }
    }
}
