using System;
using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;


public class VTOLServerPlugin : Plugin
{
    public override bool ThreadSafe => false;

    public override Version Version => new Version(0, 0, 1);

    Dictionary<IClient, Player> players = new Dictionary<IClient, Player>();

    public VTOLServerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
    {
        ClientManager.ClientConnected += ClientConnected;
    }

    private void ClientConnected(object sender, ClientConnectedEventArgs e)
    {
        Player newPlayer = new Player(e.Client.ID, 0, 0, 0);

        using (DarkRiftWriter newPlayerWriter = DarkRiftWriter.Create())
        {
            newPlayerWriter.Write(newPlayer.ID);
            newPlayerWriter.Write(newPlayer.X);
            newPlayerWriter.Write(newPlayer.Y);
            newPlayerWriter.Write(newPlayer.Z);

            using (Message newPlayerMessage = Message.Create(Tags.SpawnPlayerTag, newPlayerWriter))
            {
                foreach (IClient client in ClientManager.GetAllClients().Where(x => x != e.Client))
                    client.SendMessage(newPlayerMessage, SendMode.Reliable);
            }
        }

        players.Add(e.Client, newPlayer);

        using (DarkRiftWriter playerWriter = DarkRiftWriter.Create())
        {
            foreach (Player player in players.Values)
            {
                playerWriter.Write(player.ID);
                playerWriter.Write(player.X);
                playerWriter.Write(player.Y);
                playerWriter.Write(player.Z);
            }

            using (Message playerMessage = Message.Create(Tags.SpawnPlayerTag, playerWriter))
                e.Client.SendMessage(playerMessage, SendMode.Reliable);
        }
    }
}