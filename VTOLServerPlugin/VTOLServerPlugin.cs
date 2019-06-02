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

    //Dictionary<IClient, Player> players = new Dictionary<IClient, Player>();


    //Sever Info
    private string ServerName;
    private int PlayerCount;


    public VTOLServerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
    {
        ServerName = "VTOL VR Dedicated Server";
        ClientManager.ClientConnected += ClientConnected;
        ClientManager.ClientDisconnected += ClientDisconnected;
    }

    public override Command[] Commands => new Command[]
    {
        new Command("set", "Sets variables in the server", "set VariableToChange", SetSettings),
        new Command("players", "Says how many players there are", "players", Players)
    };

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

    private void ClientConnected(object sender, ClientConnectedEventArgs e)
    {
        WriteEvent("Player " + e.Client.ID + " has joined", LogType.Warning);
        PlayerCount += 1;
        SendServerInfo(e.Client);
        e.Client.MessageReceived += ClientMessageReceived;
        //Player newPlayer = new Player(e.Client.ID);

        //This sends to everyone but the person who joined, the new persons ID
        using (DarkRiftWriter newPlayerWriter = DarkRiftWriter.Create())
        {
            newPlayerWriter.Write(e.Client.ID);

            using (Message newPlayerMessage = Message.Create((ushort)Tags.SpawnPlayerTag, newPlayerWriter))
            {
                //.Where(x => x != e.Client)
                foreach (IClient client in ClientManager.GetAllClients())
                {
                    client.SendMessage(newPlayerMessage, SendMode.Reliable);
                    
                }
            }
        }

        //players.Add(e.Client, newPlayer);

        //This sends backto the player who joined, everyones ID
        using (DarkRiftWriter playerWriter = DarkRiftWriter.Create())
        {
            foreach (IClient client in ClientManager.GetAllClients().Where(x => x != e.Client))
            {
                playerWriter.Write(client.ID);
            }

            using (Message playerMessage = Message.Create((ushort)Tags.SpawnPlayerTag, playerWriter))
            {
                e.Client.SendMessage(playerMessage, SendMode.Reliable);
            }
                
        }
    }

    private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
    {
        PlayerCount -= 1;
        SendServerInfo();

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
        }
    }
}