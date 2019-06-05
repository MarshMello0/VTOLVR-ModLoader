using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.SceneManagement;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using NetworkedObjects.Player;
using NetworkedObjects.Vehicles;


public class NetworkingManager : MonoBehaviour
{
    //This handles all of the multiplayer code in the game scene

    private UnityClient client;
    public MultiplayerMod mod;

    //These are things used to spawn other clients in
    private GameObject av42cPrefab, fa26bPrefab;

    //Information collected from the server to store on the client
    private string debugInfo, playersInfo;
    private string serverName;
    private int playerCount;


    private void Start()
    {
        client = gameObject.AddComponent<UnityClient>();
        client.MessageReceived += MessageReceived;
        client.Disconnected += Disconnected;
    }
    private void Update()
    {
        if (mod.state == MultiplayerMod.ConnectionState.Connected)
        {
            //If the client is connected we want to update this information on the main screen
            debugInfo = @"Multiplayer Info
Server Name: " + serverName + @"
Player Count: " + playerCount.ToString();
        }
    }
    private void OnGUI()
    {
        //Displaying different UI at different states
        switch (mod.state)
        {
            case MultiplayerMod.ConnectionState.Connecting:
                GUIConnecting();
                break;
            case MultiplayerMod.ConnectionState.Connected:
                GUIConnected();
                break;
            case MultiplayerMod.ConnectionState.Failed:
                GUIFailed();
                break;
        }
    }
    private void GUIConnecting()
    {
        GUI.Label(new Rect(0, 0, 100, 20), "Connecting to Server...");
    }
    private void GUIConnected()
    {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Connected \n" + debugInfo + "\n" + playersInfo);
    }
    private void GUIFailed()
    {
        GUI.Label(new Rect(0, 0, 100, 20), "Failed Connecting, please restart your game");
    }
    private void Disconnected(object sender, DisconnectedEventArgs e)
    {
        Console.Log("Disconnecting");

        Receiver[] receivers = FindObjectsOfType<Receiver>();
        Console.Log("Receivers Count:" + receivers.Length);
        for (int i = 0; i < receivers.Length; i++)
        {
            receivers[i].DestoryReceiver();
        }

        Sender[] senders = FindObjectsOfType<Sender>();
        Console.Log("Senders Count:" + senders.Length);
        for (int i = 0; i < senders.Length; i++)
        {
            Destroy(senders[i].gameObject);
        }
        Console.Log("Disconnected");
    }
    private void ConnectToServer()
    {
        mod.state = MultiplayerMod.ConnectionState.Connecting;
        try
        {
            client.Connect(IPAddress.Parse("109.158.178.214"), 4296, DarkRift.IPVersion.IPv4); //This causes an error if it doesn't connect
            //If it errros it won't get to the connected method
            Connected();
        }
        catch (Exception e)
        {
            Console.Log("Failed to Connect to server \n" + e.Message);
            mod.state = MultiplayerMod.ConnectionState.Failed;
        }
        
    }
    private void Connected()
    {
        mod.state = MultiplayerMod.ConnectionState.Connected;
        //Sending the players information to the server
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(mod.pilotName);
            writer.Write(mod.vehicle == MultiplayerMod.Vehicle.AV42C ? "AV-42c" : "F/A-26B");
            using (Message message = Message.Create((ushort)Tags.PlayersInfo, writer))
            {
                client.SendMessage(message, SendMode.Reliable);
                Console.Log("Told the server about our player");
            }

        }

        StartCoroutine(DelayFindBody());
        SetPrefabs();
    }
    private void SetPrefabs()
    {
        av42cPrefab = VTResources.GetPlayerVehicle("AV-42C").vehiclePrefab;
        fa26bPrefab = VTResources.GetPlayerVehicle("F/A-26B").vehiclePrefab;
        if (!av42cPrefab)
            Console.Log("Couldn't find the prefab for the AV-42C");
        if (!fa26bPrefab)
            Console.Log("Couldn't find the prefab for the F/A-26B");
    }
    private IEnumerator DelayFindBody()
    {
        //This has a delay to wait for everything to spawn in the game world first,so then we can find it
        yield return new WaitForSeconds(3);
        //The scene should be loaded by then

        if (VRDevice.model.Contains("Oculus"))
        {
            Console.Log("This is a Oculus User");
            FindRiftTouch();
        }
        else
        {
            Console.Log("This is a Vive User");
            FindViveWands();
        }

        VRHead camera = FindObjectOfType<VRHead>();
        if (camera)
        {
            Console.Log("Found the VR Camera");
            camera.gameObject.AddComponent<PlayerHeadNetworkedObjectSender>().client = client;
        }
        else
        {
            Console.Log("Looking for cameras");
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera item in cameras)
            {
                if (item.enabled && item.gameObject.activeInHierarchy)
                {
                    Console.Log("Found a camera, lets use this one");
                    item.gameObject.AddComponent<PlayerHeadNetworkedObjectSender>().client = client;
                }
            }
        }

        //Finding Vehicle
        FindPlayersObjects();
    }
    private void FindPlayersObjects()
    {
        //This is going to be searching for object in the scene that needed to be spawned in by the game
        //and the found to sync across the network
        GameObject vehicle = GameObject.Find("VTOL4(Clone)");
        if (vehicle)
        {
            Console.Log("Found Vehicle");
            vehicle.AddComponent<BasicVehicleNetworkedObjectSender>().client = client;
        }
    }
    private void FindViveWands()
    {
        SteamVR_TrackedController[] controllers = FindObjectsOfType<SteamVR_TrackedController>();
        Console.Log("There are " + controllers.Length + " vive controllers found");

        if (controllers.Length >= 1)
            controllers[0].gameObject.AddComponent<PlayerHandLeftNetworkedObjectSender>().client = client;
        if (controllers.Length >= 2)
            controllers[1].gameObject.AddComponent<PlayerHandRightNetworkedObjectSender>().client = client;
    }
    private void FindRiftTouch()
    {
        RiftTouchController[] controllers = FindObjectsOfType<RiftTouchController>();
        Console.Log("There are " + controllers.Length + " rift controllers found");

        if (controllers.Length >= 1)
            controllers[0].gameObject.AddComponent<PlayerHandLeftNetworkedObjectSender>().client = client;
        if (controllers.Length >= 2)
            controllers[1].gameObject.AddComponent<PlayerHandRightNetworkedObjectSender>().client = client;
    }
    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                ushort tag = (ushort)message.Tag;
                switch (tag)
                {
                    case (ushort)Tags.SpawnPlayerTag:
                        while (reader.Position < reader.Length)
                        {
                            ushort id = 0;
                            try
                            {
                                id = reader.ReadUInt16();
                            }
                            catch (Exception error)
                            {
                                Console.Log("Reading the ID of the new player to spawn caused an error \n" + error.Message);
                            }

                            GameObject HandLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            GameObject HandRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            GameObject Head = GameObject.CreatePrimitive(PrimitiveType.Cube);

                            HandLeft.GetComponent<Collider>().enabled = false;
                            HandRight.GetComponent<Collider>().enabled = false;
                            Head.GetComponent<Collider>().enabled = false;

                            PlayerHandLeftNetworkedObjectReceiver leftReceiver = HandLeft.AddComponent<PlayerHandLeftNetworkedObjectReceiver>();
                            PlayerHandRightNetworkedObjectReceiver rightRecevier = HandRight.AddComponent<PlayerHandRightNetworkedObjectReceiver>();
                            PlayerHeadNetworkedObjectReceiver headReceiver = Head.AddComponent<PlayerHeadNetworkedObjectReceiver>();

                            leftReceiver.client = client;
                            rightRecevier.client = client;
                            headReceiver.client = client;

                            leftReceiver.SetReceiver();
                            rightRecevier.SetReceiver();
                            headReceiver.SetReceiver();

                            leftReceiver.id = id;
                            rightRecevier.id = id;
                            headReceiver.id = id;

                            HandLeft.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            HandRight.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            Head.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                            Console.Log(string.Format("Spawned Player, ID:{0}", id));
                        }
                        break;
                    case (ushort)Tags.ServerInfo:
                        while (reader.Position < reader.Length)
                        {
                            try
                            {
                                serverName = reader.ReadString();
                                playerCount = reader.ReadInt32();
                            }
                            catch (Exception error)
                            {
                                Console.Log("Reading the server info caused an error (TAG = " + (ushort)Tags.ServerInfo + " \n" + error.Message);
                            }

                            Console.Log("Received Updated Server Info");
                        }
                        break;
                    case (ushort)Tags.PlayersInfo:
                        ReceivedPlayerInfo(reader);
                        break;
                }
            }
        }
    }
    private void ReceivedPlayerInfo(DarkRiftReader reader)
    {
        Console.Log("Received Players Info");
        while (reader.Position < reader.Length)
        {
            int playersCount = reader.ReadInt32();
            playersInfo = "\nPlayers Info:";
            for (int i = 0; i < playersCount; i++)
            {
                string playerName = reader.ReadString();
                string playerVehicle = reader.ReadString();
                playersInfo += "\n" + playerName + "\nVehicle: " + playerVehicle + "\n";
            }
        }
    }
}