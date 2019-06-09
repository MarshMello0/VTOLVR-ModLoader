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
using NetworkedObjects.Players;
using NetworkedObjects.Vehicles;


public class NetworkingManager : MonoBehaviour
{
    //This handles all of the multiplayer code in the game scene

    private UnityClient client;
    public MultiplayerMod mod;

    //These are things used to spawn other clients in
    private GameObject av42cPrefab, fa26bPrefab;

    //Information collected from the server to store on the client
    private string debugInfo, playerListString;
    public List<Player> playersInfo = new List<Player>();
    private string serverName;
    private int playerCount;

    //Settings - These are settings which should be changed only when doing an update to the mod, so everyone has the same
    private bool syncBody = false;

    private void Start()
    {
        client = gameObject.AddComponent<UnityClient>();
        client.MessageReceived += MessageReceived;
        client.Disconnected += Disconnected;

        ConnectToServer();
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
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Connecting to Server... \n(The game will freeze if it can't find it, but will come back and responce after it failed)");
    }
    private void GUIConnected()
    {
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Connected \n" + debugInfo + "\n" + playerListString);
    }
    private void GUIFailed()
    {
        GUI.Label(new Rect(0, 0, 100, 20), "Failed Connecting, please restart your game");
    }
    public void UpdatePlayerListString()
    {
        playerListString = "Player List:";
        foreach (Player player in playersInfo)
        {
            playerListString += "\n" + player.pilotName + " [" + player.id + "] : " + player.vehicle.ToString() +
                "\nPosition:" + player.GetPosition() + " Rotation:" + player.GetRotation().eulerAngles +
                "\nSpeed:" + player.speed + " Land Gear:" + player.landingGear + " Flaps:" + player.flaps + 
                "\nThrusters Angle:" + player.thrusterAngle;
        }
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
        SetPrefabs();
        StartCoroutine(DelayFindBody());
    }
    private void SetPrefabs()
    {
        #region Players Vehicles Prefabs
        /*
        //We need these prefabs to spawn the other players in for this client
        av42cPrefab = VTResources.GetPlayerVehicle("AV-42C").vehiclePrefab;
        fa26bPrefab = VTResources.GetPlayerVehicle("F/A-26B").vehiclePrefab;
        */
        #endregion

        UnitCatalogue.UpdateCatalogue();
        av42cPrefab = UnitCatalogue.GetUnitPrefab("AV-42CAI");
        fa26bPrefab = UnitCatalogue.GetUnitPrefab("FA-26B AI");
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
        if (syncBody)
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
        }
        //Finding Vehicle
        FindPlayersObjects();
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
    private void FindPlayersObjects()
    {
        //This is going to be searching for object in the scene that needed to be spawned in by the game
        //and the found to sync across the network
        GameObject vehicle = GameObject.Find("VTOL4(Clone)");
        if (vehicle)
        {
            Console.Log("Found Vehicle");
            vehicle.AddComponent<AV42cNetworkedObjectSender>().client = client;
        }
        PlayerReady();
    }
    private void PlayerReady()
    {
        //This is when the client is ready to tell everyone that we have joined and receive everyone elses information

        //Sending the players information to the server
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(mod.pilotName);
            writer.Write(mod.vehicle == MultiplayerMod.Vehicle.AV42C ? "AV-42c" : "F/A-26B");

            using (Message message = Message.Create((ushort)Tags.SpawnPlayerTag, writer))
            {
                client.SendMessage(message, SendMode.Reliable);
                Console.Log("Told the server about our player");
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
                        ReceivedNewPlayer(reader);
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
    private void ReceivedNewPlayer(DarkRiftReader reader)
    {
        Console.Log("Received New Player");
        while (reader.Position < reader.Length)
        {
            int amount = reader.ReadInt32();

            for (int i = 0; i < amount; i++)
            {
                ushort id = reader.ReadUInt16();
                string pilotName = reader.ReadString();
                string vehicle = reader.ReadString();
                MultiplayerMod.Vehicle vehicleEnum = MultiplayerMod.Vehicle.FA26B;
                if (vehicle == "AV-42c")
                    vehicleEnum = MultiplayerMod.Vehicle.AV42C;

                SpawnPlayer(id, pilotName, vehicleEnum);
            }
            UpdatePlayerListString();
        }
    }
    private void SpawnPlayer(ushort id, string pilotName, MultiplayerMod.Vehicle vehicle)
    {
        Player newPlayer = new Player(id, pilotName, vehicle);
        playersInfo.Add(newPlayer);

        //This will spawn all of the correct assets needed to display a player over the network

        if (syncBody)
        {
            //Spawning the players Body
            GameObject HandLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject HandRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject Head = GameObject.CreatePrimitive(PrimitiveType.Cube);

            //During testing these colliders, where colliding with the plane and insta killing me
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
        }

        //Spawning the Vehicle
        GameObject vehicleGO = Instantiate(vehicle == MultiplayerMod.Vehicle.AV42C ? av42cPrefab : fa26bPrefab); //Probally cause null errors
        try
        {
            vehicleGO.GetComponent<AIAircraftSpawn>().enabled = false;
            vehicleGO.GetComponent<Actor>().enabled = false;
            vehicleGO.GetComponent<Health>().enabled = false;
            vehicleGO.GetComponent<SimpleDrag>().enabled = false;
            vehicleGO.GetComponent<MissileDetector>().enabled = false;
            vehicleGO.GetComponent<VehicleFireDeath>().enabled = false;
            vehicleGO.GetComponent<ChaffCountermeasure>().enabled = false;
            vehicleGO.GetComponent<CountermeasureManager>().enabled = false;
            vehicleGO.GetComponent<Tailhook>().enabled = false;
            vehicleGO.GetComponent<AeroController>().enabled = false;
            vehicleGO.GetComponent<RCSController>().enabled = false;
            vehicleGO.GetComponent<FuelTank>().enabled = false;
            vehicleGO.GetComponent<FlareCountermeasure>().enabled = false;
            vehicleGO.GetComponent<MassUpdater>().enabled = false;
            vehicleGO.GetComponent<FlightInfo>().enabled = false;
            vehicleGO.GetComponent<AudioUpdateModeSetter>().enabled = false;
            vehicleGO.GetComponent<WeaponManager>().enabled = false;
            vehicleGO.GetComponent<FlightAssist>().enabled = false;
            vehicleGO.GetComponent<VehiclePart>().enabled = false;
            vehicleGO.GetComponent<AirBrakeController>().enabled = false;
            vehicleGO.GetComponent<TiltController>().enabled = false;
            vehicleGO.GetComponent<AutoPilot>().enabled = false;
            vehicleGO.GetComponent<AirFormationLeader>().enabled = false;
            vehicleGO.GetComponent<AIPilot>().enabled = false;
            vehicleGO.GetComponent<KinematicPlane>().enabled = false;
            vehicleGO.GetComponent<AIPlaneConfigurator>().enabled = false;
            //vehicleGO.GetComponent<WheelsController>().enabled = false;
            vehicleGO.GetComponent<VTOLAutoPilot>().enabled = false;
            vehicleGO.GetComponent<LODBase>().enabled = false;
            vehicleGO.GetComponent<LODRenderer>().enabled = false;
            vehicleGO.GetComponent<LODObject>().enabled = false;
            vehicleGO.GetComponent<AudioUpdateModeSetter>().enabled = false;
            vehicleGO.GetComponent<WingMaster>().enabled = false;
            vehicleGO.GetComponent<RadarCrossSection>().enabled = false;
            
            /*
            Console.Log("Searching for Colldiers on vehicle");

            BoxCollider[] cols = vehicleGO.GetComponentsInChildren(typeof(BoxCollider)) as BoxCollider[];

            foreach (Collider collider in cols)
            {
                try
                {
                    collider.enabled = false;
                    Console.Log("Disabled collider on " + collider.name);
                }
                catch (Exception e)
                {
                    Console.Log("Failed to disable collider " + e.Message);
                }
            }
            Console.Log("Trying to get Rigidbody");
            vehicleGO.GetComponent<Rigidbody>().isKinematic = true;
            */
        }
        catch (Exception e)
        {
            Console.Log("Error: " + e.Message);
        }
        
        /* Spawning Cube
        GameObject vehicleGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicleGO.GetComponent<BoxCollider>().enabled = false;
        vehicleGO.transform.localScale = new Vector3(10, 10, 10);
        */

        /*
        //Trying to stop the player moving there
        vehicleGO.GetComponent<FloatingOriginShifter>().enabled = false;
        vehicleGO.GetComponent<FloatingOriginTransform>().enabled = false;
        

        vehicleGO.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
        */

        AV42cNetworkedObjectReceiver vehicleReceiver = vehicleGO.AddComponent<AV42cNetworkedObjectReceiver>();

        vehicleReceiver.client = client;
        vehicleReceiver.manager = this;
        vehicleReceiver.player = newPlayer;

        vehicleReceiver.SetReceiver();

        vehicleReceiver.id = id;

        Console.Log(string.Format("Spawned {0} [{1}] with vehicle {2}", pilotName, id, vehicle.ToString()));
    }
}