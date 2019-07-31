using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;


public class VTOLServerPlugin : Plugin
{
    public override bool ThreadSafe => false;
    public override Version Version => new Version(0, 1, 0);
    private Server server;
    public VTOLServerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
    {
        server = new Server();
        server.plugin = this;
        server.Start();
        WriteEvent("Started " + server.Name, LogType.Info);

        ClientManager.ClientDisconnected += ClientDisconnected;
        ClientManager.ClientConnected += ClientJoinedLobby;
    }

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

    public void Log(string message)
    {
        WriteEvent(message, LogType.Info);
    }

    #region Commands

    public override Command[] Commands => new Command[]
{
        new Command("set", "Sets variables in the server", "set VariableToChange", SetSettings),
        new Command("playersinfo", "Displays all the information stored about the players","playersinfo", PlayersInfo),
        new Command("new","","",CreatePlayer),
        new Command("ban","Bans a player from the server using their steam id or player id", "ban steam steamid reason",BanUser),
        new Command("unban", "Unbans a player from the server using their steam id", "unban steamid", UnBanUser)
};
    private void CreatePlayer(object sender, CommandEventArgs e)
    {
        //Creating a fake player for testing
        Player newPlayer = new Player(999, null, "", "Player(" + (server.playerCount + 1) + ")", 999, "Non Steam player", server.useSteamName);
        server.AddPlayer(newPlayer);
        WriteEvent("Added Fake Player", LogType.Info);
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
                string newName = "";
                foreach (string word in argsBroken)
                {
                    newName += word + " ";
                }
                server.ChangeServerName(newName);
                WriteEvent("Server Name has Changed to \"" + server.Name + "\"", LogType.Info);
                break;
            case "maxplayercount":
                int result = server.MaxPlayerCount;
                if (int.TryParse(argsBroken[0], out result))
                {
                    if (result != server.MaxPlayerCount)
                    {
                        server.ChangeMaxPlayerCount(result);
                        WriteEvent("Changed the max player count to " + result, LogType.Info);
                    }
                }
                else
                    WriteEvent("Failed to change max player count", LogType.Warning);
                break;
            default:
                WriteEvent("Couldn't find command " + command, LogType.Warning);
                return;
        }

        SendServerInfo();
    }

    private void PlayersInfo(object sender, CommandEventArgs e)
    {
        string playersInfo = "There are " + server.playerCount + " players.";
        foreach (Player player in server.playerData.players)
        {
            if (player.isConnected)
            {
                playersInfo += "\nName:" + player.currentName + " Vehicle:" + player.vehicle;
            }
            
        }
        WriteEvent(playersInfo, LogType.Info);
    }

    private void BanUser(object sender, CommandEventArgs e)
    {
        string[] args = e.RawArguments;
        if (args.Length <= 2)
            return;
        args[0] = args[0].ToLower();
        
        if (args[0] == "steam" || args[0] == "playerid")
        {
            string[] reasonArray = new string[e.RawArguments.Length - 2];
            Array.Copy(e.RawArguments, 2, reasonArray, 0, e.RawArguments.Length - 2);
            string reason = "";
            foreach (string word in reasonArray)
            {
                reason += " " + word;
            }
            if (args[0] == "steam")
            {
                ulong steamid;
                if (ulong.TryParse(args[1], out steamid))
                {
                    switch (server.Ban(steamid, reason))
                    {
                        case Server.BanState.Banned:
                            WriteEvent("SteamID: " + steamid + " has been banned, reason\n" + reason, LogType.Warning);
                            break;
                        case Server.BanState.AlreadyBanned:
                            WriteEvent("This user is already banned", LogType.Error);
                            break;
                        default:
                            WriteEvent("There seemed to be an error", LogType.Error);
                            break;
                    }
                }
                else
                    WriteEvent("Failed to convert " + args[1] + " to a number", LogType.Error);
            }
            else if (args[0] == "playerid")
            {
                ushort playerID;
                if (ushort.TryParse(args[1], out playerID))
                {
                    switch(server.Ban(playersID: playerID, reason))
                    {
                        case Server.BanState.Banned:
                            WriteEvent("Player ID:" + playerID + " has been banned, reason\n" + reason, LogType.Warning);
                            break;
                        case Server.BanState.AlreadyBanned:
                            WriteEvent("This user is already banned", LogType.Error);
                            break;
                        case Server.BanState.NotOnline:
                            WriteEvent("There seems to be no player online with the ID " + playerID, LogType.Error);
                            break;
                        default:
                            WriteEvent("There seemed to be an error", LogType.Error);
                            break;
                    }
                    
                }
                else
                    WriteEvent("Failed to convert " + args[1] + " to a number", LogType.Error);
            }

            
        }
        
    }

    private void UnBanUser(object sender, CommandEventArgs e)
    {
        string[] args = e.RawArguments;
        ulong steamid;
        if (ulong.TryParse(args[0], out steamid))
        {
            if (server.Unban(steamid))
            {
                WriteEvent("User has been unbanned", LogType.Info);
            }
            else
            {
                WriteEvent("That user either didn't exist or wasn't banned", LogType.Warning);
            }
        }
        else
        {
            WriteEvent("Failed to convert " + args[0] + " to a steamid", LogType.Error);
        }
    }
    #endregion

    #region Lobby
    private void ClientJoinedLobby(object sender, ClientConnectedEventArgs e)
    {
        //Just got to add the listern when the first connect
        e.Client.MessageReceived += ClientMessageReceived;
        //After this we wait for a message with the users info, then we do a ban check on that user
    }
    /// <summary>
    /// Ban Check, this runs when the user sends us their information, then we check if they are banned or not.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="message"></param>
    private void ReceivedUserInfoTag(MessageReceivedEventArgs e, Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                ushort id = e.Client.ID;
                ulong steamid = reader.ReadUInt64();
                string PilotName = reader.ReadString();
                string SteamName = reader.ReadString();
                string banReason = "";

                Player newPlayer = new Player(id, e.Client, "", PilotName, steamid, SteamName, true);
                
                if (!server.CheckBan(steamid, out banReason))
                {
                    //Sending the information needed to display in the lobby page
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(server.Name);
                        writer.Write(server.Map.ToString());
                        writer.Write(server.playerCount);
                        writer.Write(server.MaxPlayerCount);

                        string playersNames = "";
                        if (server.playerCount > 0)
                        {
                            foreach (Player player in server.playerData.players)
                            {
                                if (player.isConnected)
                                    playersNames += player.currentName + ",";
                            }

                            //Removing that last ","
                            //playersNames = playersNames.Remove(playersNames.Length - 1); //Causing Error
                        }


                        writer.Write(playersNames);

                        using (Message newmessage = Message.Create((ushort)Tags.LobbyInfo, writer))
                        {
                            e.Client.SendMessage(newmessage, SendMode.Reliable);
                            WriteEvent("Told " + e.Client.ID + " lobby information", LogType.Info);
                        }

                        server.AddPlayer(newPlayer);
                    }

                    //Need to add the new player here instead of spawn tag method
                }
                else
                {
                    //This user is banned from the server
                    //Sending the information needed to display in the lobby page
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(banReason);
                        using (Message newMessage = Message.Create((ushort)Tags.Banned, writer))
                        {
                            e.Client.SendMessage(newMessage, SendMode.Reliable);
                            WriteEvent("Told " + SteamName + "[" + steamid + "]" + " that they where banned", LogType.Info);
                        }

                    }

                    e.Client.Disconnect();//Forcing the client to disconnect as they are banned :D
                }
            }
        }
    }
    #endregion

    #region In-Game

    #endregion

    private void SendServerInfo()
    {
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(server.Name);
            writer.Write(server.playerCount);

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
    private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
    {
        server.RemovePlayer(e.Client.ID);
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
            if (message.Tag == (ushort)Tags.UserInfo)
                ReceivedUserInfoTag(e, message);
            else if (message.Tag == (ushort)Tags.SpawnPlayerTag)
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
        SendServerInfo();
        //Adding my own on client connected so that we only start sending them information when their game is ready
        using (DarkRiftReader reader = message.GetReader())
        {
            while (reader.Position < reader.Length)
            {
                string name = reader.ReadString();
                string vehicle = reader.ReadString();
                //server.AddPlayer(new Player(e.Client.ID, e.Client, vehicle, name,0));
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
                    writer.Write(server.playerCount - 1);

                    foreach (Player player in server.playerData.players.Where(x => x.isConnected = true && x.client.ID != e.Client.ID))
                    {
                        writer.Write(player.ID);
                        writer.Write(player.currentName);
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