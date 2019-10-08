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

            //gameObject.AddComponent<CSharp>();
            
        }
        /*
        IEnumerator Test()
        {
            Debug.Log("Spawning");
            yield return new WaitForSeconds(4);
            GameObject prefab = assets.LoadAsset<GameObject>("PlanePrefab");
            GameObject spawnedPrefab = Instantiate(prefab);
            spawnedPrefab.transform.position = new Vector3(0, 10, 0);
            //spawnedPrefab.transform.localScale = new Vector3(1, 1, 1);
            Debug.Log("Spawned");

            Debug.Log("Finding player");
            GameObject player = GameObject.Find("FA-26B(Clone)");
            if (player == null)
            {
                Debug.Log("Player is null");
                //spawnedPrefab.transform.position = new Vector3(0, 50, 0);
            }
            else
            {
                spawnedPrefab.transform.position = player.transform.position + new Vector3(5, 10, 5);
                spawnedPrefab.transform.rotation = Quaternion.Euler(0, 90, 0);
                Debug.Log("Moved Aircraft to " + spawnedPrefab.transform.position + "\nPlayer is at " + player.transform.position);


                Debug.Log("Adding Physics");
                spawnedPrefab.AddComponent<BoxCollider>();
                spawnedPrefab.AddComponent<Rigidbody>();
                Debug.Log("Loading Textures");
                Material mat = assets.LoadAsset<Material>("Plane");
                Debug.Log("Loaded Material");
                Debug.Log("Applying");

                MeshRenderer mr = spawnedPrefab.transform.GetChild(0).GetComponent<MeshRenderer>();
                mr.materials[0] = mat;
                Debug.Log("Appied one");
                mr.materials[1] = mat;
                Debug.Log("Appied all of them");
            }

        }
        */
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
        public GameObject listGO; 
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
