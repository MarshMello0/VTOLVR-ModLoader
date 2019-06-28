using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.CrashReportHandler;

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
            console = Instantiate(assets.LoadAsset<GameObject>("UConsole-Canvas")).AddComponent<UConsole>();

            console.AddCommand("spawn", "spawn", SpawnConsole);
        }

        private void SpawnConsole()
        {
            //Spawning ModLoader Panel
            GameObject modloader = Instantiate(assets.LoadAsset<GameObject>("ModLoader"));

            GameObject cam = FindObjectOfType<Camera>().gameObject;
            modloader.transform.position = cam.transform.position + Vector3.forward * 5f;
        }

        private UConsole console;
    }
}
