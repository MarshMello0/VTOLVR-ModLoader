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

        public UnityClient client;
        public AssetBundle assets;

        private string rootPath;
        private string assetsPath = @"\modloader.assets";

        //Discord
        private DiscordController discord;
        public string discordDetail, discordState;
        public int loadedModsCount;

        private void Awake()
        {
            if (instance)
                Destroy(this.gameObject);

            instance = this;
            DontDestroyOnLoad(this.gameObject);
            Debug.Log("This is the first mod loader manager");

            discord = gameObject.AddComponent<DiscordController>();
            discordDetail = "Launching Game";
            discordState = "using . Marsh.Mello .'s Mod Loader";
            UpdateDiscord();

            SteamAPI.Init();
            
            SceneManager.sceneLoaded += SceneLoaded;
            SetPaths();
            CreateAssetBundle();
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
            //Spawning UConsole
            //Instantiate(assets.LoadAsset<GameObject>("UConsole-Canvas")).AddComponent<UConsole>();
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
            while (SceneManager.GetActiveScene().name != "SamplerScene")
            {
                yield return null;
            }
            GameObject modLoader = new GameObject("Mod Loader", typeof(ModLoader));
        }

        public UnityClient GetUnityClient()
        {
            if (!client)
            {
                client = gameObject.AddComponent<UnityClient>();
            }
            return client;
        }

    }
}
