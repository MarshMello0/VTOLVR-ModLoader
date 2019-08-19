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
    [Info("SkinManager","Handles selecting and loading skins","1.0.0")]
    class SkinManager : VTOLMOD
    {
        //This variables are used on different scenes
        private Dictionary<string, Material> skins;
        private List<Skin> installedSkins = new List<Skin>();
        private int selectedSkin = -1;

        //Vehicle Config scene only
        private int currentSkin;
        private Text scenarioName;
        private RawImage skinPreview;
        private void Start()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            Directory.CreateDirectory(ModLoaderManager.instance.rootPath + @"\skins");
        }

        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.buildIndex == 3)
            {
                //Vehicle Configuration Room
                Debug.Log("Started Skins Vehicle Config room");
                StartCoroutine(VehicleConfigurationScene());
            }
            else if (arg0.buildIndex == 7 || arg0.buildIndex == 11)
            {
                //In Game World
                Debug.Log("Setting Players Skin");
                StartCoroutine(GameScene());
            }
        }

        private void SetMaterials()
        {
            skins = new Dictionary<string, Material>();
            string vehicleName = PilotSaveManager.currentVehicle.vehiclePrefab.name + "(Clone)";
            GameObject vehicle = GameObject.Find(vehicleName);
            Debug.Log("Finding Materials on " + vehicle.name);
            switch (VTOLAPI.instance.GetPlayersVehicleEnum())
            {
                case VTOLVehicles.AV42C:
                    skins.Add("mat_vtol4Exterior", vehicle.transform.Find("VT4Body(new)").GetComponent<MeshRenderer>().material);
                    skins.Add("mat_vtol4Exterior2", vehicle.transform.Find("VT4Body(new)").GetChild(1).GetComponent<MeshRenderer>().material);
                    skins.Add("mat_vtol4Interior", vehicle.transform.Find("Body").GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material);
                    skins.Add("mat_vtol4TiltEngine", vehicle.transform.Find("Body").Find("WingRight").Find("RightEnginePart").GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetComponent<MeshRenderer>().material);
                    break;
                case VTOLVehicles.FA26B:
                    break;
                case VTOLVehicles.F45A:
                    break;
            }
        }

        private IEnumerator VehicleConfigurationScene()
        {
            while (SceneManager.GetActiveScene().buildIndex != 3)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1);
            //Vehicle scene is now the active one
            /*
              Dupe the left panel
              Delete its contents
              Find the skins
              Add my contents depnding on how many skins there are
              Change main vehicles skin when the select one
             */

            SetMaterials();

            GameObject MissionLauncher = GameObject.Find("MissionLauncher");

            yield return new WaitForSeconds(2);
            GameObject pannel = Instantiate(MissionLauncher);
            pannel.GetComponent<VehicleConfigScenarioUI>().enabled = false;
            pannel.GetComponent<TimedEvents>().enabled = false;
            pannel.transform.position = new Vector3(-83.822f, -15.68818f, 5.774f);
            pannel.transform.rotation = Quaternion.Euler(-180, 62.145f, 180);

            //Reusing the item already there
            Transform scenarioDisplayObject = pannel.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1);
            Text pageTitle = scenarioDisplayObject.GetChild(0).GetComponent<Text>();
            pageTitle.text = "Skins";
            PoseBounds pb = pannel.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponentInChildren<PoseBounds>();

            //Destroying Quit Button
            Destroy(scenarioDisplayObject.GetChild(2).gameObject);

            //Main Contents Page
            Transform selectMapPage = scenarioDisplayObject.GetChild(1);

            scenarioName = selectMapPage.GetChild(3).GetComponent<Text>();
            scenarioName.text = "No Skins Found";
            skinPreview = selectMapPage.GetChild(1).GetComponent<RawImage>();

            selectMapPage.GetChild(4).GetComponentInChildren<Text>().text = "Select";
            VRInteractable launchMissionButton = selectMapPage.GetChild(4).GetComponent<VRInteractable>();
            ModLoader.SetDefaultInteractable(launchMissionButton, pb);
            launchMissionButton.interactableName = "Select Skin";
            launchMissionButton.OnInteract.AddListener(delegate { SelectSkin();ApplySkin(); });

            Transform EnvironmentSelectObject = selectMapPage.GetChild(6);

            VRInteractable NextENVButton = EnvironmentSelectObject.GetChild(1).GetComponent<VRInteractable>();
            VRInteractable PrevENVButton = EnvironmentSelectObject.GetChild(2).GetComponent<VRInteractable>();
            ModLoader.SetDefaultInteractable(NextENVButton, pb);
            ModLoader.SetDefaultInteractable(PrevENVButton, pb);
            NextENVButton.interactableName = "Next";
            PrevENVButton.interactableName = "Previous";
            NextENVButton.OnInteract.AddListener(Next);
            PrevENVButton.OnInteract.AddListener(Previous);

            //Destroying Things
            Destroy(selectMapPage.GetChild(2).gameObject); //Description
            Destroy(EnvironmentSelectObject.GetChild(0).gameObject); // envName

            /* # Moving Up "Animation"
             Not doing the animation because it just doesn't go to the correct height,
             Instead I am duping it once the first one has finished its animation.

            Transform liftArm = pannel.transform.GetChild(0).GetChild(0).GetChild(0);
            TranslationToggle translationToggle = liftArm.GetComponent<TranslationToggle>();

            translationToggle.Toggle(); //For some reason our pannel goes too high.
            yield return new WaitForSeconds(2);
            liftArm.GetChild(0).GetComponent<RotationToggle>().Toggle();
            */

            FindSkins();
            UpdateUI();
        }
        private void FindSkins()
        {
            string path = ModLoaderManager.instance.rootPath + @"\skins";
            foreach (string folder in Directory.GetDirectories(path))
            {
                Skin currentSkin = new Skin();
                string[] split = folder.Split('\\');
                currentSkin.name = split[split.Length - 1];
                if (File.Exists(folder + @"\0.png")) //AV-42C
                {
                    currentSkin.hasAv42c = true;
                    if (File.Exists(folder + @"\vtol4Exterior.png"))
                        currentSkin.textures.Add("vtol4Exterior", folder + @"\vtol4Exterior.png");
                    if (File.Exists(folder + @"\vtol4Exterior2.png"))
                        currentSkin.textures.Add("vtol4Exterior2", folder + @"\vtol4Exterior2.png");
                    if (File.Exists(folder + @"\vtol4Interior.png"))
                        currentSkin.textures.Add("vtol4Interior", folder + @"\vtol4Interior.png");
                    if (File.Exists(folder + @"\vtol4TiltEngine.png"))
                        currentSkin.textures.Add("vtol4TiltEngine", folder + @"\vtol4TiltEngine.png");
                }

                if (File.Exists(folder + @"\1.png")) //FA26B
                {
                    currentSkin.hasFA26B = true;
                }

                if (File.Exists(folder + @"\2.png")) //F45A
                {
                    currentSkin.hasF45A = true;
                }

                if (currentSkin.hasAv42c || currentSkin.hasFA26B || currentSkin.hasF45A)
                {
                    currentSkin.folderPath = folder;
                    installedSkins.Add(currentSkin);
                    Debug.Log("Added Skin " + currentSkin.name);
                }
            }
        }
        public void Next()
        {
            currentSkin++;
            ClampCount();
            UpdateUI();
        }
        public void Previous()
        {
            currentSkin--;
            ClampCount();
            UpdateUI();
        }
        public void SelectSkin()
        {
            selectedSkin = currentSkin;
        }
        private void ApplySkin()
        {
            if (selectedSkin < 0)
                return;
            Skin selected = installedSkins[selectedSkin];
            switch (VTOLAPI.instance.GetPlayersVehicleEnum())
            {
                case VTOLVehicles.AV42C:
                    if (File.Exists(selected.folderPath + @"\vtol4Exterior.png"))
                        StartCoroutine(UpdateTexture(selected.folderPath + @"\vtol4Exterior.png", skins["mat_vtol4Exterior"]));
                    if (File.Exists(selected.folderPath + @"\vtol4Exterior2.png"))
                        StartCoroutine(UpdateTexture(selected.folderPath + @"\vtol4Exterior2.png", skins["mat_vtol4Exterior2"]));
                    if (File.Exists(selected.folderPath + @"\vtol4Interior.png"))
                        StartCoroutine(UpdateTexture(selected.folderPath + @"\vtol4Interior.png", skins["mat_vtol4Interior"]));
                    if (File.Exists(selected.folderPath + @"\vtol4TiltEngine.png"))
                        StartCoroutine(UpdateTexture(selected.folderPath + @"\vtol4TiltEngine.png", skins["mat_vtol4TiltEngine"]));
                    break;
                case VTOLVehicles.FA26B:
                    break;
                case VTOLVehicles.F45A:
                    break;
            }
        }

        private IEnumerator UpdateTexture(string path, Material material)
        {
            Debug.Log("Updating Texture from path: " + path);
            if (material == null)
            {
                Debug.LogError("Material was null, not updating texture");
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
                currentSkin = installedSkins.Count - 1;
            else if (currentSkin > installedSkins.Count - 1)
                currentSkin = 0;
        }
        private void UpdateUI()
        {
            if (installedSkins.Count == 0)
                return;
            StartCoroutine(UpdateUIEnumerator());
        }
        private IEnumerator UpdateUIEnumerator()
        {
            string preview = @"";
            switch (VTOLAPI.instance.GetPlayersVehicleEnum())
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

        private IEnumerator GameScene()
        {
            yield return new WaitForSeconds(3);
            Debug.Log("In game scene, setting skins");
            SetMaterials();
            ApplySkin();
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
        }

        private class Skin
        {
            public string name;
            public bool hasAv42c, hasFA26B, hasF45A;
            public string folderPath;
            public Dictionary<string, string> textures = new Dictionary<string, string>();
        }
    }
}
