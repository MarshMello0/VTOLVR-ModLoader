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
    private bool isConnected;

    private string debugInfo;
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
    }

    private void Update()
    {
        debugInfo = @"Multiplayer Info
Server Name: " + serverName + @"
Player Count: " + playerCount.ToString();
    }

    private void Connected()
    {
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
        if (!camera)
        {
            Console.Log("Found the VR Camera");
            camera.gameObject.AddComponent<PlayerHeadNetworkedObjectSender>().client = client;
        }
        else
        {
            Console.Log("Looking for normal Camera as VR one is missing");
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera item in cameras)
            {
                if (item.enabled)
                {
                    Console.Log("Found a normal camera which is enabled, using that one");
                    item.gameObject.AddComponent<PlayerHeadNetworkedObjectSender>().client = client;
                }
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
        if (false && GUI.Button(new Rect(500,500,100,100), "Info"))
        {
            Debug.Log(SceneManager.sceneCountInBuildSettings);
            Debug.Log("Current Scene Name " + SceneManager.GetActiveScene().name + " : " + SceneManager.GetActiveScene().buildIndex);
            Console.Log(string.Format("Pilot:{3}, Campaign:{0}, Scenario:{1}, Vehicle{2}",
               PilotSaveManager.currentCampaign.campaignID, PilotSaveManager.currentScenario.scenarioID,
               PilotSaveManager.currentVehicle.name, PilotSaveManager.current.pilotName));

        }
        if (false && GUI.Button(new Rect(600, 500, 100, 100), "7"))
        {
            Console.Log("Setting Pilot");
            PilotSaveManager.LoadPilotsFromFile();
            PilotSaveManager.current = PilotSaveManager.pilots["Ben"];
            Console.Log("Going though All built in campaigns");
            foreach (VTCampaignInfo info in VTResources.GetBuiltInCampaigns())
            {
                if (info != null && info.campaignID != null && info.campaignID == "Quick Flight")
                {
                    Console.Log("Setting Campaign");
                    PilotSaveManager.currentCampaign = info.ToIngameCampaign();
                    Console.Log("Setting Vehicle");
                    PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(info.vehicle);
                }
            }
            Console.Log("Going though All missions in that campaign");
            foreach (CampaignScenario cs in PilotSaveManager.currentCampaign.missions)
            {
                if (cs != null && cs.scenarioID != null && cs.scenarioID == "Free Flight")
                {
                    Console.Log("Setting Scenario");
                    PilotSaveManager.currentScenario = cs;
                }
            }
           
            Console.Log(string.Format("Loading into Scene 7 with Test Pilot, Campaign:{0}, Scenario:{1}, Vehicle{2}",
                PilotSaveManager.currentCampaign.campaignName, PilotSaveManager.currentScenario.scenarioName,
                PilotSaveManager.currentVehicle.vehicleName));
            SceneManager.LoadScene(7);
        }

        if (isConnected)
        {
            GUI.Label(new Rect(110, 0, 100, 20), "Connected");
            if (GUI.Button(new Rect(0,0,100,20),"Disconnect"))
            {
                client.Disconnect();
                isConnected = false;
            }

            GUI.Label(new Rect(0, 30, Screen.width, 400), debugInfo);
        }
        else
        {
            GUI.Label(new Rect(110, 0, 100, 20), "Not Connected");
            if (GUI.Button(new Rect(0, 0, 100, 20), "Retry"))
            {
                try
                {
                    Console.Log("Connecting");
                    client.Connect(IPAddress.Parse("109.158.178.214"), 4296, DarkRift.IPVersion.IPv4);
                    Console.Log("Connected");
                    isConnected = true;
                }
                catch (Exception e)
                {
                    Console.Log("Error when Connecting \n " + e.Message);
                }
                
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
                }
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
    DestroyPlayer
}
