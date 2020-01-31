using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.CrashReportHandler;
using Steamworks;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine.UI;
using System.Net;
using System.ComponentModel;

namespace ModLoader
{
    public class Load
    {
        public static void Init()
        {
            PlayerLogText();
            CrashReportHandler.enableCaptureExceptions = false;
            new GameObject("Mod Loader Manager", typeof(ModLoaderManager), typeof(SkinManager));
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
if you are having any issues with mods and would like to report a bug, please contact @. Marsh.Mello .#0001 
on the offical VTOL VR Discord or post an issue on github. 

VTOL VR Modding Discord Server: https://discord.gg/XZeeafp
Mod Loader Github: https://github.com/MarshMello0/VTOLVR-ModLoader
Mod Loader Website: https://vtolvr-mods.com/

Special Thanks to Ketkev for his continuous support to the mod loader and the website.

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
        private VTOLAPI api;
        public string rootPath;
        private string assetsPath = @"\modloader.assets";
        private string[] args;
        private WebClient client;

        //Discord
        private DiscordController discord;
        public string discordDetail, discordState;
        public int loadedModsCount;

        //Console
        private Windows.ConsoleWindow console = new Windows.ConsoleWindow();
        private Windows.ConsoleInput input = new Windows.ConsoleInput();
        private bool runConsole;

        private void Awake()
        {
            if (instance)
                Destroy(this.gameObject);

            instance = this;
            DontDestroyOnLoad(this.gameObject);
            SetPaths();
            Debug.Log("This is the first mod loader manager");
            args = Environment.GetCommandLineArgs();
            if (args.Contains("dev"))
            {
                Debug.Log("Creating Console");
                console.Initialize();
                console.SetTitle("VTOL VR Console");
                input.OnInputText += ConsoleInput;
                Application.logMessageReceived += LogCallBack;
                runConsole = true;
                Debug.Log("Created Console");
            }

            

            CreateAPI();
            

            discord = gameObject.AddComponent<DiscordController>();
            discordDetail = "Launching Game";
            discordState = ". Marsh.Mello .'s Mod Loader";
            UpdateDiscord();

            SteamAPI.Init();

            LoadStartupMods();

            SceneManager.sceneLoaded += SceneLoaded;

            //gameObject.AddComponent<CSharp>();

            api.CreateCommand("quit", delegate { Application.Quit(); });
            api.CreateCommand("print", PrintMessage);
            api.CreateCommand("help", api.ShowHelp);
        }

        private void ConsoleInput(string obj)
        {
            api.CheckConsoleCommand(obj);
        }

        private void LogCallBack(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Warning)
                System.Console.ForegroundColor = ConsoleColor.Yellow;
            else if (type == LogType.Error)
                System.Console.ForegroundColor = ConsoleColor.Red;
            else
                System.Console.ForegroundColor = ConsoleColor.White;

            // We're half way through typing something, so clear this line ..
            if (Console.CursorLeft != 0)
                input.ClearLine();

            System.Console.WriteLine(message);

            // If we were typing something re-add it.
            input.RedrawInputLine();
        }

        private void Update()
        {
            if (runConsole)
                input.Update();
        }
        private void OnDestroy()
        {
            if (runConsole)
                console.Shutdown();
        }

        private void CreateAPI()
        {
            api = gameObject.AddComponent<VTOLAPI>();
        }
        private void SetPaths()
        {
            rootPath = Directory.GetCurrentDirectory() + @"\VTOLVR_Modloader";
        }
        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            string sceneName = arg0.name;
            Debug.Log("Scene Loaded = " + sceneName);
            switch (sceneName)
            {
                case "SamplerScene":
                    discordDetail = "Selecting mods";
                    StartCoroutine(CreateModLoader());
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].Contains("PILOT="))
                        {
                            StartCoroutine(LoadLevel());
                        }
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
            GameObject modloader = new GameObject("Mod Loader", typeof(ModLoader));
            DontDestroyOnLoad(modloader);
        }

        public void PrintMessage(string obj)
        {
            obj.Remove(0, 5);
            Debug.Log(obj);
        }

        private IEnumerator LoadLevel()
        {
            Debug.Log("Loading Pilots from file");
            PilotSaveManager.LoadPilotsFromFile();
            yield return new WaitForSeconds(2);
                        

            string pilotName = "";
            string cID = "";
            string sID = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains("PILOT="))
                {
                    pilotName = args[i].Replace("PILOT=", "");
                }
                else if (args[i].Contains("SCENARIO_CID="))
                {
                    cID = args[i].Replace("SCENARIO_CID=","");
                }
                else if (args[i].Contains("SCENARIO_ID="))
                {
                    sID = args[i].Replace("SCENARIO_ID=", "");
                }
            }

            Debug.Log($"Loading Level\nPilot={pilotName}\ncID={cID}\nsID={sID}");
            VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
            LoadingSceneController.LoadScene(7);

            yield return new WaitForSeconds(5);
            //After here we should be in the loader scene

            Debug.Log("Setting Pilot");
            PilotSaveManager.current = PilotSaveManager.pilots[pilotName];
            Debug.Log("Going though All built in campaigns");
            if (VTResources.GetBuiltInCampaigns() != null)
            {
                foreach (VTCampaignInfo info in VTResources.GetBuiltInCampaigns())
                {
                    if (info.campaignID == cID)
                    {
                        Debug.Log("Setting Campaign");
                        PilotSaveManager.currentCampaign = info.ToIngameCampaign();
                        Debug.Log("Setting Vehicle");
                        PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(info.vehicle);
                        break;
                    }
                }
            }
            else
                Debug.Log("Campaigns are null");

            Debug.Log("Going though All missions in that campaign");
            foreach (CampaignScenario cs in PilotSaveManager.currentCampaign.missions)
            {
                if (cs.scenarioID == sID)
                {
                    Debug.Log("Setting Scenario");
                    PilotSaveManager.currentScenario = cs;
                    break;
                }
            }

            VTScenario.currentScenarioInfo = VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign);

            Debug.Log(string.Format("Loading into game, Pilot:{3}, Campaign:{0}, Scenario:{1}, Vehicle:{2}",
                PilotSaveManager.currentCampaign.campaignName, PilotSaveManager.currentScenario.scenarioName,
                PilotSaveManager.currentVehicle.vehicleName, pilotName));

            LoadingSceneController.instance.PlayerReady(); //<< Auto Ready

            while (SceneManager.GetActiveScene().buildIndex != 7)
            {
                //Pausing this method till the loader scene is unloaded
                yield return null;
            }
        }

        private void LoadStartupMods()
        {
            List<string> modsToLoad = new List<string>();
            string path;
            for (int i = 0; i < args.Length; i++)
            {
                Debug.Log("ARG=\n" + args[i]);
                if (args[i].Contains("mod="))
                {
                    Debug.Log("Found mod line\n" + args[i]);
                    //This is removeing "mod="
                    path = args[i].Remove(0,4);
                    modsToLoad.Add(path);
                    Debug.Log("Start up mod added, path = " + path);
                }
            }
            if (modsToLoad.Count == 0)
                return;


            for (int i = 0; i < modsToLoad.Count; i++)
            {
                try
                {
                    Debug.Log(rootPath + @"\mods\" + modsToLoad[i]);
                    IEnumerable<Type> source =
              from t in Assembly.Load(File.ReadAllBytes(rootPath + @"\mods\" + modsToLoad[i])).GetTypes()
              where t.IsSubclassOf(typeof(VTOLMOD))
              select t;
                    if (source != null && source.Count() == 1)
                    {
                        GameObject newModGo = new GameObject(modsToLoad[i], source.First());
                        VTOLMOD mod = newModGo.GetComponent<VTOLMOD>();
                        mod.SetModInfo(new Mod(modsToLoad[i], "STARTUPMOD", modsToLoad[i]));
                        newModGo.name = modsToLoad[i];
                        DontDestroyOnLoad(newModGo);
                        mod.ModLoaded();

                        ModLoaderManager.instance.loadedModsCount++;
                        ModLoaderManager.instance.UpdateDiscord();
                    }
                    else
                    {
                        Debug.LogError("Source is null");
                    }

                    Debug.Log("Loaded Startup mod from path = " + modsToLoad[i]);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error when loading startup mod\n" + e.ToString());
                }
            }
            
        }
    }
}
