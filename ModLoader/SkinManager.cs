using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ModLoader
{
    class SkinManager : VTOLMOD
    {
        //This variables are used on different scenes
        private List<Skin> installedSkins = new List<Skin>();
        private int selectedSkin = -1;

        //Vehicle Config scene only
        private int currentSkin;
        private Text scenarioName, scenarioDescription;
        private RawImage skinPreview;

        private static GameObject prefab;

        /// <summary>
        /// All the materials in the game
        /// </summary>
        private List<Mat> materials;
        /// <summary>
        /// The default textures so we can revert back
        /// </summary>
        private Dictionary<string, Texture> defaultTextures;
        private string[] matsNotToTouch = new string[] { "Font Material", "Font Material_0", "Font Material_1", "Font Material_2", "Font Material_3", "Font Material_4", "Font Material_5", "Font Material_6" };
        private struct Mat
        {
            public string name;
            public Material material;

            public Mat(string name, Material material)
            {
                this.name = name;
                this.material = material;
            }
        }
        private void Start()
        {
            Mod mod = new Mod();
            mod.name = "Skin Manger";
            SetModInfo(mod);
            VTOLAPI.SceneLoaded += SceneLoaded;
            Directory.CreateDirectory(ModLoaderManager.instance.rootPath + @"\skins");           
        }

        private void GetDefaultTextures()
        {
            Log("Getting Default Textures");
            Material[] materials = Resources.FindObjectsOfTypeAll(typeof(Material)) as Material[];
            defaultTextures = new Dictionary<string, Texture>(materials.Length);

            
            for (int i = 0; i < materials.Length; i++)
            {
                if (!matsNotToTouch.Contains(materials[i].name) && !defaultTextures.ContainsKey(materials[i].name))
                    defaultTextures.Add(materials[i].name, materials[i].GetTexture("_MainTex"));
            }

            Log($"Got {materials.Length} default textures stored");
            FindMaterials(materials);
        }

        private void SceneLoaded(VTOLScenes scene)
        {
            if (scene == VTOLScenes.VehicleConfiguration)
            {
                //Vehicle Configuration Room
                Log("Started Skins Vehicle Config room");
                if (defaultTextures == null)
                    GetDefaultTextures();
                else
                    RevertTextures();
                SpawnMenu();
            }
        }
        private void SpawnMenu()
        {
            if (prefab == null)
                prefab = ModLoader.assetBundle.LoadAsset<GameObject>("SkinLoaderMenu");
            
            //Setting Position
            GameObject pannel = Instantiate(prefab);
            pannel.transform.position = new Vector3(-83.822f, -15.68818f, 5.774f);
            pannel.transform.rotation = Quaternion.Euler(-180, 62.145f, 180);

            Transform scenarioDisplayObject = pannel.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1);

            //Storing Objects for later use
            scenarioName = scenarioDisplayObject.GetChild(1).GetChild(3).GetComponent<Text>();
            scenarioDescription = scenarioDisplayObject.GetChild(1).GetChild(2).GetComponent<Text>();
            skinPreview = scenarioDisplayObject.GetChild(1).GetChild(1).GetComponent<RawImage>();

            //Linking buttons with methods
            VRInteractable NextENVButton = scenarioDisplayObject.GetChild(1).GetChild(5).GetComponent<VRInteractable>();
            VRInteractable PrevENVButton = scenarioDisplayObject.GetChild(1).GetChild(6).GetComponent<VRInteractable>();
            NextENVButton.OnInteract.AddListener(Next);
            PrevENVButton.OnInteract.AddListener(Previous);

            VRInteractable ResetButton = scenarioDisplayObject.GetChild(2).GetComponent<VRInteractable>();
            ResetButton.OnInteract.AddListener(RevertTextures);

            VRInteractable ApplyButton = scenarioDisplayObject.GetChild(1).GetChild(4).GetComponent<VRInteractable>();
            ApplyButton.OnInteract.AddListener(delegate { SelectSkin(); Apply(); });

            FindSkins();
            UpdateUI();

        }
        private void FindSkins()
        {
            Log("Searching for Skins!");
            string path = ModLoaderManager.instance.rootPath + @"\skins";
            foreach (string folder in Directory.GetDirectories(path))
            {
                Skin currentSkin = new Skin();
                string[] split = folder.Split('\\');
                currentSkin.name = split[split.Length - 1];
                if (File.Exists(folder + @"\0.png")) //AV-42C
                {
                    currentSkin.hasAv42c = true;
                    Log($"[{folder}] has a skin for the AV-42C");
                }

                if (File.Exists(folder + @"\1.png")) //FA26B
                {
                    currentSkin.hasFA26B = true;
                    Log($"[{folder}] has a skin for the FA-26B");
                }

                if (File.Exists(folder + @"\2.png")) //F45A
                {
                    currentSkin.hasF45A = true;
                    Log($"[{folder}] has a skin for the F-45A");
                }

                if (VTOLAPI.GetPlayersVehicleEnum() == VTOLVehicles.AV42C && currentSkin.hasAv42c)
                {
                    currentSkin.folderPath = folder;
                    installedSkins.Add(currentSkin);
                    Log("Added that skin to the list");
                }
                else if (VTOLAPI.GetPlayersVehicleEnum() == VTOLVehicles.FA26B && currentSkin.hasFA26B)
                {
                    currentSkin.folderPath = folder;
                    installedSkins.Add(currentSkin);
                    Log("Added that skin to the list");
                }
                else if (VTOLAPI.GetPlayersVehicleEnum() == VTOLVehicles.F45A && currentSkin.hasF45A)
                {
                    currentSkin.folderPath = folder;
                    installedSkins.Add(currentSkin);
                    Log("Added that skin to the list");
                }
                else if (!currentSkin.hasAv42c && !currentSkin.hasF45A && !currentSkin.hasF45A)
                {
                    LogError($"It seems that a folder doesn't have any skins in it. Folder: {folder}");
                }

            }
        }
        public void Next()
        {
            currentSkin += 1;
            ClampCount();
            UpdateUI();
        }
        public void Previous()
        {
            currentSkin -= 1;
            ClampCount();
            UpdateUI();
            
        }
        public void SelectSkin()
        {
            Debug.Log("Changed selected skin to " + currentSkin);
            selectedSkin = currentSkin;
        }

        

        private void FindMaterials(Material[] mats)
        {
            if (mats == null)
                mats = Resources.FindObjectsOfTypeAll<Material>();
            materials = new List<Mat>(mats.Length);

            //We now add every texture into the dictionary which gives more things to change for the skin creators
            for (int i = 0; i < mats.Length; i++)
            {
                materials.Add(new Mat(mats[i].name, mats[i]));
            }
        }
        public void RevertTextures()
        {
            Log("Reverting Textures");
            for (int i = 0; i < materials.Count; i++)
            {
                if (defaultTextures.ContainsKey(materials[i].name))
                    materials[i].material.SetTexture("_MainTex", defaultTextures[materials[i].name]);
                else
                    LogError($"Tried to get material {materials[i].name} but it wasn't in the default dictonary");
            }
        }
        private void Apply()
        {
            Log("Applying Skin Number " + selectedSkin);
            if (selectedSkin < 0)
            {
                Debug.Log("Selected Skin was below 0");
                return;
            }

            Skin selected = installedSkins[selectedSkin];

            Log("\nSkin: " + selected.name + " \nPath: " + selected.folderPath);

            for (int i = 0; i < materials.Count; i++)
            {
                if (File.Exists(selected.folderPath + @"\" + materials[i].name + ".png"))
                    StartCoroutine(UpdateTexture(selected.folderPath + @"\" + materials[i].name + ".png", materials[i].material));
                else
                    Log("File Doesn't exist for skin\n" +
                        selected.folderPath + @"\" + materials[i].name + ".png");
            }
        }

        #region Old Method
        /*
        private void ApplySkin(GameObject vehicle = null)
        {
            Debug.Log("Applying Skin Number " + selectedSkin);
            if (selectedSkin < 0)
            {
                Debug.Log("Selected Skin was below 0");
                return;
            }

            Skin selected = installedSkins[selectedSkin];

            Debug.Log("\nSkin: " + selected.name + " \nPath: " + selected.folderPath + "\nHasAV42C: " + selected.hasAv42c);
            switch (VTOLAPI.GetPlayersVehicleEnum())
            {
                case VTOLVehicles.AV42C:
                    ApplyVTOL4((vehicle == null? GameObject.Find("VTOL4(Clone)") : vehicle).transform, selected);
                    break;
                case VTOLVehicles.FA26B:
                    ApplyFA26B((vehicle == null ? GameObject.Find("FA-26B(Clone)") : vehicle).transform, selected);
                    break;
                case VTOLVehicles.F45A:
                    
                    if (File.Exists(selected.folderPath + @"\sevtf_CanopyInt.png") && skins.ContainsKey("mat_sevtf_CanopyInt"))
                        StartCoroutine(UpdateTexture(selected.folderPath + @"\sevtf_CanopyInt.png", skins["mat_sevtf_CanopyInt"]));
                    if (File.Exists(selected.folderPath + @"\sevtf_engine.png") && skins.ContainsKey("mat_sevtf_engine"))
                        StartCoroutine(UpdateTexture(selected.folderPath + @"\sevtf_engine.png", skins["mat_sevtf_engine"]));
                    if (File.Exists(selected.folderPath + @"\sevtf_ext.png") && skins.ContainsKey("mat_sevtf_ext"))
                        StartCoroutine(UpdateTexture(selected.folderPath + @"\sevtf_ext.png", skins["mat_sevtf_ext"]));
                    if (File.Exists(selected.folderPath + @"\sevtf_ext2.png") && skins.ContainsKey("mat_sevtf_ext2"))
                        StartCoroutine(UpdateTexture(selected.folderPath + @"\sevtf_ext2.png", skins["mat_sevtf_ext2"]));
                    if (File.Exists(selected.folderPath + @"\sevtf_int.png") && skins.ContainsKey("mat_sevtf_int"))
                        StartCoroutine(UpdateTexture(selected.folderPath + @"\sevtf_int.png", skins["mat_sevtf_int"]));
                    if (File.Exists(selected.folderPath + @"\sevtf_lowPoly.png") && skins.ContainsKey("mat_sevtf_lowPoly"))
                        StartCoroutine(UpdateTexture(selected.folderPath + @"\sevtf_lowPoly.png", skins["mat_sevtf_lowPoly"]));
                    Debug.Log("Loaded F-45A Skins");
                    
                    break;
                case VTOLVehicles.None:
                    Debug.LogError("API FAILED");
                    break;
            }
        }
        private void ApplyVTOL4(Transform vehicle, Skin selected)
        {
            /*
             * Here we are finding all the Game Objects which have the texture on it, 
             * we are manually going though them all instead of changing the material in memory
             * because then it will just only apply to that vehicle meaning different vehilces could
             * have different textures. EG one team blue other team red but all VTOL4's
             
            Debug.Log("Applying Skin...");
            Transform VT4Body = vehicle.Find("VT4Body(new)");
            Transform Body = vehicle.Find("Body");
            Transform LeftGear = vehicle.Find("NewLandingGear").GetChild(1);
            Transform RightGear = vehicle.Find("NewLandingGear").GetChild(2);
            if (File.Exists(selected.folderPath + @"\vtol4Exterior.png"))
            {
                Debug.Log("Searching for objects with vtol4Exterior material");
                List<Material> mats = new List<Material>();
                
                mats.Add(VT4Body.GetComponent<MeshRenderer>().material);
                mats.Add(VT4Body.GetChild(0).GetComponent<MeshRenderer>().material);
                mats.Add(VT4Body.GetChild(3).GetComponent<MeshRenderer>().material);
                mats.Add(VT4Body.GetChild(4).GetComponent<MeshRenderer>().material);
                mats.Add(VT4Body.GetChild(5).GetComponent<MeshRenderer>().material);
                mats.Add(VT4Body.GetChild(6).GetComponent<MeshRenderer>().material);
                mats.Add(VT4Body.GetChild(7).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);
                mats.Add(VT4Body.GetChild(7).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);

                Transform VTOLAnimated = vehicle.Find("VTOLAnimated(doNotChange)");
                mats.Add(VTOLAnimated.GetChild(0).GetComponent<MeshRenderer>().material);
                mats.Add(VTOLAnimated.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);
                mats.Add(VTOLAnimated.GetChild(1).GetComponent<MeshRenderer>().material);
                mats.Add(VTOLAnimated.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material);
                mats.Add(VTOLAnimated.GetChild(2).GetComponent<MeshRenderer>().material);
                mats.Add(VTOLAnimated.GetChild(3).GetComponent<MeshRenderer>().material);
                mats.Add(VTOLAnimated.GetChild(4).GetComponent<MeshRenderer>().material);
                mats.Add(VTOLAnimated.GetChild(5).GetComponent<MeshRenderer>().material);
                mats.Add(VTOLAnimated.GetChild(6).GetComponent<MeshRenderer>().material);

                mats.Add(Body.GetChild(11).GetChild(0).GetComponent<MeshRenderer>().material); //fuelDoorLeft
                mats.Add(Body.GetChild(11).GetChild(1).GetComponent<MeshRenderer>().material); //fuelDoorRight

                mats.Add(Body.GetChild(12).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material); //landingHook

                mats.Add(Body.GetChild(13).GetChild(0).GetComponent<MeshRenderer>().material); //canopyFrame.002

                mats.Add(Body.GetChild(20).GetComponent<MeshRenderer>().material); //airBrake
                mats.Add(Body.GetChild(20).GetChild(0).GetComponent<MeshRenderer>().material); //airbrakePiston
                mats.Add(Body.GetChild(21).GetComponent<MeshRenderer>().material); //airbrakeCylinder

                mats.Add(LeftGear.GetChild(1).GetChild(1).GetChild(2).GetComponent<MeshRenderer>().material);//gearLowerDoorLeft
                mats.Add(RightGear.GetChild(1).GetChild(0).GetChild(2).GetComponent<MeshRenderer>().material);//gearLowerDoorRight
                StartCoroutine(UpdateTexture(selected.folderPath + @"\vtol4Exterior.png",mats));
            }
            if (File.Exists(selected.folderPath + @"\vtol4Exterior2.png"))
            {
                Debug.Log("Searching for objects with vtol4Exterior2 material");
                List<Material> mats = new List<Material>();
                mats.Add(VT4Body.GetChild(1).GetComponent<MeshRenderer>().material);
                mats.Add(VT4Body.GetChild(2).GetComponent<MeshRenderer>().material);
                mats.Add(Body.GetChild(9).GetComponent<MeshRenderer>().material); //WingLeft
                mats.Add(Body.GetChild(9).GetChild(3).GetComponent<MeshRenderer>().material); //aileronLeft
                mats.Add(Body.GetChild(9).GetChild(4).GetComponent<MeshRenderer>().material); //leadingEdgeLeft
                mats.Add(Body.GetChild(9).GetChild(5).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material); //tiltHousingLeft
                mats.Add(Body.GetChild(10).GetComponent<MeshRenderer>().material); //WingRight
                mats.Add(Body.GetChild(10).GetChild(1).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material); //tiltHousingRight
                mats.Add(Body.GetChild(10).GetChild(2).GetComponent<MeshRenderer>().material); //aileronRight
                mats.Add(Body.GetChild(10).GetChild(3).GetComponent<MeshRenderer>().material); //leadingEdgeRight

                mats.Add(Body.GetChild(22).GetComponent<MeshRenderer>().material);//TailplaneLeft
                mats.Add(Body.GetChild(22).GetChild(0).GetComponent<MeshRenderer>().material);//leftElevator
                mats.Add(Body.GetChild(22).GetChild(1).GetComponent<MeshRenderer>().material);//leftRudder
                mats.Add(Body.GetChild(23).GetComponent<MeshRenderer>().material);//rightTailplane
                mats.Add(Body.GetChild(23).GetChild(0).GetComponent<MeshRenderer>().material);//rightElevator
                mats.Add(Body.GetChild(23).GetChild(1).GetComponent<MeshRenderer>().material);//rightRudder

                Transform FrontGear = vehicle.Find("NewLandingGear").GetChild(0);
                mats.Add(FrontGear.GetChild(1).GetComponent<MeshRenderer>().material);// frontGearBase
                mats.Add(FrontGear.GetChild(2).GetComponent<MeshRenderer>().material);// frontGearSupportPiston
                mats.Add(FrontGear.GetChild(6).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);// frontGearSteering
                mats.Add(FrontGear.GetChild(6).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);// frontWheel
                mats.Add(FrontGear.GetChild(6).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);// catHookBase
                mats.Add(FrontGear.GetChild(6).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);// catHook
                mats.Add(FrontGear.GetChild(6).GetChild(1).GetComponent<MeshRenderer>().material);// frontGearCylinder
                mats.Add(FrontGear.GetChild(6).GetChild(1).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);// frontGearSupportCylinder
                mats.Add(FrontGear.GetChild(6).GetChild(1).GetChild(1).GetComponent<MeshRenderer>().material);// frontGearPiston

                mats.Add(LeftGear.GetChild(1).GetChild(1).GetComponent<MeshRenderer>().material);//leftGearArm
                mats.Add(LeftGear.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//leftGearBracket
                mats.Add(LeftGear.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//leftWheel
                mats.Add(LeftGear.GetChild(1).GetChild(1).GetChild(1).GetComponent<MeshRenderer>().material);//leftGearPiston
                mats.Add(LeftGear.GetChild(2).GetComponent<MeshRenderer>().material);//leftGearCylinder

                mats.Add(RightGear.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material);//rightGearArm
                mats.Add(RightGear.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//rightGearBracket
                mats.Add(RightGear.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//rightWheel
                mats.Add(RightGear.GetChild(1).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);//rightGearPiston
                mats.Add(RightGear.GetChild(2).GetComponent<MeshRenderer>().material);//rightGearCylinder
                StartCoroutine(UpdateTexture(selected.folderPath + @"\vtol4Exterior2.png", mats));
            }
            if (File.Exists(selected.folderPath + @"\vtol4Interior.png"))
            {
                Debug.Log("Searching for objects with vtol4Interior material");
                List<Material> mats = new List<Material>();
                //vtol4AdjustableThrottle
                mats.Add(Body.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material); //adjCollectiveBase
                mats.Add(Body.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material); //collectiveBaseHinge
                mats.Add(Body.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material); //collective
                mats.Add(Body.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material); //collectiveHandle
                //vtol4adjustableJoystick
                mats.Add(Body.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material);//adJoyBase
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material);//adJoyHeight
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//adJoyFwd
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//adJoyRight
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//joystick
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);//dButtonBase
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material);//dButton
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(2).GetComponent<MeshRenderer>().material);//dButtonBase (1)
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(2).GetChild(0).GetComponent<MeshRenderer>().material);//dButton
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(3).GetComponent<MeshRenderer>().material);//dButtonBase (2)
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(3).GetChild(0).GetComponent<MeshRenderer>().material);//dButton
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(4).GetComponent<MeshRenderer>().material);//dButtonBase (3)
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(4).GetChild(0).GetComponent<MeshRenderer>().material);//dButton
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(5).GetComponent<MeshRenderer>().material);//dButtonBase (4)
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(5).GetChild(0).GetComponent<MeshRenderer>().material);//dButton
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(6).GetComponent<MeshRenderer>().material);//dButtonBase (5)
                mats.Add(Body.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(6).GetChild(0).GetComponent<MeshRenderer>().material);//dButton

                mats.Add(Body.GetChild(13).GetChild(1).GetComponent<MeshRenderer>().material); //Cube

                mats.Add(vehicle.Find("helmPanel").GetComponent<MeshRenderer>().material);//helmPanel
                StartCoroutine(UpdateTexture(selected.folderPath + @"\vtol4Interior.png", mats));
            }
            if (File.Exists(selected.folderPath + @"\vtol4TiltEngine.png"))
            {
                Debug.Log("Searching for objects with vtol4TiltEngine material");
                List<Material> mats = new List<Material>();
                mats.Add(Body.GetChild(9).GetChild(5).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material); //tiltEngineLeft
                mats.Add(Body.GetChild(10).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material); //tiltEngineRight
                StartCoroutine(UpdateTexture(selected.folderPath + @"\vtol4TiltEngine.png", mats));
            }
            Debug.Log("Loaded AV42C Skins\nvtol4exterior.png" + File.Exists(selected.folderPath + @"\vtol4Exterior.png") +
                "\nvtol4Exterior2.png: " + File.Exists(selected.folderPath + @"\vtol4Exterior2.png") +
                "\nvtol4Interior.png: " + File.Exists(selected.folderPath + @"\vtol4Interior.png") +
                "\nvtol4TiltEngine.png: " + File.Exists(selected.folderPath + @"\vtol4TiltEngine.png"));
        }
        private void ApplyFA26B(Transform vehicle, Skin selected)
        {
            Debug.Log("Applying Skin...");
            Transform aFighter2 = vehicle.Find("aFighter2");
            Transform LandingGear = vehicle.Find("LandingGear");
            Transform FrontGear = LandingGear.GetChild(1);
            Transform LeftGear = LandingGear.GetChild(2);
            Transform RightGear = LandingGear.GetChild(3);
            if (File.Exists(selected.folderPath + @"\aFighterCanopyExt.png"))
            {
                List<Material> mats = new List<Material>();

                StartCoroutine(UpdateTexture(selected.folderPath + @"\aFighterCanopyExt.png", mats));
            }
            if (File.Exists(selected.folderPath + @"\aFighterCanopyInt.png"))
            {
                List<Material> mats = new List<Material>();

                StartCoroutine(UpdateTexture(selected.folderPath + @"\aFighterCanopyInt.png", mats));
            }
            if (File.Exists(selected.folderPath + @"\afighterExt1.png"))
            {
                //Broken
                List<Material> mats = new List<Material>();

                mats.Add(aFighter2.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//airbrakeCylinder
                mats.Add(aFighter2.GetChild(0).GetChild(2).GetComponent<MeshRenderer>().material);//airBrake
                mats.Add(aFighter2.GetChild(0).GetChild(2).GetChild(0).GetComponent<MeshRenderer>().material);//airbrakePiston

                mats.Add(aFighter2.GetChild(1).GetComponent<MeshRenderer>().material);//body
                mats.Add(aFighter2.GetChild(1).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//airFuelDoor
                mats.Add(aFighter2.GetChild(1).GetChild(2).GetComponent<MeshRenderer>().material);//intakeLeft
                mats.Add(aFighter2.GetChild(1).GetChild(3).GetComponent<MeshRenderer>().material);//intakeRight
                mats.Add(aFighter2.GetChild(8).GetComponent<MeshRenderer>().material);//gunBarrels
                mats.Add(aFighter2.GetChild(12).GetChild(1).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//landingHook

                Transform AnimatedDoors = LandingGear.GetChild(5);
                mats.Add(AnimatedDoors.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//frontDoorLeft
                mats.Add(AnimatedDoors.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material);//frontDoorRear
                mats.Add(AnimatedDoors.GetChild(2).GetChild(0).GetComponent<MeshRenderer>().material);//frontDoorRight
                mats.Add(AnimatedDoors.GetChild(3).GetChild(0).GetComponent<MeshRenderer>().material);//rearLeftDoor1
                mats.Add(AnimatedDoors.GetChild(4).GetChild(0).GetComponent<MeshRenderer>().material);//rearLeftDoor2
                mats.Add(AnimatedDoors.GetChild(5).GetChild(0).GetComponent<MeshRenderer>().material);//rearRightDoor1
                mats.Add(AnimatedDoors.GetChild(6).GetChild(0).GetComponent<MeshRenderer>().material);//rearRightDoor2
                StartCoroutine(UpdateTexture(selected.folderPath + @"\afighterExt1.png", mats));
            }
            if (File.Exists(selected.folderPath + @"\afighterExt2.png"))
            {
                List<Material> mats = new List<Material>();
                mats.Add(aFighter2.GetChild(1).GetChild(1).GetComponent<MeshRenderer>().material);//cockpitFrame
                mats.Add(aFighter2.GetChild(3).GetChild(1).GetChild(0).GetChild(2).GetComponent<MeshRenderer>().material);//canopyFrame
                mats.Add(aFighter2.GetChild(3).GetChild(1).GetChild(0).GetChild(3).GetComponent<MeshRenderer>().material);//canopyHinge
                mats.Add(aFighter2.GetChild(3).GetChild(1).GetChild(0).GetChild(3).GetChild(0).GetComponent<MeshRenderer>().material);//canopyCylinder
                mats.Add(aFighter2.GetChild(3).GetChild(1).GetChild(0).GetChild(4).GetComponent<MeshRenderer>().material);//canopySlideRails
                mats.Add(aFighter2.GetChild(3).GetChild(1).GetChild(1).GetComponent<MeshRenderer>().material);//canopyHingeBars
                mats.Add(aFighter2.GetChild(3).GetChild(2).GetComponent<MeshRenderer>().material);//canopyPiston
                mats.Add(aFighter2.GetChild(5).GetChild(0).GetComponent<MeshRenderer>().material);//elevonLeft
                mats.Add(aFighter2.GetChild(6).GetChild(0).GetComponent<MeshRenderer>().material);//elevonRight
                mats.Add(aFighter2.GetChild(16).GetComponent<MeshRenderer>().material);//verticalStabLeft
                mats.Add(aFighter2.GetChild(16).GetChild(0).GetComponent<MeshRenderer>().material);//rudderLeft
                mats.Add(aFighter2.GetChild(17).GetComponent<MeshRenderer>().material);//verticalStabRight
                mats.Add(aFighter2.GetChild(17).GetChild(0).GetComponent<MeshRenderer>().material);//rudderRight

                mats.Add(aFighter2.GetChild(19).GetChild(0).GetComponent<MeshRenderer>().material);//wingLeft
                mats.Add(aFighter2.GetChild(19).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);//flapLeft
                mats.Add(aFighter2.GetChild(19).GetChild(0).GetChild(2).GetComponent<MeshRenderer>().material);//rootSlatLeft
                mats.Add(aFighter2.GetChild(19).GetChild(0).GetChild(5).GetChild(0).GetComponent<MeshRenderer>().material);//wingFold
                mats.Add(aFighter2.GetChild(19).GetChild(0).GetChild(5).GetChild(0).GetChild(2).GetComponent<MeshRenderer>().material);//aileronLeft
                mats.Add(aFighter2.GetChild(19).GetChild(0).GetChild(5).GetChild(0).GetChild(3).GetComponent<MeshRenderer>().material);//tipSlatLeft
                mats.Add(aFighter2.GetChild(20).GetChild(0).GetComponent<MeshRenderer>().material);//wingRight
                mats.Add(aFighter2.GetChild(20).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);//flapRight
                mats.Add(aFighter2.GetChild(20).GetChild(0).GetChild(2).GetComponent<MeshRenderer>().material);//rootSlatRight
                mats.Add(aFighter2.GetChild(20).GetChild(0).GetChild(3).GetChild(0).GetComponent<MeshRenderer>().material);//wingFoldRight
                mats.Add(aFighter2.GetChild(20).GetChild(0).GetChild(3).GetChild(0).GetChild(3).GetComponent<MeshRenderer>().material);//aileronRight
                mats.Add(aFighter2.GetChild(20).GetChild(0).GetChild(3).GetChild(0).GetChild(4).GetComponent<MeshRenderer>().material);//tipSlatRight

                mats.Add(FrontGear.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//frontGearPiston
                mats.Add(FrontGear.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//catHookCylinder
                mats.Add(FrontGear.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//catHookPiston
                mats.Add(FrontGear.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);//frontWheels
                mats.Add(FrontGear.GetChild(2).GetChild(0).GetComponent<MeshRenderer>().material);//frontGearCylinder
                mats.Add(FrontGear.GetChild(2).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//frontGearSupport1
                mats.Add(FrontGear.GetChild(2).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);//frontGearSupport2
                mats.Add(FrontGear.GetChild(4).GetComponent<MeshRenderer>().material);//frontGearHinge

                mats.Add(LeftGear.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//leftGearCylinder
                mats.Add(LeftGear.GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//leftGearSupport1
                mats.Add(LeftGear.GetChild(0).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);//leftGearSupport2
                mats.Add(LeftGear.GetChild(2).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//leftGearPiston
                mats.Add(LeftGear.GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//leftWheel
                mats.Add(LeftGear.GetChild(4).GetComponent<MeshRenderer>().material);//leftGearHinge

                mats.Add(RightGear.GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//rightGearPiston
                mats.Add(RightGear.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//rightWheel
                mats.Add(RightGear.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material);//rightGearCylinder
                mats.Add(RightGear.GetChild(1).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//rightGearSupport1
                mats.Add(RightGear.GetChild(1).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);//rightGearSupport2
                StartCoroutine(UpdateTexture(selected.folderPath + @"\afighterExt2.png", mats));
            }
            if (File.Exists(selected.folderPath + @"\aFighterInterior.png"))
            {
                List<Material> mats = new List<Material>();
                mats.Add(aFighter2.GetChild(4).GetComponent<MeshRenderer>().material);//dash
                mats.Add(aFighter2.GetChild(10).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//heightAdjustModel
                mats.Add(aFighter2.GetChild(10).GetChild(0).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);//fwdAdjust
                mats.Add(aFighter2.GetChild(10).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material);//rightAdjust
                mats.Add(aFighter2.GetChild(10).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//joyYaw.001
                mats.Add(aFighter2.GetChild(13).GetComponent<MeshRenderer>().material);//sidePanels
                mats.Add(aFighter2.GetChild(14).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);//throttle
                mats.Add(aFighter2.GetChild(15).GetComponent<MeshRenderer>().material);//throttleTrack
                StartCoroutine(UpdateTexture(selected.folderPath + @"\aFighterInterior.png", mats));
            }
            if (File.Exists(selected.folderPath + @"\aFighterInterior2.png"))
            {
                List<Material> mats = new List<Material>();
                mats.Add(aFighter2.GetChild(3).GetChild(1).GetChild(0).GetChild(2).GetChild(0).GetComponent<MeshRenderer>().material);//canopyFrame.001
                mats.Add(aFighter2.GetChild(3).GetChild(1).GetChild(0).GetChild(2).GetChild(1).GetComponent<MeshRenderer>().material);//canopyFrame.002
                mats.Add(aFighter2.GetChild(4).GetChild(2).GetComponent<MeshRenderer>().material);//dash.001
                StartCoroutine(UpdateTexture(selected.folderPath + @"\aFighterInterior2.png", mats));
            }
            if (File.Exists(selected.folderPath + @"\vgLowpoly.png"))
            {
                List<Material> mats = new List<Material>();
                MeshRenderer[] meshRenders = aFighter2.GetChild(21).GetChild(0).GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer meshRenderer in meshRenders)
                {
                    mats.Add(meshRenderer.material);
                }

                MeshRenderer[] meshRenders2 = aFighter2.GetChild(22).GetChild(0).GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer meshRenderer in meshRenders2)
                {
                    mats.Add(meshRenderer.material);
                }
                StartCoroutine(UpdateTexture(selected.folderPath + @"\vgLowpoly.png", mats));
            }
            Debug.Log("Loaded FA-26B Skins");
        }
        */
        #endregion

        private IEnumerator UpdateTexture(string path, Material material)
        {
            Log("Updating Texture from path: " + path);
            if (material == null)
            {
                LogError("Material was null, not updating texture");
            }
            else
            {
                WWW www = new WWW("file:///" + path);
                while (!www.isDone)
                    yield return null;
                material.SetTexture("_MainTex", www.texture);
            }
        }

        private void ClampCount()
        {
            if (currentSkin < 0)
            {
                Debug.Log("Current Skin was below 0, moving to max amount which is " + (installedSkins.Count - 1));
                currentSkin = installedSkins.Count - 1;
            }
            else if (currentSkin > installedSkins.Count - 1)
            {
                Debug.Log("Current Skin was higher than the max amount of skins, reseting to 0");
                currentSkin = 0;
            }
        }
        private void UpdateUI()
        {
            if (installedSkins.Count == 0)
                return;
            StartCoroutine(UpdateUIEnumerator());
            Log("Current Skin = " + currentSkin);
        }
        private IEnumerator UpdateUIEnumerator()
        {
            string preview = @"";
            switch (VTOLAPI.GetPlayersVehicleEnum())
            {
                case VTOLVehicles.AV42C:
                    preview = @"\0.png";
                    break;
                case VTOLVehicles.FA26B:
                    preview = @"\1.png";
                    break;
                case VTOLVehicles.F45A:
                    preview = @"\2.png";
                    break;
            }
            WWW www = new WWW("file:///" + installedSkins[currentSkin].folderPath + preview);
            while (!www.isDone)
                yield return null;
            scenarioName.text = installedSkins[currentSkin].name;
            skinPreview.texture = www.texture;
        }
        private void OnDestroy()
        {
            VTOLAPI.SceneLoaded -= SceneLoaded;
        }
        
        private class Skin
        {
            public string name;
            public bool hasAv42c, hasFA26B, hasF45A;
            public string folderPath;
        }
    }
}
