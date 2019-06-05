using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;


public class VTOLServerPlugin : Plugin
{
    public override bool ThreadSafe => false;

    public override Version Version => new Version(0, 0, 1);

    private List<Player> players = new List<Player>();


    //Sever Info
    private string ServerName;
    private int PlayerCount;


    public VTOLServerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
    {
        ServerName = "VTOL VR Dedicated Server";
        //ClientManager.ClientConnected += ClientConnected;
        ClientManager.ClientDisconnected += ClientDisconnected;
    }
    public override Command[] Commands => new Command[]
    {
        new Command("set", "Sets variables in the server", "set VariableToChange", SetSettings),
        new Command("players", "Says how many players there are", "players", Players),
        new Command("playersinfo", "Displays all the information stored about the players","playersinfo", PlayersInfo)
    };
    private void PlayersInfo(object sender, CommandEventArgs e)
    {
        string playersInfo = "There are " + PlayerCount + " players.";
        foreach (Player player in players)
        {
            playersInfo += "\nName:" + player.name + " Vehicle:" + player.vehicle;
        }
        WriteEvent(playersInfo, LogType.Info);
    }
    private void Players(object sender, CommandEventArgs e)
    {
        WriteEvent("There are " + PlayerCount + " players", LogType.Info);
    }
    private void SetSettings(object sender, CommandEventArgs e)
    {
        string[] args = e.RawArguments;
        if (args.Length <= 1)
            return;

        args[0] = args[0].ToLowerInvariant();

        switch (args[0])
        {
            case "servername":
                ServerName = args[1];
                WriteEvent("Server Name has Changed to \"" + ServerName + "\"", LogType.Info);
                break;
        }

        SendServerInfo();
    }
    private void SendServerInfo()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(ServerName);
            writer.Write(PlayerCount);

            using (Message message = Message.Create((ushort)Tags.ServerInfo, writer))
            {
                foreach (IClient client in ClientManager.GetAllClients())
                {
                    client.SendMessage(message,SendMode.Reliable);
                }
                WriteEvent("Sent New Server Info to Clients", LogType.Info);
            }
                
        }
    }
    private void SendServerInfo(IClient client)
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(ServerName);
            writer.Write(PlayerCount);

            using (Message message = Message.Create((ushort)Tags.ServerInfo, writer))
            {
                client.SendMessage(message, SendMode.Reliable);
                WriteEvent("Sent Server Info to " + client.ID + " (TAG = " + (ushort)Tags.ServerInfo + ")", LogType.Info);
            }

        }
    }
    private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
    {
        PlayerCount -= 1;
        SendServerInfo();

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ID == e.Client.ID)
            {
                players.RemoveAt(i);
                break;
            }
        }

        SendPlayerInfo();

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(e.Client.ID);

            using (Message message = Message.Create((ushort)Tags.DestroyPlayer, writer))
            {
                foreach (IClient client in ClientManager.GetAllClients().Where(x => x != e.Client))
                {
                    client.SendMessage(message, SendMode.Reliable);
                }
            }
        }
    }
    private void ClientMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            //This is sending the information back to all the other clients, all movement is the same, two vector3s
            if (message.Tag == (ushort)Tags.PlayerHandLeft_Movement || message.Tag == (ushort)Tags.PlayerHandRight_Movement
                || message.Tag == (ushort)Tags.PlayerHead_Movement || message.Tag == (ushort)Tags.PlayerHandLeft_Rotation || message.Tag == (ushort)Tags.PlayerHandRight_Rotation
                || message.Tag == (ushort)Tags.PlayerHead_Rotation)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    float newX = reader.ReadSingle();
                    float newY = reader.ReadSingle();
                    float newZ = reader.ReadSingle();

                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(e.Client.ID);
                        writer.Write(newX);
                        writer.Write(newY);
                        writer.Write(newZ);
                        message.Serialize(writer);
                    }
                    //Removed .Where(x => x != e.Client)
                    foreach (IClient c in ClientManager.GetAllClients())
                        c.SendMessage(message, e.SendMode);
                }
            }
            else if (message.Tag == (ushort)Tags.PlayersInfo)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    string name = reader.ReadString();
                    string vehicle = reader.ReadString();
                    players.Add(new Player(e.Client.ID, e.Client, vehicle, name));
                    WriteEvent(name + " joined using " + vehicle, LogType.Warning);
                }
                SendPlayerInfo();
            }
            else if (message.Tag == (ushort)Tags.SpawnPlayerTag)
                ReceivedSpawnPlayerTag(e,message);
        }
    }
    private void SendPlayerInfo()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(players.Count);

            foreach (Player player in players)
            {
                writer.Write(player.name);
                writer.Write(player.vehicle);
            }

            using (Message message = Message.Create((ushort)Tags.PlayersInfo, writer))
            {
                foreach (IClient client in ClientManager.GetAllClients())
                {
                    client.SendMessage(message, SendMode.Reliable);
                }
            }
        }
    }
    private void ReceivedSpawnPlayerTag(MessageReceivedEventArgs e, Message message)
    {
        PlayerCount += 1;
        SendServerInfo(e.Client);
        e.Client.MessageReceived += ClientMessageReceived;

        //Adding my own on client connected so that we only start sending them information when their game is ready
        using (DarkRiftReader reader = message.GetReader())
        {
            while(reader.Position > reader.Length)
            {
                string name = reader.ReadString();
                string vehicle = reader.ReadString();
                players.Add(new Player(e.Client.ID, e.Client, vehicle, name));
                WriteEvent(name + " joined using " + vehicle, LogType.Warning);

                //This sends to everyone but the person who joined, the new persons ID
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    //First send how many new players to spawn
                    writer.Write(1);

                    writer.Write(e.Client.ID);
                    writer.Write(name);
                    writer.Write(vehicle);

                    using (Message newPlayerMessage = Message.Create((ushort)Tags.SpawnPlayerTag, writer))
                    {
                        //.Where(x => x != e.Client)
                        foreach (IClient client in ClientManager.GetAllClients())
                        {
                            client.SendMessage(newPlayerMessage, SendMode.Reliable);
                        }
                    }
                }

                //This sends backto the player who joined, everyones ID
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    //First send how many new players to spawn
                    writer.Write(players.Count - 1);

                    foreach (Player player in players.Where(x => x.client.ID != e.Client.ID))
                    {
                        writer.Write(player.ID);
                        writer.Write(player.name);
                        writer.Write(player.vehicle);
                    }

                    using (Message playerMessage = Message.Create((ushort)Tags.SpawnPlayerTag, writer))
                    {
                        e.Client.SendMessage(playerMessage, SendMode.Reliable);
                    }

                }
            }
        }
    }
}