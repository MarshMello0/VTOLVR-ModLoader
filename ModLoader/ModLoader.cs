using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.CrashReportHandler;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using Steamworks;

namespace ModLoader
{
    public class Load
    {
        public static void Init()
        {
            CrashReportHandler.enableCaptureExceptions = false;
            new GameObject("Mod Loader", typeof(ModLoader));
        }
    }

    public class ModLoader : MonoBehaviour
    {
        private string assetsPath = @"\modloader.assets";
        private string root;

        private AssetBundle assets;

        //UI Objects
        GameObject warningPage, spmp, sp, mp, spModPage, spList, mpPV, mpIPPort;
        PoseBounds pb;
        public enum Page { warning, spmp,spMod,spList,mpPV,mpIPPort}

        //Discord
        private DiscordController discord;
        public string discordDetail, discordState;
        public int loadedModsCount;

        //Multiplayer
        private MultiplayerMod multiplayer;
        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            PlayerLogText();
        }
        private void PlayerLogText()
        {
            string playerLogMessage = @" 
                                                                                                         
                                                                                                         
 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
                                                                                                         
 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
                                                                                                         
 #     #                                              #     #                                            
 ##   ##   ####   #####   #####   ######  #####       #     #  ######  #####    ####   #   ####   #    # 
 # # # #  #    #  #    #  #    #  #       #    #      #     #  #       #    #  #       #  #    #  ##   # 
 #  #  #  #    #  #    #  #    #  #####   #    #      #     #  #####   #    #   ####   #  #    #  # #  # 
 #     #  #    #  #    #  #    #  #       #    #       #   #   #       #####        #  #  #    #  #  # # 
 #     #  #    #  #    #  #    #  #       #    #        # #    #       #   #   #    #  #  #    #  #   ## 
 #     #   ####   #####   #####   ######  #####          #     ######  #    #   ####   #   ####   #    # 

Thank you for download VTOL VR Mod loader by . Marsh.Mello .

Please don't report bugs unless you can reproduce them without any mods loaded
if you are having any issues with mods and would like to report a bug, please contact @. Marsh.Mello .#3194 
on the offical VTOL VR Discord or post an issue on github. 

VTOL VR Discord Server: https://discord.gg/azNkZHj
Mod Loader Github: https://github.com/MarshMello0/VTOLVR-ModLoader

Special Thanks to Ketkev and Nebriv with help in testing and modding.

 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
                                                                                                         
 #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  #####  ##### 
";
            Debug.Log(playerLogMessage);
        }

        private void Start()
        {
            SetPaths();
            CreateAssetBundle();
            StartCoroutine(WaitForScene());
            discord = gameObject.AddComponent<DiscordController>();
            discordDetail = "Launching Game";
            UpdateDiscord();
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            string sceneName = arg0.name;
            switch (sceneName)
            {
                case "LaunchSplashScene":
                    discordDetail = "Launching Game";
                    break;
                case "LoadingScene":
                    discordDetail = "Loading into mission";
                    break;
                case "ReadyRoom":
                    if (loadedModsCount == 0)
                    {
                        discordDetail = "In Main Menu";
                    }
                    else
                    {
                        discordDetail = "In Main Menu with " + loadedModsCount + (loadedModsCount == 0 ? " mod" : " mods");
                    }
                    
                    break;
                case "Akutan":
                    discordDetail = "Flying the " + PilotSaveManager.currentVehicle.vehicleName;
                    discordState = "Akutan: " + PilotSaveManager.currentCampaign.campaignName + " " + PilotSaveManager.currentScenario.scenarioName;
                    break;
                case "CustomMapBase":
                    discordDetail = "Flying the " + PilotSaveManager.currentVehicle.vehicleName;
                    discordState = "CustomMap: " + PilotSaveManager.currentCampaign.campaignName + " " + PilotSaveManager.currentScenario.scenarioName;
                    break;
                case "VehicleConfiguration":
                    discordDetail = "Configuring " + PilotSaveManager.currentVehicle.vehicleName;
                    break;
                case "SamplerScene":
                    discordDetail = "Selecting Mods";
                    break;
                default:
                    discordDetail = "In ";
                    discordState = sceneName;
                    break;
            }
            UpdateDiscord();
        }

        private void SetPaths()
        {
            root = Directory.GetCurrentDirectory() + @"\VTOLVR_Modloader";
        }
        private void CreateAssetBundle()
        {
            assets = AssetBundle.LoadFromFile(root + assetsPath);
            if (assets == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                return;
            }
            //Spawning UConsole
            Instantiate(assets.LoadAsset<GameObject>("UConsole-Canvas")).AddComponent<UConsole>();
        }
        IEnumerator WaitForScene()
        {
            while (SceneManager.GetActiveScene().name != "SamplerScene")
            {
                yield return null;
            }

            SteamAPI.Init();


            //We should now be in the game scene
            SetInGameUI();
            UpdateDiscord();

            
        }
        private void SetInGameUI()
        {
            //Adding Multiplayer Script
            multiplayer = gameObject.AddComponent<MultiplayerMod>();
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
            GameObject canvaus = Instantiate(assets.LoadAsset<GameObject>("ModLoader"));
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
            mpButton.interactableName = "Start Multiplayer";
            spButton.OnInteract.AddListener(delegate { SwitchPage(Page.spList); });
            mpButton.OnInteract.AddListener(delegate { SwitchPage(Page.mpPV); });
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

            SPModManager modManager = spList.AddComponent<SPModManager>();
            modManager.assets = assets;
            modManager.discord = gameObject.GetComponent<DiscordController>();
            modManager.modloader = this;
            modManager.SetButtons(spListNextPage.gameObject, spListPreviousPage.gameObject);
            modManager.SetModPageItems(
                spModLoad,
                spModPage.transform.GetChild(0).GetComponent<Text>(),
                spModPage.transform.GetChild(1).GetComponent<Text>(),
                spModPage.transform.GetChild(2).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material,
                spModPage.transform.GetChild(2).GetChild(1).GetComponent<Text>());
            spListSwitch.OnInteract.AddListener(delegate {modManager.SwitchButton(); });
            spListNextPage.OnInteract.AddListener(delegate { modManager.NextPage(); });
            spListPreviousPage.OnInteract.AddListener(delegate { modManager.PreviousPage(); });
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
            mpPVAV42.OnInteract.AddListener(delegate { multiplayer.SwitchVehicle(MultiplayerMod.Vehicle.AV42C); });
            mpPVFA26.OnInteract.AddListener(delegate { multiplayer.SwitchVehicle(MultiplayerMod.Vehicle.FA26B); });
            mpPVF45.OnInteract.AddListener(delegate { multiplayer.SwitchVehicle(MultiplayerMod.Vehicle.F45A); });
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
            multiplayer.pilotName = pilots[0].pilotName;

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
                pilot0.OnInteract.AddListener(delegate { multiplayer.pilotName = pilots[0].pilotName; ; });
            }
            if (pilots.Count >= 2)
            {
                Console.Log("2");
                pilot1.transform.parent.parent.gameObject.SetActive(true);
                mpPV.transform.GetChild(7).GetChild(1).GetComponent<Text>().text = pilots[1].pilotName;
                pilot1.OnInteract.AddListener(delegate { multiplayer.pilotName = pilots[1].pilotName; });
            }
            if (pilots.Count >= 3)
            {
                Console.Log("3");
                pilot2.transform.parent.parent.gameObject.SetActive(true);
                mpPV.transform.GetChild(8).GetChild(1).GetComponent<Text>().text = pilots[2].pilotName;
                pilot2.OnInteract.AddListener(delegate { multiplayer.pilotName = pilots[2].pilotName; });
            }

            //MP Server IP and Port
            VRInteractable mpIPPortJoin = mpIPPort.transform.GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(mpIPPortJoin);
            mpIPPortJoin.interactableName = "Join";
            mpIPPortJoin.OnInteract.AddListener(delegate { multiplayer.Connect(); });
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
                    multiplayer.enabled = false;
                    sp.SetActive(true);
                    spList.SetActive(true);
                    break;
                case Page.mpPV:
                    discordDetail = "Playing Online";
                    mp.SetActive(true);
                    mpPV.SetActive(true);
                    break;
                case Page.mpIPPort:
                    mp.SetActive(true);
                    mpIPPort.SetActive(true);
                    break;
            }
        }
       public void UpdateDiscord()
        {
            discord.UpdatePresence(loadedModsCount, discordDetail, discordState);
        }
    }
}
