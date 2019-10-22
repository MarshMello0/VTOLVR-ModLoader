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
            discordState = "Using . Marsh.Mello .'s Mod Loader";
            UpdateDiscord();

            SteamAPI.Init();

            SceneManager.sceneLoaded += SceneLoaded;
            SetPaths();
            CreateAssetBundle();

            //gameObject.AddComponent<CSharp>();

        }
        private void CreateAPI()
        {
            gameObject.AddComponent<VTOLAPI>();
        }
        private void SetPaths()
        {
            rootPath = Directory.GetCurrentDirectory() + @"\VTOLVR_Modloader";
        }
        public void CreateAssetBundle()
        {
            //Stopped Loading of asset bundle as its not used
            return;
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
                    discordDetail = "Selecting mods";
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
            GameObject modloader = new GameObject("Mod Loader", typeof(ModLoader));
            DontDestroyOnLoad(modloader);
        }
    }
}
