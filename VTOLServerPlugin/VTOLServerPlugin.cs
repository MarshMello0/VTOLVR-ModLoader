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
        ClientManager.ClientDisconnected += ClientDisconnected;
        ClientManager.ClientConnected += ClientFirstConnect;
    }
    private void ClientFirstConnect(object sender, ClientConnectedEventArgs e)
    {
        //Just got to add the listern when the first connect
        e.Client.MessageReceived += ClientMessageReceived;
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
            if (message.Tag == (ushort)Tags.SpawnPlayerTag)
                ReceivedSpawnPlayerTag(e, message);
            else if (message.Tag == (ushort)Tags.AV42c_General)
                ReceivedAV42CGeneral(e, message);
            //This is sending the information back to all the other clients, all movement is the same, two vector3s
            else if (message.Tag == (ushort)Tags.PlayerHandLeft_Movement || message.Tag == (ushort)Tags.PlayerHandRight_Movement
                || message.Tag == (ushort)Tags.PlayerHead_Movement || message.Tag == (ushort)Tags.PlayerHandLeft_Rotation || message.Tag == (ushort)Tags.PlayerHandRight_Rotation
                || message.Tag == (ushort)Tags.PlayerHead_Rotation || message.Tag == (ushort)Tags.BasicVehicle_Movement || message.Tag == (ushort)Tags.BasicVehicle_Rotation)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    float newX = reader.ReadSingle();
                    float newY = reader.ReadSingle();
                    float newZ = reader.ReadSingle();

                    newZ += 50;
                    newX += 50;

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
        }
    }
    private void ReceivedSpawnPlayerTag(MessageReceivedEventArgs e, Message message)
    {
        PlayerCount += 1;
        SendServerInfo(e.Client);
        

        //Adding my own on client connected so that we only start sending them information when their game is ready
        using (DarkRiftReader reader = message.GetReader())
        {
            while(reader.Position < reader.Length)
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
    private void ReceivedAV42CGeneral(MessageReceivedEventArgs e, Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                ushort id = e.Client.ID;

                float positionX = reader.ReadSingle();
                float positionY = reader.ReadSingle();
                float positionZ = reader.ReadSingle();
                float rotationX = reader.ReadSingle();
                float rotationY = reader.ReadSingle();
                float rotationZ = reader.ReadSingle();

                float speed = reader.ReadSingle();
                bool landingGear = reader.ReadBoolean();
                float flaps = reader.ReadSingle();
                float thrusterAngle = reader.ReadSingle();

                //Sending the information to all other clients

                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(id);
                    writer.Write(positionX);
                    writer.Write(positionY);
                    writer.Write(positionZ);

                    writer.Write(rotationX);
                    writer.Write(rotationY);
                    writer.Write(rotationZ);

                    writer.Write(speed);
                    writer.Write(landingGear);
                    writer.Write(flaps);
                    writer.Write(thrusterAngle);

                    using (Message newMessage = Message.Create((ushort)Tags.AV42c_General, writer))
                    {
                        //.Where(x => x != e.Client)
                        foreach (IClient client in ClientManager.GetAllClients())
                        {
                            client.SendMessage(newMessage, SendMode.Unreliable);
                        }
                    }

                }

            }
        }
    }
}