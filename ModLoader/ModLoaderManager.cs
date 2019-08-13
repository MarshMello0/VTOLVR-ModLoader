using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using UnityEngine.CrashReportHandler;
using Steamworks;
using System.Collections;
using System.IO;
using ModLoader.Multiplayer;
using System.Reflection;

namespace ModLoader
{
    public class Load
    {
        public static void Init()
        {
            PlayerLogText();
            CrashReportHandler.enableCaptureExceptions = false;
            new GameObject("Mod Loader Manager", typeof(ModLoaderManager));
        }
        private static void PlayerLogText()
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
    }

    /// <summary>
    /// This class is to handle the changes between scenes
    /// </summary>
    class ModLoaderManager : MonoBehaviour
    {
        public static ModLoaderManager instance { get; private set; }

        
        public AssetBundle assets;

        public string rootPath;
        private string assetsPath = @"\modloader.assets";

        //Discord
        private DiscordController discord;
        public string discordDetail, discordState;
        public int loadedModsCount;

        //Multiplayer
        public UnityClient client;
        public bool doneFirstLoad;
        public ModLoader.Vehicle multiplayerVehicle;

        //SinglePlayer
        public static List<ModItem> mods = new List<ModItem>();

        private void Awake()
        {
            if (instance)
                Destroy(this.gameObject);

            instance = this;
            DontDestroyOnLoad(this.gameObject);
            Debug.Log("This is the first mod loader manager");
            
            CreateAPI();

            
            discord = gameObject.AddComponent<DiscordController>();
            discordDetail = "Launching Game";
            discordState = "using . Marsh.Mello .'s Mod Loader";
            UpdateDiscord();

            SteamAPI.Init();
            
            SceneManager.sceneLoaded += SceneLoaded;
            SetPaths();
            CreateAssetBundle();

            gameObject.AddComponent<CSharp>();
        }
        private void CreateAPI()
        {
            gameObject.AddComponent<VTOLAPI>();
        }
        private void SetPaths()
        {
            rootPath = Directory.GetCurrentDirectory() + @"\VTOLVR_Modloader";
        }
        private void CreateAssetBundle()
        {
            assets = AssetBundle.LoadFromFile(rootPath + assetsPath);
            if (assets == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                return;
            }
        }
        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            string sceneName = arg0.name;
            switch (sceneName)
            {
                case "SamplerScene":
                    discordDetail = "Selecting Mods";
                    StartCoroutine(CreateModLoader());
                    break;
                case "Akutan":
                    discordDetail = "Flying the " + PilotSaveManager.currentVehicle.vehicleName;
                    discordState = "Akutan: " + PilotSaveManager.currentCampaign.campaignName + " " + PilotSaveManager.currentScenario.scenarioName;
                    break;
                case "CustomMapBase":
                    discordDetail = "Flying the " + PilotSaveManager.currentVehicle.vehicleName;
                    discordState = "CustomMap: " + PilotSaveManager.currentCampaign.campaignName + " " + PilotSaveManager.currentScenario.scenarioName;
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
                case "VehicleConfiguration":
                    discordDetail = "Configuring " + PilotSaveManager.currentVehicle.vehicleName;
                    break;
                case "LaunchSplashScene":
                    break;
                default:
                    Debug.Log("ModLoader.cs | Scene not found (" + sceneName + ")");
                    break;
            }
            UpdateDiscord();
        }
        public void UpdateDiscord()
        {
            discord.UpdatePresence(loadedModsCount, discordDetail, discordState);
        }
        private IEnumerator CreateModLoader()
        {
            Debug.Log("Creating Mod Loader");
            while (SceneManager.GetActiveScene().name != "SamplerScene")
            {
                Debug.Log("Waiting for active Scene");
                yield return null;
            }
            Debug.Log("Creating new gameobject");
            new GameObject("Mod Loader", typeof(ModLoader));
        }
        public UnityClient GetUnityClient()
        {
            if (!client)
            {
                client = gameObject.AddComponent<UnityClient>();
            }
            return client;
        }
        public void StartMultiplayerProcedure(ModLoader.Vehicle vehicle, string pilotName)
        {
            StartCoroutine(StartMultiplayerEnumerator(vehicle, pilotName));
        }

        private IEnumerator StartMultiplayerEnumerator(ModLoader.Vehicle vehicle, string pilotName)
        {
            VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
            LoadingSceneController.LoadScene(3);

            yield return new WaitForSeconds(5);
            //After here we should be in the loader scene

            Console.Log("Setting Pilot");
            PilotSaveManager.current = PilotSaveManager.pilots[pilotName];

            Console.Log("Going though All built in campaigns");
            if (VTResources.GetBuiltInCampaigns() != null)
            {
                foreach (VTCampaignInfo info in VTResources.GetBuiltInCampaigns())
                {

                    if (vehicle == ModLoader.Vehicle.AV42C && info.campaignID == "av42cQuickFlight")
                    {
                        Console.Log("Setting Campaign");
                        PilotSaveManager.currentCampaign = info.ToIngameCampaign();
                        Console.Log("Setting Vehicle");
                        PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(info.vehicle);
                        break;
                    }

                    if (vehicle == ModLoader.Vehicle.FA26B && info.campaignID == "fa26bFreeFlight")
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
                    PilotSaveManager.currentScenario.baseBudget = 999999;
                    PilotSaveManager.currentScenario.totalBudget = 999999;
                    break;
                }
            }

            VTScenario.currentScenarioInfo = VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign);

            Console.Log(string.Format("Loading into game, Pilot:{3}, Campaign:{0}, Scenario:{1}, Vehicle:{2}",
                PilotSaveManager.currentCampaign.campaignName, PilotSaveManager.currentScenario.scenarioName,
                PilotSaveManager.currentVehicle.vehicleName, pilotName));

            LoadingSceneController.instance.PlayerReady(); //<< Auto Ready

            while (SceneManager.GetActiveScene().buildIndex != 7)
            {
                //Pausing this method till the loader scene is unloaded
                if (SceneManager.GetActiveScene().buildIndex == 4)
                {
                    LoadingSceneController.instance.PlayerReady();
                }
                yield return null;
            }

            //Adding the networking script to the game which will handle all of the other stuff
            NetworkingManager nm = new GameObject("Networking Manager").AddComponent<NetworkingManager>();
            nm.pilotName = pilotName;
            multiplayerVehicle = vehicle;
            client.MessageReceived += nm.MessageReceived;
        }


        public static List<ModItem> FindMods()
        {
            DirectoryInfo folder = new DirectoryInfo(ModLoaderManager.instance.rootPath + @"\mods");
            FileInfo[] files = folder.GetFiles("*.dll");
            mods = new List<ModItem>(files.Length);

            foreach (FileInfo file in files)
            {
                //Going though each .dll file, checking if there is a class which derives from VTOLMOD
                Assembly lastAssembly = Assembly.Load(File.ReadAllBytes(file.FullName));
                IEnumerable<Type> source = from t in lastAssembly.GetTypes() where t.IsSubclassOf(typeof(VTOLMOD)) select t;

                if (source.Count() != 1)
                {
                    Debug.Log("The mod " + file.FullName + " doesn't specify a mod class or specifies more than one");
                }
                else
                {
                    ModItem item = source.First().GetInfo();
                    item.SetPath(file.FullName);
                    item.SetAssembly(lastAssembly);
                    mods.Add(item);
                }
            }

            return mods;
        }
        public static void ListMods(string[] args)
        {
            if (mods.Count == 0)
                Debug.Log("There are no mods in the mods folder");
            foreach (ModItem item in mods)
            {
                Debug.Log(item.name + (item.isLoaded ? " [Loaded]" : ""));
            }
        }
        public static void ReloadMods(string[] args)
        {
            mods.Clear();
            FindMods();
        }

        public static void LoadCommand(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Debug.Log("You need to include a mod name \nEG: load No Gravity");
                return;
            }
            string modName = string.Join(" ", args);
            if (mods.Count == 0)
                FindMods();
            ModItem modToLoad = mods.Find(x => x.name.ToLower() == modName.ToLower());
            LoadMod(modToLoad);
        }

        public static void LoadMod(ModItem item)
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
                mods.Find(x => x.name == item.name).isLoaded = true;
                instance.loadedModsCount++;
                instance.UpdateDiscord();
            }
            else
            {
                Debug.LogError("Source is null");
            }
        }
    }

    public class ModItem
    {
        public string name { private set; get; }
        public string version { private set; get; }
        public string description { private set; get; }
        public Assembly assembly { private set; get; }
        public string path { private set; get; }
        public bool isLoaded = false;
        public ModItem(string name, string description, string version)
        {
            this.name = name;
            this.description = description;
            this.version = version;
        }

        public void SetAssembly(Assembly assembly)
        {
            this.assembly = assembly;
            Debug.Log("We have set the assembly " + assembly.FullName);
        }

        public void SetPath(string path)
        {
            this.path = path;
        }
    }
}
