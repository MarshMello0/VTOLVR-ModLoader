using DarkRift.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Linq;
public class Server
{
    //Information about server
    public string Name = "VTOL VR Dedicated Server";
    public string Description = "Server Description";
    public Maps Map = Maps.Akutan;
    public int MaxPlayerCount = 5;
    public int MaxBudget = 99999;
    public Weapons WeaponsData = new Weapons();
    public bool SpawnAircraftCarrier = true;
    public bool SpawnAirRefuelTanker = true;
    /// <summary>
    /// If ture, this uses the players steam name over the pilots name in game
    /// </summary>
    public bool useSteamName;


    [XmlIgnore] public int playerCount { private set; get; }
    [XmlIgnore] public PlayerData playerData = new PlayerData();

    [XmlIgnore] private string root;
    [XmlIgnore] private string serverDataPath = @"\ServerData.xml";
    [XmlIgnore] private string playerDataPath = @"\PlayerData.xml";
    [XmlIgnore] public VTOLServerPlugin plugin;
    //public Server(){}
    public void Start()
    {
        root = Directory.GetCurrentDirectory();
        if (File.Exists(root + serverDataPath))
            LoadServerData();
        else
            SaveServerData();

        if (File.Exists(root + playerDataPath))
            LoadPlayerData();
        else
            SavePlayerData();
    }
    private void LoadServerData()
    {
        using (FileStream stream = new FileStream(root + serverDataPath, FileMode.Open))
        {
            XmlSerializer server = new XmlSerializer(typeof(Server));
            Server deserialized = (Server)server.Deserialize(stream);
            Name = deserialized.Name;
            Description = deserialized.Description;
            Map = deserialized.Map;
            MaxPlayerCount = deserialized.MaxPlayerCount;
            useSteamName = deserialized.useSteamName;
        }
    }
    private void LoadPlayerData()
    {
        using (FileStream stream = new FileStream(root + playerDataPath, FileMode.Open))
        {
            XmlSerializer players = new XmlSerializer(typeof(PlayerData));
            playerData = (PlayerData)players.Deserialize(stream);
        }
    }
    public void SaveServerData()
    {
        using (FileStream stream = new FileStream(root + serverDataPath, FileMode.Create))
        {
            XmlSerializer server = new XmlSerializer(typeof(Server));
            server.Serialize(stream, this);
        }
    }
    public void SavePlayerData()
    {
        using (FileStream stream = new FileStream(root + playerDataPath, FileMode.Create))
        {
            XmlSerializer players = new XmlSerializer(typeof(PlayerData));
            players.Serialize(stream, playerData);
        }
    }
    
    private Player FindPlayerFromID(ushort playerid)
    {
        if (playerData.players == null)
        {
            plugin.Log("Players is null #####");
            return null;
        }
            
        for (int i = 0; i < playerData.players.Length; i++)
        {
            if (playerData.players[i].isConnected && playerData.players[i].ID == playerid)
            {
                return playerData.players[i];
            }
            plugin.Log(playerData.players[i].ID + "");
        }
        plugin.Log("Got to end");
        return null;
    }
    private Player FindPlayerFromID(ulong steamID)
    {
        if (playerData.players == null)
            return null;
        for (int i = 0; i < playerData.players.Length; i++)
        {
            if (playerData.players[i].SteamID == steamID)
            {
                return playerData.players[i];
            }
        }
        return null;
    }

    public void AddPlayer(Player player)
    {
        playerCount++;
        Player newPlayer = FindPlayerFromID(player.SteamID);
        if (newPlayer == null)
        {
            player.isConnected = true;
            if (playerData.players == null)
            {
                playerData.players = new Player[1];
                playerData.players[0] = player;
            }
            else
            {
                playerData.players = playerData.players.Concat(new Player[] { player }).ToArray();
            }
        }
        else
        {
            //Applying new information to our stored information
            newPlayer.ID = player.ID;
            newPlayer.isConnected = true;
            newPlayer.vehicle = player.vehicle;
            newPlayer.client = player.client;
            newPlayer.LastIP = player.client.RemoteTcpEndPoint.Address.ToString(); //Could of changed
            newPlayer.SteamName = player.SteamName; //Could of changed
            newPlayer.pilotName = player.pilotName; //Could of changed
        }
        SavePlayerData();
    }
    public void RemovePlayer(ushort playersID)
    {
        plugin.Log("Removing Player");
        playerCount = playerCount - 1;
        plugin.Log("Finding Player " + playersID);
        Player playerLeaving = FindPlayerFromID(playerid: playersID);
        playerLeaving.isConnected = false;
    }
    public enum BanState { Banned, NotBanned, NotOnline, AlreadyBanned }
    public BanState Ban(ushort playersID, string reason)
    {
        Player player = FindPlayerFromID(playersID);
        if (player == null)
            return BanState.NotOnline; //This will give a false ban positive, but the player isn't connected
        return Ban(player.SteamID, reason);
    }
    public BanState Ban(ulong steamID, string reason)
    {
        if (!CheckBan(steamID))
        {
            //BannedPlayers.Add(new Ban(steamID, reason));
            Player bannedPlayer = FindPlayerFromID(steamID);
            if (bannedPlayer == null)
            {
                bannedPlayer = new Player();
                if (playerData.players == null)
                {
                    playerData.players = new Player[] { bannedPlayer };
                }
                else
                {
                    playerData.players = playerData.players.Concat(new Player[] { bannedPlayer }).ToArray();
                }
            }
                
            bannedPlayer.IsBanned = true;
            bannedPlayer.BanReason = reason;
            bannedPlayer.SteamID = steamID;
            
            SavePlayerData();
            return BanState.Banned;
        }
        //This person is already banned
        return BanState.AlreadyBanned;
    }
    public bool Unban(ulong steamID)
    {
        if (playerData.players == null)
            return false;
        for (int i = 0; i < playerData.players.Length; i++)
        {
            if (playerData.players[i].SteamID == steamID)
            {
                if (!playerData.players[i].IsBanned)
                    return false;//This is just to state to the server owner, that they are already not banned
                playerData.players[i].IsBanned = false;
                playerData.players[i].BanReason = null;
                SavePlayerData();
                return true;
            }
        }
        
        return false;
    }
    public bool CheckBan(ulong steamID)
    {
        if (playerData.players == null)
            return false;
        foreach (Player player in playerData.players)
        {
            if (player.SteamID == steamID && player.IsBanned)
            {
                return true;
            }
        }
        return false;
    }
    public bool CheckBan(ulong steamID, out string reason)
    {
        reason = "";
        if (playerData.players == null)
            return false; 
        foreach (Player player in playerData.players)
        {
            if (player.SteamID == steamID && player.IsBanned)
            {
                reason = player.BanReason;
                return true;
            }
        }
        return false;
    }
    public void ChangeServerName(string newName)
    {
        Name = newName;
    }
    public void ChangeMaxPlayerCount(int newMax)
    {
        MaxPlayerCount = newMax;
    }
    

}

public class Player : PlayerData
{
    [XmlAttribute]
    public ulong SteamID;
    public string SteamName;
    public string LastIP;
    public bool IsBanned = false;
    public string BanReason = "";
    public bool isAdmin = false;
    [XmlIgnore]
    public bool isConnected;
    [XmlIgnore]
    public ushort ID;
    [XmlIgnore]
    public IClient client;
    [XmlIgnore]
    public string vehicle;
    [XmlIgnore]
    public string currentName;
    public string pilotName;
    public Player(ushort ID, IClient client, string vehicle, string PilotName, ulong SteamID, string SteamName, bool useSteamName)
    {
        this.ID = ID;
        this.client = client;
        this.vehicle = vehicle;
        this.pilotName = PilotName;
        this.SteamID = SteamID;
        this.SteamName = SteamName;
        if (useSteamName)
            currentName = this.SteamName;
        else
            currentName = this.pilotName;
        
    }
    public Player()
    {

    }

}
/// <summary>
/// This is the information which is stored about the player in the servers files
/// </summary>

public class PlayerData
{
    public Player[] players;
    public PlayerData()
    {

    }
}
public class Weapons
{
    [XmlAttribute]
    public bool AllowWeapons = true;
    public Weapons() { }
}
public enum Maps { Akutan }