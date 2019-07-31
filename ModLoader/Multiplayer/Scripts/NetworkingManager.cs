using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using NetworkedObjects.Players;
using ModLoader.Multiplayer.NetworkedObjects.Vehicles;
using TMPro;

namespace ModLoader.Multiplayer
{
    public class NetworkingManager : MonoBehaviour
    {
        //This handles all of the multiplayer code in the game scene

        public UnityClient client;
        private ModLoaderManager manager;
        public string pilotName;

        //These are things used to spawn other clients in and update them
        private GameObject av42cPrefab, fa26bPrefab;
        private Transform worldCenter;

        //Information collected from the server to store on the client
        private string debugInfo, playerListString;
        public List<Player> playersInfo = new List<Player>();
        private string serverName;
        private int playerCount;

        //Settings - These are settings which should be changed only when doing an update to the mod, so everyone has the same
        private bool syncBody = false;

        //Things used for other features
        private Health vehicleHeath; //Used to check if the vehicle crashed and died
        private BlackoutEffect blackoutEffect; //Used to check if the player died by G Forces

        private void Start()
        {
            manager = ModLoaderManager.instance;
            client = manager.GetUnityClient();
            StartCoroutine(StartProcedureEnumerator());
        }
        private void Update()
        {
            if (false)
            {
                //If the client is connected we want to update this information on the main screen
                debugInfo = @"Multiplayer Info
Server Name: " + serverName + @"
Player Count: " + playerCount.ToString();
            }
        }
        public void UpdatePlayerListString()
        {
            playerListString = "Player List:";
            foreach (Player player in playersInfo)
            {
                playerListString += "\n" + player.pilotName + " [" + player.id + "] : " + player.vehicle.ToString() +
                    "\nPosition:" + player.GetPosition() + " Rotation:" + player.GetRotation().eulerAngles +
                    "\nSpeed:" + player.speed + " Land Gear:" + player.landingGear + " Flaps:" + player.flaps +
                    "\nPitch, Yaw, Roll" + player.GetPitchYawRoll() + " Breaks:" + player.breaks + " Throttle:" + player.throttle + " Wheels:" + player.wheels;
                if (player.vehicle == ModLoader.Vehicle.AV42C)
                {
                    playerListString += "\nThrusters Angle:" + player.thrusterAngle;
                }

            }
        }



        private IEnumerator StartProcedureEnumerator()
        {
            yield return StartCoroutine(SetPrefabs());

            //This has a delay to wait for everything to spawn in the game world first,so then we can find it
            yield return new WaitForSeconds(4);
            //The scene should be loaded by then

            if (syncBody)
                yield return StartCoroutine(DelayFindBody());

            Console.Log("Creating Zero Reference");
            yield return StartCoroutine(CreateZeroReference());

            //Finding Vehicle
            Console.Log("Searching for players vehicle");
            yield return StartCoroutine(FindPlayersObjects());
            Console.Log("Searching for players scripts");
            yield return StartCoroutine(StorePlayersScripts());
            Console.Log("Done the Start Procedure, telling server we are ready");
            PlayerReady();
        }
        private IEnumerator SetPrefabs()
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

            yield break;
        }
        private IEnumerator DelayFindBody()
        {
            Console.Log("Syncing Body");
            if (XRDevice.model.Contains("Oculus"))
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
            yield break;
        }
        private IEnumerator CreateZeroReference()
        {
            //Used to workout the offset when sending pos over network and receiving
            GameObject cube = new GameObject("[Multiplayer] World Center", typeof(FloatingOriginTransform));
            cube.transform.position = new Vector3(0, 0, 0);
            worldCenter = cube.transform;
            yield break;
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
        private IEnumerator FindPlayersObjects()
        {
            //This is going to be searching for object in the scene that needed to be spawned in by the game
            //and the found to sync across the network
            GameObject vehicle = GameObject.Find(manager.multiplayerVehicle == ModLoader.Vehicle.AV42C ? "VTOL4(Clone)" : "FA-26B(Clone)");
            if (vehicle)
            {
                if (manager.multiplayerVehicle == ModLoader.Vehicle.AV42C)
                {
                    AV42cNetworkedObjectSender sender = vehicle.AddComponent<AV42cNetworkedObjectSender>();
                    sender.client = client;
                    sender.worldCenter = worldCenter;

                    Console.Log("Found the AV42C");
                }
                else
                {
                    FA26BNetworkedObjectSender sender = vehicle.AddComponent<FA26BNetworkedObjectSender>();
                    sender.client = client;
                    sender.worldCenter = worldCenter;
                    Console.Log("Found the FA26B");
                }
                //vehicle.GetComponent<Health>().minDamage = float.MaxValue;//God Mode
            }

            yield break;
        }
        private IEnumerator StorePlayersScripts()
        {
            //This is finding and storing scripts which will be used later
            GameObject vehicle = GameObject.Find(manager.multiplayerVehicle == ModLoader.Vehicle.AV42C ? "VTOL4(Clone)" : "FA-26B(Clone)");
            if (vehicle)
            {
                vehicleHeath = vehicle.GetComponent<Health>();
                vehicleHeath.OnDeath.AddListener(VehicleLocalDeath);
                blackoutEffect = vehicle.GetComponentInChildren<BlackoutEffect>();
                blackoutEffect.OnAccelDeath.AddListener(PlayerLocalDeath);
            }
            yield break;
        }
        private void PlayerReady()
        {
            //This is when the client is ready to tell everyone that we have joined and receive everyone elses information

            //Temp
            GameObject fuel = FindObjectOfType<RefuelPlane>().gameObject;
            Texture2D customTexture = ModLoaderManager.instance.assets.LoadAsset<Texture2D>("tex_refuelPlane-Custom");
            if (fuel)
            {
                Console.Log("Found the vehicle " + fuel.name);
                fuel = fuel.transform.Find("refuelPlane2").gameObject;
                fuel.transform.Find("bodyCylinder").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("wings").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("vertStab").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("tailPlanes").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("outFlapLeft").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("inFlapLeft").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("inFlapRight").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("outFlapRight").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("elevatorLeft").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("elevatorRight").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("rudder").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("aileronLeft").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
                fuel.transform.Find("aileronRight").GetComponent<MeshRenderer>().material.mainTexture = customTexture;
            }
            
            //Sending the players information to the server
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(pilotName);
                writer.Write(manager.multiplayerVehicle == ModLoader.Vehicle.AV42C ? "AV-42c" : "F/A-26B");

                using (Message message = Message.Create((ushort)Tags.SpawnPlayerTag, writer))
                {
                    client.SendMessage(message, SendMode.Reliable);
                    Console.Log("Told the server about our player");
                }
            }



        }

        public void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage())
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    ushort tag = (ushort)message.Tag;
                    switch (tag)
                    {
                        case (ushort)Tags.PlayerDeath:
                            PlayerNetworkDeath(reader);
                            break;
                        case (ushort)Tags.VehicleDeath:
                            VehicleNetworkDeath(reader);
                            break;
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
                    ModLoader.Vehicle vehicleEnum = ModLoader.Vehicle.FA26B;
                    if (vehicle == "AV-42c")
                        vehicleEnum = ModLoader.Vehicle.AV42C;

                    SpawnPlayer(id, pilotName, vehicleEnum);
                }
                UpdatePlayerListString();
            }
        }
        private void SpawnPlayer(ushort id, string pilotName, ModLoader.Vehicle vehicle)
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

            GameObject vehicleGO = Instantiate(vehicle == ModLoader.Vehicle.AV42C ? av42cPrefab : fa26bPrefab);
            vehicleGO.GetComponent<AIPilot>().startLanded = true;
            vehicleGO.transform.position += new Vector3(0, 10, 0);
            Console.Log("Enabling God Mode");
            vehicleGO.GetComponent<Health>().minDamage = float.MaxValue; //God Mode for this vehicle

            vehicleGO.name = "[Multiplayer] Player: " + pilotName;

            if (vehicle == ModLoader.Vehicle.AV42C)
            {
                Console.Log("This new player is using AV42-c");
                AV42cNetworkedObjectReceiver vehicleReceiver = vehicleGO.AddComponent<AV42cNetworkedObjectReceiver>();

                vehicleReceiver.client = client;
                vehicleReceiver.manager = this;
                vehicleReceiver.player = newPlayer;
                vehicleReceiver.worldCenter = worldCenter;

                vehicleReceiver.SetReceiver();

                vehicleReceiver.id = id;
            }
            else
            {
                Console.Log("This new player is using FA26-B");
                FA26BNetworkedObjectReceiver vehicleReceiver = vehicleGO.AddComponent<FA26BNetworkedObjectReceiver>();

                vehicleReceiver.client = client;
                vehicleReceiver.manager = this;
                vehicleReceiver.player = newPlayer;
                vehicleReceiver.worldCenter = worldCenter;

                vehicleReceiver.SetReceiver();

                vehicleReceiver.id = id;
            }





            //Spawning Players Name

            /*
            try
            {
                GameObject text = new GameObject(pilotName);
                TextMeshPro tm = text.AddComponent<TextMeshPro>();
                tm.text = pilotName;
                tm.fontSize = 100;
                text.transform.position = new Vector3(vehicleGO.transform.position.x, vehicleGO.transform.position.y + 10, vehicleGO.transform.position.z);
                text.transform.SetParent(vehicleGO.transform);
            }
            catch (Exception )
            {
                Console.Log("Error on text");
            }
            */


            Console.Log(string.Format("Spawned {0} [{1}] with vehicle {2}", pilotName, id, vehicle.ToString()));
            FlightLogger.Log(pilotName + " has joined the game using " + vehicle.ToString());
        }

        private void VehicleLocalDeath()
        {
            //This runs when the vehicle took too much damage and dies

            //Telling the server that we died
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                string arg = vehicleHeath.killedByActor ? vehicleHeath.killedByActor.actorName : "Environment";
                writer.Write(string.Format("{0} was killed by {1}. {2}", pilotName, arg, vehicleHeath.killMessage));
                using (Message message = Message.Create((ushort)Tags.VehicleDeath, writer))
                {
                    client.SendMessage(message, SendMode.Reliable);
                }
            }
        }
        private void VehicleNetworkDeath(DarkRiftReader reader)
        {
            //This runs when someone on the network crashed
            while (reader.Position < reader.Length)
            {
                ushort id = reader.ReadUInt16();
                string deathMessage = reader.ReadString();

                FlightLogger.Log(deathMessage);
            }
        }

        private void PlayerLocalDeath()
        {
            //This runs when the player dies by G Forces
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(string.Format("{0} was killed by G Force", pilotName));

                using (Message message = Message.Create((ushort)Tags.PlayerDeath, writer))
                {
                    client.SendMessage(message, SendMode.Reliable);
                }
            }
        }

        private void PlayerNetworkDeath(DarkRiftReader reader)
        {
            //This runs when someone on the network player died
            while (reader.Position < reader.Length)
            {
                ushort id = reader.ReadUInt16();
                string deathMessage = reader.ReadString();

                FlightLogger.Log(deathMessage);
            }
        }
    }
}
