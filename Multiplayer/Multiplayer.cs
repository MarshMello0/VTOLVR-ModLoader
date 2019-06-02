using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DarkRift.Client;
using DarkRift;
using System.Net;
using NetworkedObjects.Player;
using System.Collections;
using DarkRift.Client.Unity;
using UnityEngine.VR;
using UnityEngine.SceneManagement;

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
    private UnityClient client;

    private enum ConnectionState { Offline,Loading,Connecting,Connected, Failed}
    private ConnectionState state = ConnectionState.Offline;

    private enum Vehicle { FA26B,AV42C}
    private Vehicle vehicle  = Vehicle.AV42C;
    private string pilotName = "Pilot Name";

    private string debugInfo, playersInfo;
    private string serverName;
    private int playerCount;

    public static MultiplayerScript _instance;
    private void Awake()
    {
        if (!_instance)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
    }
    private void Start()
    {
        client = gameObject.AddComponent<UnityClient>();
        client.MessageReceived += MessageReceived;
        client.Disconnected += Disconnected;

        PilotSaveManager.LoadPilotsFromFile();
    }
    private void Update()
    {
        debugInfo = @"Multiplayer Info
Server Name: " + serverName + @"
Player Count: " + playerCount.ToString();
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
    private void OnGUI()
    {
        switch (state)
        {
            case ConnectionState.Offline:
                GUIOffline();
                break;
            case ConnectionState.Loading:
                GUILoading();
                break;
            case ConnectionState.Connecting:
                GUIConnecting();
                break;
            case ConnectionState.Connected:
                GUIConnected();
                break;
            case ConnectionState.Failed:
                GUIFailed();
                break;
        }
    }

    private void GUIOffline()
    {
        GUI.Label(new Rect(0, 0, 100, 20), "Offline");
        if (vehicle == Vehicle.AV42C && GUI.Button(new Rect(100, 0, 100, 20), "AV-42C"))
        {
            vehicle = Vehicle.FA26B;
            Console.Log("Switched player's vehicle to F/A-26B");
        }
        else if (vehicle == Vehicle.FA26B && GUI.Button(new Rect(100, 0, 100, 20), "F/A-26B"))
        {
            vehicle = Vehicle.AV42C;
            Console.Log("Switched player's vehicle to AV-42C");
        }

        pilotName = GUI.TextField(new Rect(210, 0, 100, 20), pilotName);

        if (GUI.Button(new Rect(320, 0, 100, 20), "Connect"))
        {
            if (!CheckIfPilotExists(pilotName))
            {
                Console.Log("Pilot \"" + pilotName + "\" doesn't exist");
                return;
            }
            StartCoroutine(LoadLevel());
        }
    }

    private void GUILoading()
    {
        GUI.Label(new Rect(0, 0, 100, 20), "Game Loading");
    }

    private void GUIConnecting()
    {
        GUI.Label(new Rect(0, 0, 100, 20), "Connecting To Server");
    }

    private void GUIConnected()
    {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Connected \n" + debugInfo + "\n" + playersInfo);
    }

    private void GUIFailed()
    {
        GUI.Label(new Rect(0, 0, 100, 20), "Failed Connecting, please restart your game");
    }

    private bool CheckIfPilotExists(string name)
    {
        return PilotSaveManager.pilots.ContainsKey(name);
    }

    private IEnumerator LoadLevel()
    {
        state = ConnectionState.Loading;
        Console.Log("Connection State == Loading");
        VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
        LoadingSceneController.LoadScene(7);

        yield return new WaitForSeconds(5);
        Console.Log("Setting Information");


        Console.Log("Setting Pilot");
        PilotSaveManager.current = PilotSaveManager.pilots[pilotName];
        Console.Log("Going though All built in campaigns");
        if (VTResources.GetBuiltInCampaigns() != null)
        {
            foreach (VTCampaignInfo info in VTResources.GetBuiltInCampaigns())
            {

                if (vehicle == Vehicle.AV42C && info.campaignID == "av42cQuickFlight")
                {
                    Console.Log("Setting Campaign");
                    PilotSaveManager.currentCampaign = info.ToIngameCampaign();
                    Console.Log("Setting Vehicle");
                    PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(info.vehicle);
                    break;
                }

                if (vehicle == Vehicle.FA26B && info.campaignID == "fa26bFreeFlight")
                {
                    Console.Log("Setting Campaign");
                    PilotSaveManager.currentCampaign = info.ToIngameCampaign();
                    Console.Log("Setting Vehicle");
                    PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(info.vehicle);
                    break;
                }
            }
        }
        else
            Console.Log("Campaigns are null");

        Console.Log("Going though All missions in that campaign");
        foreach (CampaignScenario cs in PilotSaveManager.currentCampaign.missions)
        {
            Console.Log("CampaignScenario == " + cs.scenarioID);
            if (cs.scenarioID == "freeFlight" || cs.scenarioID == "Free Flight")
            {
                Console.Log("Setting Scenario");
                PilotSaveManager.currentScenario = cs;
                break;
            }
        }

        VTScenario.currentScenarioInfo = VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign);

        Console.Log(string.Format("Loading into game, Pilot:{3}, Campaign:{0}, Scenario:{1}, Vehicle:{2}",
            PilotSaveManager.currentCampaign.campaignName, PilotSaveManager.currentScenario.scenarioName,
            PilotSaveManager.currentVehicle.vehicleName, pilotName));

        LoadingSceneController.instance.PlayerReady(); //<< Auto Ready
        StartCoroutine(ConnectToServer());
    }

    private IEnumerator ConnectToServer()
    {
        while (SceneManager.GetActiveScene().buildIndex != 7)
        {
            //Loop to wait till the active scene is switched
            yield return null;
        }
        state = ConnectionState.Connecting;
        Console.Log("Connection State == Connecting");

        try
        {
            client.Connect(IPAddress.Parse("109.158.178.214"), 4296, DarkRift.IPVersion.IPv4);
        }
        catch (Exception e)
        {
            Console.Log("Failed to Connect to server \n" + e.Message);
            state = ConnectionState.Failed;
            Console.Log("Connection State == Failed");
        }
        Connected();
    }

    private void Connected()
    {
        state = ConnectionState.Connected;
        Console.Log("Connection State == Connected");

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

        //Sending the players information to the server

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(pilotName);
            writer.Write(vehicle == Vehicle.AV42C ? "AV-42c" : "F/A-26B");
            using (Message message = Message.Create((ushort)Tags.PlayersInfo, writer))
            {
                client.SendMessage(message, SendMode.Reliable);
                Console.Log("Told the server about our player");
            }
                
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
                            Head.transform.localScale = new Vector3(0.1f, 1f, 0.1f);

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

public static class Console
{
    public static void Log(object message)
    {
        Debug.Log("Multiplayer Mod: " + message);
    }
}
public enum Tags
{
    SpawnPlayerTag,
    PlayerHandLeft_Movement, PlayerHandLeft_Rotation,
    PlayerHandRight_Movement, PlayerHandRight_Rotation,
    PlayerHead_Movement, PlayerHead_Rotation,
    ServerInfo,
    DestroyPlayer,
    PlayersInfo
}
