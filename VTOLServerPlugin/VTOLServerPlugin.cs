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
    private string MapName;
    private int PlayerCount;
    private int MaxPlayerCount;


    public VTOLServerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
    {
        ServerName = "VTOL VR Dedicated Server";
        MapName = "Akutan";
        MaxPlayerCount = 100;
        ClientManager.ClientDisconnected += ClientDisconnected;
        ClientManager.ClientConnected += ClientJoinedLobby;
    }
    public override Command[] Commands => new Command[]
    {
        new Command("set", "Sets variables in the server", "set VariableToChange", SetSettings),
        new Command("playersinfo", "Displays all the information stored about the players","playersinfo", PlayersInfo),
        new Command("new","","",CreatePlayer)
    };

    /// <summary>
    /// Sends a message to everyone but the person who send in the message
    /// </summary>
    /// <param name="tag">The tag for the message</param>
    /// <param name="writer">The writer which has the message</param>
    /// <param name="e">The Messaeg Received Args of who sent the message</param>
    /// <param name="reliable">If it is a reliable message or not</param>
    private void MessageToEveryoneElse(ushort tag, DarkRiftWriter writer, MessageReceivedEventArgs e, bool reliable)
    {
        using (Message newMessage = Message.Create(tag, writer))
        {
            //.Where(x => x != e.Client)
            foreach (IClient client in ClientManager.GetAllClients())
            {
                client.SendMessage(newMessage, (reliable ? SendMode.Reliable : SendMode.Unreliable));
            }
        }
    }

    private void CreatePlayer(object sender, CommandEventArgs e)
    {
        //Creating a fake player for testing
        Player newPlayer = new Player(999, null, "", "Player(" + (PlayerCount + 1) + ")");
        players.Add(newPlayer);
        PlayerCount++;
        WriteEvent("Added Fake Player", LogType.Info);
    }

    private void ClientJoinedLobby(object sender, ClientConnectedEventArgs e)
    {
        //Just got to add the listern when the first connect
        e.Client.MessageReceived += ClientMessageReceived;

        //Sending the information needed to display in the lobby page
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(ServerName);
            writer.Write(MapName);
            writer.Write(PlayerCount);
            writer.Write(MaxPlayerCount);

            string playersNames = "";
            if (players.Count > 0)
            {
                foreach (Player player in players)
                {
                    playersNames += player.name + ",";
                }

                //Removing that last ","
                playersNames = playersNames.Remove(playersNames.Length - 1);
            }
            

            writer.Write(playersNames);

            using (Message message = Message.Create((ushort)Tags.LobbyInfo, writer))
            {
                e.Client.SendMessage(message, SendMode.Reliable);
                WriteEvent("Told " + e.Client.ID + " lobby information",LogType.Info);
            }
        }
    }

    private void PlayersInfo(object sender, CommandEventArgs e)
    {
        string playersInfo = "There are " + PlayerCount + " players.";
        foreach (Player player in players)
        {
            playersInfo += "\nName:" + player.name + " Vehicle:" + player.vehicle;
        }
        WriteEvent(playersInfo, LogType.Info);
    }
    private void SetSettings(object sender, CommandEventArgs e)
    {
        string[] args = e.RawArguments;
        if (args.Length <= 1)
            return;

        string command = args[0].ToLowerInvariant();

        string[] argsBroken = new string[e.RawArguments.Length - 1];
        Array.Copy(e.RawArguments, 1, argsBroken, 0, e.RawArguments.Length - 1);

        switch (command)
        {
            case "servername":
                ServerName = "";
                foreach (string word in argsBroken)
                {
                    ServerName += word + " ";
                }
                WriteEvent("Server Name has Changed to \"" + ServerName + "\"", LogType.Info);
                break;
            default:
                WriteEvent("Couldn't find command " + command, LogType.Warning);
                return;
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
                    client.SendMessage(message, SendMode.Reliable);
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
            else if (message.Tag == (ushort)Tags.FA26B_General)
                ReceivedFA26BGeneral(e, message);
            else if (message.Tag == (ushort)Tags.VehicleDeath)
                VehicleDeath(e, message);
            else if (message.Tag == (ushort)Tags.PlayerDeath)
                PlayerDeath(e, message);
            //This is sending the information back to all the other clients, all movement is the same, two vector3s
            else if (message.Tag == (ushort)Tags.PlayerHandLeft_Movement || message.Tag == (ushort)Tags.PlayerHandRight_Movement
                || message.Tag == (ushort)Tags.PlayerHead_Movement || message.Tag == (ushort)Tags.PlayerHandLeft_Rotation || message.Tag == (ushort)Tags.PlayerHandRight_Rotation
                || message.Tag == (ushort)Tags.PlayerHead_Rotation)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    float newX = reader.ReadSingle();
                    float newY = reader.ReadSingle();
                    float newZ = reader.ReadSingle();

                    //newZ += 50;
                    //newX += 50;

                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(e.Client.ID);
                        writer.Write(newX);
                        writer.Write(newY);
                        writer.Write(newZ);
                        message.Serialize(writer);
                    }
                    //Removed 
                    foreach (IClient c in ClientManager.GetAllClients().Where(x => x != e.Client))
                        c.SendMessage(message, e.SendMode);
                }
            }
        }
    }
    private void ReceivedSpawnPlayerTag(MessageReceivedEventArgs e, Message message)
    {
        PlayerCount += 1;
        SendServerInfo();


        //Adding my own on client connected so that we only start sending them information when their game is ready
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
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

                    MessageToEveryoneElse((ushort)Tags.SpawnPlayerTag, writer, e, true);
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

                float pitch = reader.ReadSingle();
                float yaw = reader.ReadSingle();
                float roll = reader.ReadSingle();
                float breaks = reader.ReadSingle();
                float throttle = reader.ReadSingle();
                float wheels = reader.ReadSingle();

                //Sending the information to all other clients
                positionX += 50;
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

                    writer.Write(pitch);
                    writer.Write(yaw);
                    writer.Write(roll);
                    writer.Write(breaks);
                    writer.Write(throttle);
                    writer.Write(wheels);

                    MessageToEveryoneElse((ushort)Tags.AV42c_General, writer, e, false);

                }

            }
        }
    }
    private void ReceivedFA26BGeneral(MessageReceivedEventArgs e, Message message)
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

                float pitch = reader.ReadSingle();
                float yaw = reader.ReadSingle();
                float roll = reader.ReadSingle();
                float breaks = reader.ReadSingle();
                float throttle = reader.ReadSingle();
                float wheels = reader.ReadSingle();

                //Sending the information to all other clients
                positionX += 50;
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

                    writer.Write(pitch);
                    writer.Write(yaw);
                    writer.Write(roll);
                    writer.Write(breaks);
                    writer.Write(throttle);
                    writer.Write(wheels);

                    MessageToEveryoneElse((ushort)Tags.FA26B_General, writer, e, false);
                }
            }
        }
    }

    private void VehicleDeath(MessageReceivedEventArgs e, Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                ushort id = e.Client.ID;
                string deathMessage = reader.ReadString();

                WriteEvent("[" + id + "] " + deathMessage, LogType.Info);

                //Telling everyone that this person crashed
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(id);
                    writer.Write(deathMessage);

                    MessageToEveryoneElse((ushort)Tags.VehicleDeath, writer, e, true);
                }

            }
        }
    }
    private void PlayerDeath(MessageReceivedEventArgs e, Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                ushort id = e.Client.ID;
                string deathMessage = reader.ReadString();

                WriteEvent("[" + id + "] " + deathMessage, LogType.Info);

                //Telling everyone that this person crashed
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(id);
                    writer.Write(deathMessage);

                    MessageToEveryoneElse((ushort)Tags.PlayerDeath, writer, e, true);
                }

            }
        }
    }
}