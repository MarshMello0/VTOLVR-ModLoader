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
using System.Net;
using System.Reflection;

namespace ModLoader
{
    [Info("ModLoader", "This is the core script for the mod loader", "0")]
    public class ModLoader : VTOLMOD
    {
        private ModLoaderManager manager;
        private VTOLAPI api;
        //UI Objects
        GameObject warningPage, spmp, sp, mp, spModPage, spList, mpPV, mpIPPort, mpServerInfo, mpBanned;
        PoseBounds pb;
        public enum Page { warning, spmp, spMod, spList, mpPV, mpIPPort, mpServerInfo, mpBanned }

        private Transform spTransform;

        private string mods = @"\mods";
        private string root;

        private ModSlot[] modSlots = new ModSlot[8];
        private List list;
        private VRInteractable nextInteractable, previousInteractable, loadInteractable;
        private Text modTitleText, modDescriptionText, loadModText;
        private Material nextMaterial, previousMaterial, loadModMaterial, redMaterial, greenMaterial;
        private APIMod[] apimods = new APIMod[1];

        //New
        private GameObject modsPage;
        private ScrollRect Scroll_View;
        private Text SelectButton;
        private int selectedMod = -1;
        private float buttonHeight = 548;

        //InGameObjects
        private GameObject MainScreen;

        private void Start()
        {
            manager = ModLoaderManager.instance;
            api = VTOLAPI.instance;

            //Spawning UConsole
            //GameObject uConsole = Instantiate(manager.assets.LoadAsset<GameObject>("UConsole-Canvas"));
            //UConsole console = uConsole.AddComponent<UConsole>();

            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            switch (scene.name)
            {
                case "ReadyRoom":
                    CreateUI();
                    break;
                case "Akutan":
                    break;
                default:
                    break;
            }
        }
        private void CreateUI()
        {
            GameObject InteractableCanvas = GameObject.Find("InteractableCanvas");
            VRPointInteractableCanvas InteractableCanvasScript = InteractableCanvas.GetComponent<VRPointInteractableCanvas>();
            GameObject NewPilotButton = GameObject.Find("NewPilotButton");
            GameObject CampaignDisplay = InteractableCanvas.transform.GetChild(0).GetChild(7).GetChild(0).GetChild(0).gameObject;
            MainScreen = GameObject.Find("MainScreen");
            GameObject ModsButton = Instantiate(NewPilotButton, NewPilotButton.transform.parent);
            ModsButton.name = "ModsButton";
            Vector3 oldPos = NewPilotButton.transform.position;
            ModsButton.transform.position = new Vector3(oldPos.x, oldPos.y - 0.3035235f, oldPos.z);
            ModsButton.GetComponentInChildren<Text>().text = "Mods";
            ModsButton.GetComponent<Image>().color = Color.cyan;

            VRInteractable modsInteractable = ModsButton.GetComponent<VRInteractable>();
            modsInteractable.interactableName = "Open Mods";
            modsInteractable.OnInteract = new UnityEvent();
            modsInteractable.OnInteract.AddListener(OpenModsPage);
            InteractableCanvasScript.RefreshInteractables();

            Log(modsInteractable.gameObject.name);
            modsPage = Instantiate(CampaignDisplay, CampaignDisplay.transform.parent);
            modsPage.SetActive(true);
            //Select Button
            SelectButton = modsPage.transform.GetChild(3).GetComponentInChildren<Text>();
            SelectButton.text = "Load";
            VRInteractable selectVRI = SelectButton.transform.GetComponentInParent<VRInteractable>();
            selectVRI.interactableName = "Load Current Mod";
            selectVRI.OnInteract = new UnityEvent();
            selectVRI.OnInteract.AddListener(LoadMod);
            Destroy(modsPage.transform.GetChild(8).GetChild(0).GetChild(3).gameObject);//percentCompleteText
            Destroy(modsPage.transform.GetChild(6).gameObject);//ResetButton
            Destroy(modsPage.transform.GetChild(2).gameObject);//PrevButton
            Destroy(modsPage.transform.GetChild(1).gameObject);//NextButton
            modsPage.transform.GetChild(0).GetComponent<Text>().text = "Select a Mod";

            //Storing the prefab button for each item
            GameObject CampaignListTemplate = modsPage.transform.GetChild(5).GetChild(0).GetChild(0).GetChild(1).gameObject;
            Scroll_View = modsPage.transform.GetChild(5).GetComponent<ScrollRect>();

            buttonHeight = ((RectTransform)CampaignListTemplate.transform).rect.height;
            if (ModLoaderManager.mods.Count == 0)
            {
                ModLoaderManager.FindMods();
            }

            for (int i = 0; i < ModLoaderManager.mods.Count; i++)
            {
                ModLoaderManager.mods[i].listGO = Instantiate(CampaignListTemplate, Scroll_View.content);
                ModLoaderManager.mods[i].listGO.transform.localPosition = new Vector3(0f, -i * buttonHeight, 0f);
                ModLoaderManager.mods[i].listGO.GetComponent<VRUIListItemTemplate>().Setup(ModLoaderManager.mods[i].name,i,OpenMod);
            }

            Scroll_View.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + ModLoaderManager.mods.Count) * buttonHeight);
            Scroll_View.ClampVertical();
            InteractableCanvasScript.RefreshInteractables();
            CampaignDisplay.SetActive(false);
            CampaignListTemplate.SetActive(false);
            //modsPage.SetActive(false);

        }
        public void LoadMod()
        {
            if (selectedMod < 0)
                return;

            ModItem item = ModLoaderManager.mods[selectedMod];
            if (item.isLoaded)
            {
                Debug.Log(item.name + " is already loaded");
                return;
            }

            IEnumerable<Type> source = from t in item.assembly.GetTypes() where t.IsSubclassOf(typeof(VTOLMOD)) select t;
            if (source != null && source.Count() == 1)
            {
                new GameObject(item.name, source.First());
                ModLoaderManager.mods[selectedMod].isLoaded = true;

                ModLoaderManager.instance.loadedModsCount++;
                ModLoaderManager.instance.UpdateDiscord();
            }
            else
            {
                Debug.LogError("Source is null");
            }
        }
        public void OpenMod(int id)
        {
            selectedMod = id;
            SelectButton.text = ModLoaderManager.mods[id].isLoaded ? "Loaded!" : "Load";
            Scroll_View.ViewContent((RectTransform)ModLoaderManager.mods[selectedMod].listGO.transform);
        }
        public void OpenModsPage()
        {
            Log("Opened the mods page");
            modsPage.SetActive(true);
            MainScreen.SetActive(false);
        }
        public void CloseModsPage()
        {
            Log("Closing the mods page");
            modsPage.SetActive(false);
            MainScreen.SetActive(true);
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
            warningPage = canvasT.GetChild(2).gameObject;
            spmp = canvasT.GetChild(3).gameObject;
            sp = canvasT.GetChild(4).gameObject;
            mp = canvasT.GetChild(5).gameObject;
            spModPage = sp.transform.GetChild(0).gameObject;
            spList = sp.transform.GetChild(1).gameObject;
            mpPV = mp.transform.GetChild(0).gameObject;
            mpIPPort = mp.transform.GetChild(1).gameObject;
            mpServerInfo = mp.transform.GetChild(2).gameObject;
            mpBanned = mp.transform.GetChild(3).gameObject;

            //Setting outline in background
            Image outline = canvasT.GetChild(1).GetComponent<Image>();
            Texture2D tex = Resources.FindObjectsOfTypeAll(typeof(Texture2D)).Where(x => x.name == "crtScreenEffect").ToArray()[0] as Texture2D;
            outline.sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            //Setting PoseBounds
            RectTransform canvasRect = canvasT.GetComponent<RectTransform>();
            pb = canvasT.gameObject.AddComponent<PoseBounds>();
            pb.pose = GloveAnimation.Poses.Point;
            pb.size = new Vector3(canvasRect.rect.width, canvasRect.rect.height, 155.0f);

            //Adding Scripts

            //Warning Page
            VRInteractable warningPagevrInteractable = warningPage.transform.GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(warningPagevrInteractable, pb);
            warningPagevrInteractable.interactableName = "Okay";
            warningPagevrInteractable.OnInteract.AddListener(delegate { SwitchPage(Page.spmp); });
            //SP/MP
            VRInteractable spButton = spmp.transform.GetChild(1).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable mpButton = spmp.transform.GetChild(2).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(spButton, pb);
            SetDefaultInteractable(mpButton, pb);
            spButton.interactableName = "Start Singleplayer";
            //mpButton.interactableName = "Start Multiplayer";
            mpButton.interactableName = "Not Available Yet";
            spButton.OnInteract.AddListener(delegate { SwitchPage(Page.spList); SetupSinglePlayer(); });
            //mpButton.OnInteract.AddListener(delegate { SwitchPage(Page.mpPV); SetupMultiplayer(); });
            mpButton.OnInteract.AddListener(delegate { Log("Multiplayer Button was pressed :P"); });
            //SP Mod Page
            VRInteractable spModBack = spModPage.transform.GetChild(3).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable spModLoad = spModPage.transform.GetChild(2).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(spModBack, pb);
            SetDefaultInteractable(spModLoad, pb);
            spModBack.interactableName = "Back to list";
            spModLoad.interactableName = "Load Mod";
            spModBack.OnInteract.AddListener(delegate { SwitchPage(Page.spList); });
            //SP List
            VRInteractable spListSwitch = spList.transform.GetChild(2).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable spListNextPage = spList.transform.GetChild(0).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable spListPreviousPage = spList.transform.GetChild(1).GetChild(0).gameObject.AddComponent<VRInteractable>();
            VRInteractable spListStart = spList.transform.GetChild(11).GetChild(0).gameObject.AddComponent<VRInteractable>();
            SetDefaultInteractable(spListSwitch, pb);
            SetDefaultInteractable(spListNextPage, pb);
            SetDefaultInteractable(spListPreviousPage, pb);
            SetDefaultInteractable(spListStart, pb);
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
            spListSwitch.OnInteract.AddListener(delegate { SwitchButton(); });
            spListNextPage.OnInteract.AddListener(delegate { NextPage(); });
            spListPreviousPage.OnInteract.AddListener(delegate { PreviousPage(); });
        }

        public static VRInteractable SetDefaultInteractable(VRInteractable interactable, PoseBounds pb)
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

            Log("Switched Page to " + page.ToString());
        }

        #endregion

        #region Singleplayer

        private void SetupSinglePlayer()
        {
            root = Directory.GetCurrentDirectory() + @"\VTOLVR_ModLoader";
            if (!Directory.Exists(root + mods))
                Directory.CreateDirectory(root + mods);
            spTransform = spList.transform;

            for (int i = 0; i <= 7; i++)
            {
                modSlots[i] = new ModSlot(spTransform.GetChild(i + 3).gameObject, spTransform.GetChild(i + 3).GetChild(1).GetComponent<Text>(), spTransform.GetChild(i + 3).GetChild(0).gameObject.AddComponent<VRInteractable>());
            }

            for (int i = 0; i <= 7; i++)
            {
                SetDefaultInteractable(modSlots[i].interactable, pb);
            }

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
            throw new NotImplementedException();
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
            public string Name, Description, Creator, URL, Version;
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
