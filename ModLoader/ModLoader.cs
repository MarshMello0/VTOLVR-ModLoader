using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using Steamworks;
using ModLoader.Multiplayer;
using DarkRift.Client.Unity;
using DarkRift;
using System.Net;
using DarkRift.Client;
using System.Reflection;

namespace ModLoader
{
    [Info("ModLoader","","")]
    public class ModLoader : VTOLMOD
    {
        private ModLoaderManager manager;
        private CSharp csharp;
        private VTOLAPI api;
        //UI Objects
        GameObject warningPage, spmp, sp, mp, spModPage, spList, mpPV, mpIPPort, mpServerInfo, mpBanned;
        PoseBounds pb;
        public enum Page { warning, spmp,spMod,spList,mpPV,mpIPPort, mpServerInfo, mpBanned}


        #region Multiplayer Variables
        //Multiplayer

        //This is the state which the client is currently in
        public enum ConnectionState { Offline, Connecting, Lobby, Loading, InGame }
        public ConnectionState state = ConnectionState.Offline;

        //This is the information about what the player has chosen
        public enum Vehicle { FA26B, AV42C, F45A }
        public Vehicle vehicle = Vehicle.AV42C;
        public string pilotName = "Pilot Name";


        public UnityClient client { private set; get; }
        public Text serverInfoText;
        private Text bannedReasonText;
        private string currentMap;

        #endregion

        #region Singleplayer Variables
        //Singleplayer

        private Transform spTransform;

        private string mods = @"\mods";
        private string root;

        private ModSlot[] modSlots = new ModSlot[8];
        private List list;
        private VRInteractable nextInteractable, previousInteractable, loadInteractable;
        private Text modTitleText, modDescriptionText, loadModText;
        private Material nextMaterial, previousMaterial, loadModMaterial, redMaterial, greenMaterial;
        private APIMod[] apimods = new APIMod[1];

        #endregion
        private void Start()
        {
            manager = ModLoaderManager.instance;
            csharp = CSharp.instance;
            api = VTOLAPI.instance;
            Debug.Log("" + api.GetSteamID());
            SetInGameUI();

            if (manager.doneFirstLoad)
            {
                //This user is returning from a multiplayer game
                SwitchPage(Page.mpPV);
                SetupMultiplayer();
            }
            else
            {
                //This is the first time they have loaded
                manager.doneFirstLoad = true;
                //Spawning UConsole
                GameObject uConsole = Instantiate(manager.assets.LoadAsset<GameObject>("UConsole-Canvas"));
                UConsole console = uConsole.AddComponent<UConsole>();

                /* The CS and CSFile command don't work because it won't compile the dll

                UCommand cs = new UCommand("cs", "cs <CSharp Code>");
                UCommand csfile = new UCommand("csfile", "cs <FileName>");

                cs.callbacks.Add(csharp.CS);
                csfile.callbacks.Add(csharp.CSFile);

                console.AddCommand(cs);
                console.AddCommand(csfile);
                */
                UCommand load = new UCommand("loadmod", "loadmod <modname>");
                UCommand reloadMods = new UCommand("reloadmods", "reloadmods");
                UCommand listMods = new UCommand("listmods", "listmods");
                load.callbacks.Add(ModLoaderManager.LoadCommand);
                reloadMods.callbacks.Add(ModLoaderManager.ReloadMods);
                listMods.callbacks.Add(ModLoaderManager.ListMods);

                console.AddCommand(load);
                console.AddCommand(reloadMods);
                console.AddCommand(listMods);
            }
        }

        #region Setting UI Mod Loader

        private void SetInGameUI()
        {
            //This method moves around the panel on the second scene and creates a new one

            GameObject equipPanel = GameObject.Find("/Platoons/CarrierPlatoon/AlliedCarrier/ControlPanel (1)/EquipPanel");
            Transform equipPanelT = equipPanel.transform;
            equipPanelT.parent = null;

            //Moving it up
            equipPanelT.position = new Vector3(-35.656f, 25.674f, 304.984f);
            equipPanelT.rotation = Quaternion.Euler(0, -47.13f, 60);

            //Destrying start button and background, then rotating workshop panel
            GameObject startButton = equipPanelT.GetChild(0).GetChild(1).GetChild(4).gameObject;

            VRInteractable startButtonVR = startButton.GetComponent<VRInteractable>();

            GameObject mainBG = equipPanelT.GetChild(0).GetChild(1).GetChild(0).gameObject;
            Destroy(mainBG);
            RectTransform workshopT = equipPanelT.GetChild(0).GetChild(0).GetComponent<RectTransform>();
            workshopT.localRotation = Quaternion.Euler(50, 0, 0);
            Destroy(startButton);

            //Spawning the Mod Load Menu
            GameObject canvaus = Instantiate(manager.assets.LoadAsset<GameObject>("ModLoader"));
            canvaus.transform.position = new Vector3(-36.1251f, 25.9583f, 303.7759f);
            canvaus.transform.rotation = Quaternion.Euler(-0.303f, -46.172f, -69.8f);

            //Find Objects
            Transform prefabT = canvaus.transform;
            Transform canvasT = prefabT.GetChild(0);
            warningPage = canvasT.GetChild(1).gameObject;
            spmp = canvasT.GetChild(2).gameObject;
            sp = canvasT.GetChild(3).gameObject;
            mp = canvasT.GetChild(4).gameObject;
            spModPage = sp.transform.GetChild(0).gameObject;
            spList = sp.transform.GetChild(1).gameObject;
            mpPV = mp.transform.GetChild(0).gameObject;
            mpIPPort = mp.transform.GetChild(1).gameObject;
            mpServerInfo = mp.transform.GetChild(2).gameObject;
            mpBanned = mp.transform.GetChild(3).gameObject;

            //Setting PoseBounds
            RectTransform canvasRect = canvasT.GetComponent<RectTransform>();
            pb = canvasT.gameObject.AddComponent<PoseBounds>();
            pb.pose = GloveAnimation.Poses.Point;
            pb.size = new Vector3(canvasRect.rect.width, canvasRect.rect.height, 155.0f);


            //Adding Scripts

            //Warning Page
            VRInteractable warningPagevrInteractable = warningPage.transform.GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(warningPagevrInteractable);
            warningPagevrInteractable.interactableName = "Okay";
            warningPagevrInteractable.OnInteract.AddListener(delegate { SwitchPage(Page.spmp); });
            //SP/MP
            VRInteractable spButton = spmp.transform.GetChild(1).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable mpButton = spmp.transform.GetChild(2).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(spButton);
            SetDefaultInteractable(mpButton);
            spButton.interactableName = "Start Singleplayer";
            //mpButton.interactableName = "Start Multiplayer";
            mpButton.interactableName = "Not Available Yet";
            spButton.OnInteract.AddListener(delegate { SwitchPage(Page.spList); SetupSinglePlayer(); });
            //mpButton.OnInteract.AddListener(delegate { SwitchPage(Page.mpPV); SetupMultiplayer(); });
            mpButton.OnInteract.AddListener(delegate { Console.Log("Multiplayer Button was pressed :P"); });
            //SP Mod Page
            VRInteractable spModBack = spModPage.transform.GetChild(3).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable spModLoad = spModPage.transform.GetChild(2).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(spModBack);
            SetDefaultInteractable(spModLoad);
            spModBack.interactableName = "Back to list";
            spModLoad.interactableName = "Load Mod";
            spModBack.OnInteract.AddListener(delegate { SwitchPage(Page.spList); });
            //SP List
            VRInteractable spListSwitch = spList.transform.GetChild(2).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable spListNextPage = spList.transform.GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable spListPreviousPage = spList.transform.GetChild(1).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable spListStart = spList.transform.GetChild(11).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(spListSwitch);
            SetDefaultInteractable(spListNextPage);
            SetDefaultInteractable(spListPreviousPage);
            SetDefaultInteractable(spListStart);
            spListSwitch.interactableName = "Switch";
            spListNextPage.interactableName = "Next Page";
            spListPreviousPage.interactableName = "Previous Page";
            spListStart.interactableName = "Start Game";
           
            spListStart.OnInteract.AddListener(delegate { SceneManager.LoadScene(2); });

            SetButtons(spListNextPage.gameObject, spListPreviousPage.gameObject);
            SetModPageItems(
                spModLoad,
                spModPage.transform.GetChild(0).GetComponent<Text>(),
                spModPage.transform.GetChild(1).GetComponent<Text>(),
                spModPage.transform.GetChild(2).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material,
                spModPage.transform.GetChild(2).GetChild(1).GetComponent<Text>());
            spListSwitch.OnInteract.AddListener(delegate {SwitchButton(); });
            spListNextPage.OnInteract.AddListener(delegate { NextPage(); });
            spListPreviousPage.OnInteract.AddListener(delegate { PreviousPage(); });
            //MP Pilot and Vehicle
            VRInteractable mpPVAV42 = mpPV.transform.GetChild(2).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable mpPVFA26 = mpPV.transform.GetChild(3).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable mpPVF45 = mpPV.transform.GetChild(4).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable pilot0 = mpPV.transform.GetChild(6).GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable pilot1 = mpPV.transform.GetChild(7).GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable pilot2 = mpPV.transform.GetChild(8).GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable mpPVContinue = mpPV.transform.GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(mpPVAV42);
            SetDefaultInteractable(mpPVFA26);
            SetDefaultInteractable(mpPVF45);
            SetDefaultInteractable(pilot0);
            SetDefaultInteractable(pilot1);
            SetDefaultInteractable(pilot2);
            SetDefaultInteractable(mpPVContinue);
            mpPVAV42.interactableName = "Select Vehicle";
            mpPVFA26.interactableName = "Select Vehicle";
            mpPVF45.interactableName = "Select Vehicle";
            pilot0.interactableName = "Select Pilot";
            pilot1.interactableName = "Select Pilot";
            pilot2.interactableName = "Select Pilot";
            mpPVContinue.interactableName = "Continue";
            mpPVAV42.OnInteract.AddListener(delegate { SwitchVehicle(Vehicle.AV42C); });
            mpPVFA26.OnInteract.AddListener(delegate { SwitchVehicle(Vehicle.FA26B); });
            mpPVF45.OnInteract.AddListener(delegate { SwitchVehicle(Vehicle.F45A); });
            mpPVContinue.OnInteract.AddListener(delegate { SwitchPage(Page.mpIPPort); });

            Console.Log("Pilot Stuff");
            //Just loading all the pilots in so that we can check that the one they pick exists
            PilotSaveManager.LoadPilotsFromFile();
            Console.Log("Loaded " + PilotSaveManager.pilots.Count + " pilots");

            List<PilotSave> pilots = new List<PilotSave>();
            foreach (PilotSave pilotSave in PilotSaveManager.pilots.Values)
            {
                pilots.Add(pilotSave);
            }
            Debug.Log("Setting Pilot");
            //Setting the pilot to the first value (would cause an error if they haven't got any pilots)
            pilotName = pilots[0].pilotName;

            Console.Log("Disabling Buttons");
            //Dealing with Pilots
            pilot0.transform.parent.parent.gameObject.SetActive(false);
            pilot1.transform.parent.parent.gameObject.SetActive(false);
            pilot2.transform.parent.parent.gameObject.SetActive(false);

            Console.Log("Pilots count is " + pilots.Count);
            if (pilots.Count >= 1)
            {
                Console.Log("1");
                pilot0.transform.parent.parent.gameObject.SetActive(true);
                mpPV.transform.GetChild(6).GetChild(1).GetComponent<Text>().text = pilots[0].pilotName;
                pilot0.OnInteract.AddListener(delegate { pilotName = pilots[0].pilotName; ; });
            }
            if (pilots.Count >= 2)
            {
                Console.Log("2");
                pilot1.transform.parent.parent.gameObject.SetActive(true);
                mpPV.transform.GetChild(7).GetChild(1).GetComponent<Text>().text = pilots[1].pilotName;
                pilot1.OnInteract.AddListener(delegate { pilotName = pilots[1].pilotName; });
            }
            if (pilots.Count >= 3)
            {
                Console.Log("3");
                pilot2.transform.parent.parent.gameObject.SetActive(true);
                mpPV.transform.GetChild(8).GetChild(1).GetComponent<Text>().text = pilots[2].pilotName;
                pilot2.OnInteract.AddListener(delegate { pilotName = pilots[2].pilotName; });
            }

            //MP Server IP and Port
            VRInteractable mpIPPortJoin = mpIPPort.transform.GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(mpIPPortJoin);
            mpIPPortJoin.interactableName = "Join Lobby";
            mpIPPortJoin.OnInteract.AddListener(delegate { ConnectToServer(); });

            //MP Server Info
            VRInteractable mpInfoJoin = mpServerInfo.transform.GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable mpInfoBack = mpServerInfo.transform.GetChild(1).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(mpInfoJoin);
            SetDefaultInteractable(mpInfoBack);
            mpInfoJoin.interactableName = "Join Game";
            mpInfoBack.interactableName = "Back";
            mpInfoJoin.OnInteract.AddListener(delegate { JoinGame(); });
            mpInfoBack.OnInteract.AddListener(delegate { client.Disconnect(); SwitchPage(Page.mpIPPort); });

            serverInfoText = mpServerInfo.transform.GetChild(2).GetComponent<Text>();

            //MP Banned
            VRInteractable mpBannedOkay = mpBanned.transform.GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(mpBannedOkay);
            mpBannedOkay.interactableName = "Okay";
            mpBannedOkay.OnInteract.AddListener(delegate { SwitchPage(Page.mpIPPort); });
            bannedReasonText = mpBanned.transform.GetChild(1).gameObject.GetComponent<Text>();
        }

        public VRInteractable SetDefaultInteractable(VRInteractable interactable)
        {
            VRInteractable returnValue = interactable;
            returnValue.radius = 0.06f;
            returnValue.sqrRadius = 0.0036f;
            returnValue.OnInteract = new UnityEvent();
            returnValue.OnInteracting = new UnityEvent();
            returnValue.OnStopInteract = new UnityEvent();
            returnValue.poseBounds = pb;
            returnValue.button = VRInteractable.Buttons.Trigger;
            return returnValue;
        }
        public void SwitchPage(Page page)
        {
            warningPage.SetActive(false);
            spmp.SetActive(false);
            sp.SetActive(false);
            mp.SetActive(false);
            spModPage.SetActive(false);
            spList.SetActive(false);
            mpPV.SetActive(false);
            mpIPPort.SetActive(false);
            mpServerInfo.SetActive(false);
            mpBanned.SetActive(false);
            switch (page)
            {
                case Page.warning:
                    warningPage.SetActive(true);
                    break;
                case Page.spmp:
                    spmp.SetActive(true);
                    break;
                case Page.spMod:
                    sp.SetActive(true);
                    spModPage.SetActive(true);
                    break;
                case Page.spList:
                    sp.SetActive(true);
                    spList.SetActive(true);
                    break;
                case Page.mpPV:
                    ModLoaderManager.instance.discordDetail = "Playing Online";
                    mp.SetActive(true);
                    mpPV.SetActive(true);
                    break;
                case Page.mpIPPort:
                    mp.SetActive(true);
                    mpIPPort.SetActive(true);
                    break;
                case Page.mpServerInfo:
                    mp.SetActive(true);
                    mpServerInfo.SetActive(true);
                    break;
                case Page.mpBanned:
                    mp.SetActive(true);
                    mpBanned.SetActive(true);
                    break;
            }

            Console.Log("Switched Page to " + page.ToString());
        }

        #endregion

        #region Multiplayer

        private void SetupMultiplayer()
        {
            client = ModLoaderManager.instance.GetUnityClient();
            client.MessageReceived += MessageReceived;
        }
        public void ConnectToServer(string ip = "marsh.vtolvr-mods.com", int port = 4296)
        {
            state = ConnectionState.Connecting;

            ip = Dns.GetHostEntry(ip).AddressList[0].ToString(); //Does a DNS look up

            try
            {
                //This causes an error if it doesn't connect
                client.Connect(IPAddress.Parse(ip), port, DarkRift.IPVersion.IPv4);

            }
            catch
            {
                //Failed to connect
                return;
            }

            //Sending a message of our information
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(SteamUser.GetSteamID().m_SteamID);
                writer.Write(pilotName);
                writer.Write(SteamFriends.GetPersonaName());
                writer.Write(vehicle.ToString());
                using (Message message = Message.Create((ushort)Tags.UserInfo, writer))
                    client.SendMessage(message, SendMode.Reliable);
            }
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //This should only need to handle one message, 
            //which is displaying the info about the server to the user
            using (Message message = e.GetMessage())
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    ushort tag = (ushort)message.Tag;
                    switch (tag)
                    {
                        case (ushort)Tags.LobbyInfo:
                            while (reader.Position < reader.Length)
                            {
                                string serverName = reader.ReadString();
                                string mapName = reader.ReadString();
                                int playerCount = reader.ReadInt32();
                                int maxPlayerCount = reader.ReadInt32();
                                int maxBudget = reader.ReadInt32();
                                bool allowWeapons = reader.ReadBoolean();
                                bool useSteamName = reader.ReadBoolean();
                                string playersNames = reader.ReadString();

                                currentMap = mapName;
                                serverInfoText.text = "Name: " + serverName + "\nMap: " + mapName
                                    + "\nMaxBudget: " + maxBudget + " Allow Weapons: " + allowWeapons
                                    + "\nUse Steam Name: " + useSteamName
                                    + "\nPlayers: " + playerCount + "/" + maxPlayerCount + "\n"
                                    + playersNames;

                                state = ConnectionState.Lobby;
                                SwitchPage(Page.mpServerInfo);
                            }
                            break;
                        case (ushort)Tags.Banned:
                            while (reader.Position < reader.Length)
                            {
                                string bannedReason = reader.ReadString();
                                SwitchPage(Page.mpBanned);
                                bannedReasonText.text = "You are banned from this server :(\n\nReason: \"" + bannedReason + "\"";
                            }
                            client.Disconnect();
                            break;
                    }

                    //Need to check if the bann message was returned

                }
            }
        }

        public void JoinGame()
        {
            client.MessageReceived -= MessageReceived;

            manager.StartMultiplayerProcedure(vehicle,pilotName);
        }
        public void SwitchVehicle(Vehicle newVehicle)
        {
            vehicle = newVehicle;
            //Changing the buttons colours
            switch (newVehicle)
            {
                case Vehicle.AV42C:
                    Console.Log("Switched player's vehicle to AV-42C");
                    break;
                case Vehicle.F45A:
                    Console.Log("Switched player's vehicle to F-45A");
                    break;
                case Vehicle.FA26B:
                    Console.Log("Switched player's vehicle to F/A-26B");
                    break;
            }
        }
        #endregion

        #region Singleplayer

        private void SetupSinglePlayer()
        {
            root = Directory.GetCurrentDirectory() + @"\VTOLVR_ModLoader";
            if (!Directory.Exists(root + mods))
                Directory.CreateDirectory(root + mods);
            spTransform = spList.transform;

            modSlots[0] = new ModSlot(spTransform.GetChild(3).gameObject, spTransform.GetChild(3).GetChild(1).GetComponent<Text>(), spTransform.GetChild(3).GetChild(0).gameObject.AddComponent<VRInteractable>());
            modSlots[1] = new ModSlot(spTransform.GetChild(4).gameObject, spTransform.GetChild(4).GetChild(1).GetComponent<Text>(), spTransform.GetChild(4).GetChild(0).gameObject.AddComponent<VRInteractable>());
            modSlots[2] = new ModSlot(spTransform.GetChild(5).gameObject, spTransform.GetChild(5).GetChild(1).GetComponent<Text>(), spTransform.GetChild(5).GetChild(0).gameObject.AddComponent<VRInteractable>());
            modSlots[3] = new ModSlot(spTransform.GetChild(6).gameObject, spTransform.GetChild(6).GetChild(1).GetComponent<Text>(), spTransform.GetChild(6).GetChild(0).gameObject.AddComponent<VRInteractable>());
            modSlots[4] = new ModSlot(spTransform.GetChild(7).gameObject, spTransform.GetChild(7).GetChild(1).GetComponent<Text>(), spTransform.GetChild(7).GetChild(0).gameObject.AddComponent<VRInteractable>());
            modSlots[5] = new ModSlot(spTransform.GetChild(8).gameObject, spTransform.GetChild(8).GetChild(1).GetComponent<Text>(), spTransform.GetChild(8).GetChild(0).gameObject.AddComponent<VRInteractable>());
            modSlots[6] = new ModSlot(spTransform.GetChild(9).gameObject, spTransform.GetChild(9).GetChild(1).GetComponent<Text>(), spTransform.GetChild(9).GetChild(0).gameObject.AddComponent<VRInteractable>());
            modSlots[7] = new ModSlot(spTransform.GetChild(10).gameObject, spTransform.GetChild(10).GetChild(1).GetComponent<Text>(), spTransform.GetChild(10).GetChild(0).gameObject.AddComponent<VRInteractable>());

            SetDefaultInteractable(modSlots[0].interactable);
            SetDefaultInteractable(modSlots[1].interactable);
            SetDefaultInteractable(modSlots[2].interactable);
            SetDefaultInteractable(modSlots[3].interactable);
            SetDefaultInteractable(modSlots[4].interactable);
            SetDefaultInteractable(modSlots[5].interactable);
            SetDefaultInteractable(modSlots[6].interactable);
            SetDefaultInteractable(modSlots[7].interactable);

            FindLocalMods();

            //Adding the materials from the asset bundle
            //redMaterial = assets.LoadAsset<Material>("Red");
            //greenMaterial = assets.LoadAsset<Material>("Green");
            redMaterial = new Material(Shader.Find("Diffuse"));
            redMaterial.color = Color.red;
            greenMaterial = new Material(Shader.Find("Diffuse"));
            greenMaterial.color = Color.green;

            UpdateList();
        }
        private void FindLocalMods()
        {
            ModLoaderManager.FindMods();
        }
        public void OnPageChanged(ModLoader.Page newPage)
        {
            if (newPage == ModLoader.Page.spList)
                UpdateList();
        }
        private void UpdateList()
        {
            list = new List(ModLoaderManager.mods);
            for (int i = 0; i < 8; i++)
            {
                if (ModLoaderManager.mods.Count > i)
                {
                    ModItem currentItem = list.mods[(list.currentPage * 8) + i];
                    modSlots[i].slot.SetActive(true);
                    modSlots[i].slotText.text = currentItem.name + " " + (currentItem.isLoaded ? "[Loaded]" : "");
                    Debug.Log("This mod is " + currentItem.isLoaded);
                    modSlots[i].interactable.interactableName = "View " + currentItem.name;
                    modSlots[i].interactable.button = VRInteractable.Buttons.Trigger;
                    modSlots[i].interactable.OnInteract.AddListener(delegate { OpenMod(currentItem); });
                }
                else
                {
                    modSlots[i].slot.SetActive(false);
                    modSlots[i].slotText.text = "Mod Name";
                    modSlots[i].interactable.interactableName = "Mod Name";
                    modSlots[i].interactable.OnInteract.RemoveAllListeners();
                }
            }
            UpdatePageButtons();
        }

        public void NextPage()
        {
            list.currentPage++;
            for (int i = 0; i < 8; i++)
            {
                if (list.mods.Count <= (list.currentPage * 8) + i + 1)
                {
                    modSlots[i].slot.SetActive(true);
                    modSlots[i].slotText.text = list.mods[(list.currentPage * 8) + i].name;
                }
            }
            UpdatePageButtons();
        }

        public void PreviousPage()
        {
            list.currentPage--;
            for (int i = 0; i < 8; i++)
            {
                if (list.mods.Count <= (list.currentPage * 8) + i + 1)
                {
                    modSlots[i].slot.SetActive(true);
                    modSlots[i].slotText.text = list.mods[(list.currentPage * 8) + i].name;
                }
            }
            UpdatePageButtons();
        }

        public void SetButtons(GameObject next, GameObject previous)
        {
            nextInteractable = next.GetComponent<VRInteractable>();
            previousInteractable = previous.GetComponent<VRInteractable>();
            nextMaterial = next.GetComponent<MeshRenderer>().material;
            previousMaterial = previous.GetComponent<MeshRenderer>().material;
        }

        public void SetModPageItems(VRInteractable loadInteractable, Text title, Text description, Material loadModMaterial, Text loadModText)
        {
            this.loadInteractable = loadInteractable;
            modTitleText = title;
            modDescriptionText = description;
            this.loadModMaterial = loadModMaterial;
            this.loadModText = loadModText;
        }

        private void UpdatePageButtons()
        {
            if (list.currentPage + 1 < list.pageCount)
            {
                nextInteractable.enabled = true;
                nextMaterial = greenMaterial;
            }
            else
            {
                nextInteractable.enabled = false;
                nextMaterial = redMaterial;
            }

            if (list.currentPage > 0)
            {
                previousInteractable.enabled = true;
                previousMaterial = greenMaterial;
            }
            else
            {
                previousInteractable.enabled = false;
                previousMaterial = redMaterial;
            }
        }

        public void OpenMod(ModItem item)
        {
            Debug.Log("Opening Mod " + item.name);

            modTitleText.text = item.name;
            modDescriptionText.text = item.description;
            loadInteractable.OnInteract.RemoveAllListeners();
            SwitchPage(Page.spMod);

            if (item.isLoaded)
            {
                loadModText.text = "Loaded!";
                loadModMaterial.color = Color.red;
                return;
            }
            loadModText.text = "Load";
            loadModMaterial.color = Color.green;
            loadInteractable.OnInteract.AddListener(delegate { LoadMod(item); });

        }

        public void LoadMod(ModItem item)
        {
            if (item.isLoaded)
            {
                Debug.Log(item.name + " is already loaded");
                return;
            }
            IEnumerable<Type> source = from t in item.assembly.GetTypes() where t.IsSubclassOf(typeof(VTOLMOD)) select t;
            if (source != null && source.Count() == 1)
            {
                new GameObject(item.name, source.First());
                ModLoaderManager.mods.Find(x => x.name == item.name).isLoaded = true;

                loadInteractable.OnInteract.RemoveAllListeners();
                loadModText.text = "Loaded!";
                loadModMaterial.color = Color.red;
                ModLoaderManager.instance.loadedModsCount++;
                ModLoaderManager.instance.UpdateDiscord();
            }
            else
            {
                Debug.LogError("Source is null");
            }
        }

        public void SwitchButton()
        {
            //This is getting disabled till the json issue gets fixed after release of 2.0.0
            return;
        }
        public class ModSlot
        {
            public GameObject slot;
            public Text slotText;
            public VRInteractable interactable;
            public ModSlot(GameObject slot, Text slotText, VRInteractable interactable)
            {
                this.slot = slot;
                this.slotText = slotText;
                this.interactable = interactable;
            }
        }

        public class List
        {
            public int currentPage;
            public int pageCount;
            public List<ModItem> mods;
            public List(List<ModItem> mods)
            {
                this.mods = mods;
                pageCount = mods.Count;
                currentPage = 0;
            }
        }
        

        [Serializable]
        public class APIMod
        {
            public string Name;
            public string Description;
            public string Creator;
            public string URL;
            public string Version;
        }
        #endregion
    }

    public static class Extensions
    {
        public static ModItem GetInfo(this Type type)
        {
            VTOLMOD.Info info = type.GetCustomAttributes(typeof(VTOLMOD.Info), true).FirstOrDefault<object>() as VTOLMOD.Info;
            ModItem item = new ModItem(info.name, info.description, info.version);
            return item;
        }

        public static string GetModName(this Type type)
        {
            VTOLMOD.Info info = type.GetCustomAttributes(typeof(VTOLMOD.Info), true).FirstOrDefault<object>() as VTOLMOD.Info;
            string name = info.name;
            return name;
        }
    }
}
