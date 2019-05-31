using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DarkRift.Client;
using DarkRift;
using System.Net;

namespace Multiplayer
{
    public class Load
    {
        public static void Init()
        {
            new GameObject("Multiplayer", typeof(MultiplayerScript));
        }
    }
}
public class MultiplayerScript : MonoBehaviour
{
    private NetworkingManager manager;
    private bool isConnected;

    private void Awake()
    {
        manager = NetworkingManager.Instance;
    }

    private void Start()
    {
        manager.client.MessageReceived += MessageReceived;
        manager.client.Disconnected += Disconnected;
        isConnected = manager.Connect();
        if (isConnected)
            Connected();
    }

    private void Connected()
    {
        Log("Connected!");
        /*
         * To Do
         * Search for players hands and head
         * Place something which sends information to the server of their location on them
         * Get the server plugin to relay that information down to the clients
         */
    }

    private void Disconnected(object sender, DisconnectedEventArgs e)
    {
        Log("Disconnected");
    }

    private void OnGUI()
    {
        if (isConnected)
        {
            GUI.Label(new Rect(0, 0, 100, 100), "Connected!");
            if (GUI.Button(new Rect(0,50,100,100),"Disconnect"))
            {
                manager.client.Disconnect();
                isConnected = false;
            }
        }
        else
        {
            GUI.Label(new Rect(0, 0, 100, 100), "Failed to connect");
            if (GUI.Button(new Rect(0, 50, 100, 100), "Retry"))
            {
                isConnected = manager.Connect();
                if (isConnected)
                    Connected();
            }
        }
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                if (message.Tag == Tags.SpawnPlayerTag)
                {
                    while (reader.Position < reader.Length)
                    {
                        ushort id = reader.ReadUInt16();
                        float x = reader.ReadSingle();
                        float y = reader.ReadSingle();
                        float z = reader.ReadSingle();
                    }
                }
            }
        }
    }

    private void Log(object message)
    {
        Debug.Log("# Multiplayer: " + message);
    }

    private void OnApplicationQuit()
    {
        manager.client.MessageReceived -= MessageReceived;
        manager.client.Disconnected -= Disconnected;
    }
}

public class NetworkingManager
{
    private static NetworkingManager instance;
    public static NetworkingManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new NetworkingManager();
            }
            return instance;
        }
    }
    public DarkRiftClient client;
    
    private NetworkingManager()
    {
        client = new DarkRiftClient();
    }
    
    public bool Connect()
    {
        if (client.Connected)
            return false;

        try
        {
            client.Connect(IPAddress.Parse("127.0.0.1"), 4296, DarkRift.IPVersion.IPv4);
            return true;
        }
        catch (Exception)
        {
            return false;
        }

    }
    
}

public static class Tags
{
    public static readonly ushort SpawnPlayerTag = 0;
}
