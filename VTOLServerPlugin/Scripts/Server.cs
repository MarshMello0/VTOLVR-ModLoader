using DarkRift.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

public class Server
{
    //Information about server
    public string Name = "VTOL VR Dedicated Server";
    public string Description = "Server Description";
    public Maps Map = Maps.Akutan;
    public int MaxPlayerCount = 5;
    /// <summary>
    /// If ture, this uses the players steam name over the pilots name in game
    /// </summary>
    public bool useSteamName;
    [XmlIgnore] public int playerCount { private set; get; }
    [XmlIgnore] public List<Player> currentPlayers { private set; get; }

    [XmlIgnore] public PlayerData playerData = new PlayerData();

    [XmlIgnore] private string root;
    [XmlIgnore] private string serverDataPath = @"\ServerData.xml";
    [XmlIgnore] private string playerDataPath = @"\PlayerData.xml";
    public Server(){}
    public void Start()
    {
        currentPlayers = new List<Player>();
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
    
    private Player FindPlayerFromID(ushort id)
    {
        for (int i = 0; i < currentPlayers.Count; i++)
        {
            if (currentPlayers[i].ID == id)
            {
                return currentPlayers[i];
            }
        }
        return null;
    }

    public void AddPlayer(Player player)
    {
        playerCount++;
        currentPlayers.Add(player);
    }
    public void RemovePlayer(ushort playersID)
    {
        playerCount--;
        currentPlayers.Remove(FindPlayerFromID(playersID));

    }
    public void Ban(ushort playersID, string reason)
    {
        Player player = FindPlayerFromID(playersID);
        Ban(player.SteamID, reason);
    }
    public bool Ban(ulong steamID, string reason)
    {
        if (!CheckBan(steamID))
        {
            //BannedPlayers.Add(new Ban(steamID, reason));
            return true;
        }
        //This person is already banned
        return false;
    }
    public bool Unban(ulong steamID)
    {
        /*
        for (int i = 0; i < bannedPlayers.Count; i++)
        {
            if (bannedPlayers[i].SteamID == steamID)
            {
                bannedPlayers.Remove(bannedPlayers[i]);
                return true;
            }
        }
        */
        return false;
    }
    public bool CheckBan(ulong steamID)
    {
        foreach (Player player in playerData.players)
        {
            if (player.IsBanned)
            {
                return true;
            }
        }
        return false;
    }
    public bool CheckBan(ulong steamID, out string reason)
    {
        reason = "";
        foreach (Player player in playerData.players)
        {
            if (player.IsBanned)
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
    public ushort ID;
    [XmlIgnore]
    public IClient client;
    [XmlIgnore]
    public string vehicle;
    [XmlIgnore]
    public string currentName;
    [XmlIgnore]
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
public enum Maps { Akutan }