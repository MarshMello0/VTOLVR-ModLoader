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
        private static void Main(string[] args)
        {
            NetworkObject.Factory = new NetworkObjectFactory();

            int playerCount = 32;
            UDPServer networkHandle = new UDPServer(playerCount);
            networkHandle.textMessageReceived += ReadTextFrame;
            networkHandle.playerAccepted += PlayerAccepted;

            networkHandle.objectCreated += NetworkObjectCreated;

            networkHandle.Connect();

            while (true)
            {
                if (Console.ReadLine().ToLower() == "exit")
                {
                    break;
                }
            }

            networkHandle.Disconnect(false);
        }

        private static void NetworkObjectCreated(NetworkObject target)
        {

        }

        private static void PlayerAccepted(NetworkingPlayer player, NetWorker sender)
        {
            Console.WriteLine($"New player accepted with id {player.NetworkId}");
        }

        private static void ReadTextFrame(NetworkingPlayer player, BeardedManStudios.Forge.Networking.Frame.Text frame, NetWorker sender)
        {
            Console.WriteLine("Read: " + frame.ToString());
        }
    }
}
