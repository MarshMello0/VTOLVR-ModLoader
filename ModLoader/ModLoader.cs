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
    public class ModLoader : VTOLMOD
    {
        public enum Pages { MainMenu,Mods,Settings}
        public enum KeyboardType { DisableAll, Int, Float, String }
        public static ModLoader instance { get; private set; }
        public static AssetBundle assetBundle;
        private ModLoaderManager manager;
        private VTOLAPI api;
        
        private GameObject modsPage, settingsPage, CampaignListTemplate, settingsCampaignListTemplate,settingsScrollBox;
        private GameObject s_StringTemplate, s_BoolTemplate, s_FloatTemplate, s_IntTemplate,s_CustomLabel,s_Holder;
        private ScrollRect Scroll_View,settingsScrollView,settingsScrollBoxView;
        private Text SelectButton;
        private RectTransform selectionTF, settingsSelection;
        private Mod selectedMod;
        private float buttonHeight = 548;
        private List<Mod> currentMods = new List<Mod>();
        private List<Settings> currentSettings = new List<Settings>();
        private VRPointInteractableCanvas InteractableCanvasScript;
        private VRKeyboard stringKeyboard, floatKeyboard, intKeyboard;
        private string currentSelectedSetting = string.Empty;


        private GameObject MainScreen;
        private CampaignInfoUI modInfoUI;
        private void Awake()
        {
            if (instance)
                Destroy(this.gameObject);

            instance = this;
            Mod mod = new Mod();
            mod.name = "Mod Loader";
            SetModInfo(mod);
            StartCoroutine(LoadAssetBundle());
        }
        private void Start()
        {
            manager = ModLoaderManager.instance;
            api = VTOLAPI.instance;

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
        private IEnumerator LoadAssetBundle()
        {
            Log("Loading Asset Bundle");
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(Directory.GetCurrentDirectory() + @"\VTOLVR_ModLoader\modloader.assets");
            yield return request;
            assetBundle = request.assetBundle;
            Log("AssetBundle Loaded");
        }
        private void CreateUI()
        {
            if (!assetBundle)
                LogError("Asset Bundle is null");

            Log("Creating UI for Ready Room");
            GameObject InteractableCanvas = GameObject.Find("InteractableCanvas");
            if (InteractableCanvas == null)
                LogError("InteractableCanvas was null");
            InteractableCanvasScript = InteractableCanvas.GetComponent<VRPointInteractableCanvas>();
            GameObject CampaignDisplay = InteractableCanvas.transform.GetChild(0).GetChild(7).GetChild(0).GetChild(0).gameObject;
            if (CampaignDisplay == null)
                LogError("CampaignDisplay was null");
            CampaignDisplay.SetActive(true);
            MainScreen = GameObject.Find("MainScreen");
            if (MainScreen == null)
                LogError("Main Screen was null");

            Log("Spawning Keyboards");
            stringKeyboard = Instantiate(assetBundle.LoadAsset<GameObject>("StringKeyboard")).GetComponent<VRKeyboard>();
            floatKeyboard = Instantiate(assetBundle.LoadAsset<GameObject>("FloatKeyboard")).GetComponent<VRKeyboard>();
            intKeyboard = Instantiate(assetBundle.LoadAsset<GameObject>("IntKeyboard")).GetComponent<VRKeyboard>();
            stringKeyboard.gameObject.SetActive(false);
            floatKeyboard.gameObject.SetActive(false);
            intKeyboard.gameObject.SetActive(false);

            Log("Creating Mods Button");//Mods Button
            GameObject NewPilotButton = GameObject.Find("NewPilotButton");
            GameObject ModsButton = Instantiate(assetBundle.LoadAsset<GameObject>("ModsButton"), NewPilotButton.transform.parent);
            Vector3 oldPos = NewPilotButton.transform.position;
            ModsButton.transform.position = new Vector3(oldPos.x, oldPos.y - 0.3035235f, oldPos.z);
            VRInteractable modsInteractable = ModsButton.GetComponent<VRInteractable>();
            modsInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.Mods); SetDefaultText(); });

            Log("Creating Mods Page");//Mods Page
            modsPage = Instantiate(assetBundle.LoadAsset<GameObject>("ModLoaderDisplay"), CampaignDisplay.transform.parent);

            CampaignListTemplate = modsPage.transform.GetChild(3).GetChild(0).GetChild(0).GetChild(1).gameObject;
            Scroll_View = modsPage.transform.GetChild(3).GetComponent<ScrollRect>();
            buttonHeight = ((RectTransform)CampaignListTemplate.transform).rect.height;
            selectionTF = (RectTransform)modsPage.transform.GetChild(3).GetChild(0).GetChild(0).GetChild(0).transform;
            modInfoUI = modsPage.transform.GetChild(5).GetComponentInChildren<CampaignInfoUI>();
            SelectButton = modsPage.transform.GetChild(1).GetComponentInChildren<Text>();
            VRInteractable selectVRI = modsPage.transform.GetChild(1).GetComponent<VRInteractable>();
            if (selectVRI == null)
                LogError("selectVRI is null");
            selectVRI.OnInteract.AddListener(LoadMod);
            VRInteractable backInteractable = modsPage.transform.GetChild(2).GetComponent<VRInteractable>();
            if (backInteractable == null)
                LogError("backInteractable is null");
            backInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.MainMenu); });
            VRInteractable settingsInteractable = modsPage.transform.GetChild(4).GetComponent<VRInteractable>();
            settingsInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.Settings); });
            

            if (currentMods.Count == 0)
            {
                Log("Finding mods");
                currentMods = ModReader.GetMods(ModLoaderManager.instance.rootPath + @"\mods");
            }
            else
            {
                Log("Searching for any new mods\nCurrent Count = " + currentMods.Count);
                if (ModReader.GetNewMods(ModLoaderManager.instance.rootPath + @"\mods", ref currentMods))
                {
                    Log("Found new mods\nNew count = " + currentMods.Count);
                }
                else
                {
                    Log("Didn't find any new mods");
                }
            }


            for (int i = 0; i < currentMods.Count; i++)
            {
                currentMods[i].listGO = Instantiate(CampaignListTemplate, Scroll_View.content);
                currentMods[i].listGO.transform.localPosition = new Vector3(0f, -i * buttonHeight, 0f);
                currentMods[i].listGO.GetComponent<VRUIListItemTemplate>().Setup(currentMods[i].name, i, OpenMod);
                //Button currentButton = currentMods[i].listGO.transform.GetChild(2).GetComponent<Button>();
                //currentButton.onClick.RemoveAllListeners(); //Trying to remove the existing button click
                Log("Added Mod:\n" + currentMods[i].name + "\n" + currentMods[i].description);
            }

            Log("Loaded " + currentMods.Count + " mods");

            Log("Mod Settings");//Mod Setttings
            settingsPage = Instantiate(assetBundle.LoadAsset<GameObject>("Mod Settings"), CampaignDisplay.transform.parent);
            settingsSelection = (RectTransform)settingsPage.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).transform;
            
            Transform s_content = settingsPage.transform.GetChild(2).GetChild(0).GetChild(0);
            s_BoolTemplate = assetBundle.LoadAsset<GameObject>("BoolTemplate");
            s_StringTemplate = assetBundle.LoadAsset<GameObject>("StringTemplate");
            s_IntTemplate = assetBundle.LoadAsset<GameObject>("NumberTemplate");
            s_CustomLabel = assetBundle.LoadAsset<GameObject>("CustomLabel");
            s_FloatTemplate = s_IntTemplate;
            s_Holder = modsPage.transform.GetChild(5).gameObject;

            settingsCampaignListTemplate = settingsPage.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject;
            settingsCampaignListTemplate.SetActive(false);
            VRInteractable settingsBackInteractable = settingsPage.transform.GetChild(3).GetComponent<VRInteractable>();
            settingsBackInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.Mods); });
            settingsScrollBox = settingsPage.transform.GetChild(2).gameObject;
            settingsScrollBoxView = settingsScrollBox.GetComponent<ScrollRect>();
            settingsScrollView = settingsPage.transform.GetChild(1).GetComponent<ScrollRect>();

            Log("Finished clearning up");//Finished and clearning up
            OpenPage(Pages.MainMenu);
            Scroll_View.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + currentMods.Count) * buttonHeight);
            Scroll_View.ClampVertical();
            InteractableCanvasScript.RefreshInteractables();
            CampaignDisplay.SetActive(false);
            CampaignListTemplate.SetActive(false);
            SetDefaultText();
        }
        public void LoadMod()
        {
            if (selectedMod == null)
            {
                LogError("There was no selected mod");
                return;
            }

            if (selectedMod.isLoaded)
            {
                Log(selectedMod.name + " is already loaded");
                return;
            }

            IEnumerable<Type> source = 
                from t in Assembly.Load(File.ReadAllBytes(selectedMod.dllPath)).GetTypes()
                where t.IsSubclassOf(typeof(VTOLMOD))
                select t;
            if (source != null && source.Count() == 1)
            {
                GameObject newModGo = new GameObject(selectedMod.name, source.First());
                VTOLMOD mod = newModGo.GetComponent<VTOLMOD>();
                mod.SetModInfo(selectedMod);
                newModGo.name = selectedMod.name;
                DontDestroyOnLoad(newModGo);
                selectedMod.isLoaded = true;
                SelectButton.text = "Loaded!";
                mod.ModLoaded();

                ModLoaderManager.instance.loadedModsCount++;
                ModLoaderManager.instance.UpdateDiscord();
            }
            else
            {
                LogError("Source is null");
            }
        }
        public void OpenMod(int id)
        {
            if (id > currentMods.Count - 1)
            {
                LogError("Open Mods tried to open a number too high.");
                return;
            }
            Log("Opening Mod " + id);
            selectedMod = currentMods[id];
            SelectButton.text = selectedMod.isLoaded ? "Loaded!" : "Load";
            Scroll_View.ViewContent((RectTransform)selectedMod.listGO.transform);
            selectionTF.position = selectedMod.listGO.transform.position;
            selectionTF.GetComponent<Image>().color = new Color(0.3529411764705882f, 0.196078431372549f, 0);
            modInfoUI.campaignName.text = selectedMod.name;
            modInfoUI.campaignDescription.text = selectedMod.description;
            if (!string.IsNullOrWhiteSpace(selectedMod.imagePath))
            {
                modInfoUI.campaignImage.color = Color.white;
                StartCoroutine(SetModPreviewImage(modInfoUI.campaignImage, selectedMod.imagePath));
            }
            else
            {
                modInfoUI.campaignImage.color = new Color(0, 0, 0, 0);
            }
        }
        private void SetDefaultText()
        {
            Log("Setting Default Text for mod");
            modInfoUI.campaignName.text = "";
            modInfoUI.campaignDescription.text = "";
            modInfoUI.campaignImage.color = new Color(0, 0, 0, 0);
            selectionTF.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            settingsSelection.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        }
        public void OpenPage(Pages page)
        {
            Log("Opening Page " + page.ToString());
            modsPage.SetActive(false);
            MainScreen.SetActive(false);
            settingsPage.SetActive(false);

            switch (page)
            {
                case Pages.MainMenu:
                    MainScreen.SetActive(true);
                    break;
                case Pages.Mods:
                    modsPage.SetActive(true);
                    break;
                case Pages.Settings:
                    settingsPage.SetActive(true);
                    break;
                default:
                    break;
            }
        }

        private IEnumerator SetModPreviewImage(RawImage raw, string path)
        {
            if (raw == null)
                Debug.Log("Mat is null");
            WWW www = new WWW("file:///" + path);
            while (!www.isDone)
                yield return null;
            raw.texture = www.texture;
        }
        public void CreateSettingsMenu(Settings settings)
        {
            int currentModIndex = FindModIndex(settings.Mod.thisMod.name);

            currentMods[currentModIndex].settingsGO = Instantiate(settingsCampaignListTemplate, settingsScrollView.content);
            currentMods[currentModIndex].settingsGO.SetActive(true);
            currentMods[currentModIndex].settingsGO.transform.localPosition = new Vector3(0f, -(settingsScrollView.content.childCount - 5) * buttonHeight, 0f);
            currentMods[currentModIndex].settingsGO.GetComponent<VRUIListItemTemplate>().Setup(currentMods[currentModIndex].name, currentModIndex, OpenSetting);
            currentSettings.Add(settings);

            currentMods[currentModIndex].settingsHolerGO = new GameObject(currentMods[currentModIndex].name, typeof(RectTransform));
            currentMods[currentModIndex].settingsHolerGO.transform.SetParent(s_Holder.transform, false);

            for (int i = 0; i < settings.subSettings.Count; i++)
            {
                if (settings.subSettings[i] is Settings.BoolSetting)
                {
                    Log("Found Bool Setting");
                    Settings.BoolSetting currentBool = (Settings.BoolSetting)settings.subSettings[i];
                    GameObject boolGO = Instantiate(s_BoolTemplate, currentMods[currentModIndex].settingsHolerGO.transform, false);
                    boolGO.transform.GetChild(1).GetComponent<Text>().text = currentBool.settingName;
                    currentBool.text = boolGO.transform.GetChild(2).GetComponentInChildren<Text>();
                    currentBool.text.text = currentBool.defaultValue.ToString();
                    boolGO.transform.GetChild(2).GetComponent<VRInteractable>().OnInteract.AddListener(delegate { currentBool.Invoke(); });
                    boolGO.SetActive(true);
                    
                    Log($"Spawned Bool Setting. Name:{currentBool.settingName} at {boolGO.transform.position}");
                }
                else if (settings.subSettings[i] is Settings.FloatSetting)
                {
                    Log("Found Float Setting");
                    Settings.FloatSetting currentFloat = (Settings.FloatSetting)settings.subSettings[i];
                    GameObject floatGO = Instantiate(s_FloatTemplate, currentMods[currentModIndex].settingsHolerGO.transform, false);
                    floatGO.transform.GetChild(1).GetComponent<Text>().text = currentFloat.settingName;
                    currentFloat.text = floatGO.transform.GetChild(2).GetComponent<Text>();
                    currentFloat.text.text = currentFloat.value.ToString();
                    floatGO.transform.GetChild(3).GetComponent<VRInteractable>().OnInteract.AddListener(delegate {
                        OpenKeyboard(KeyboardType.Float, currentFloat.value.ToString(), 32, new UnityAction<string>(currentFloat.SetValue)); 
                    });
                    floatGO.SetActive(true);
                    Log($"Spawned Float setting called {currentFloat.settingName} at {floatGO.transform.position}");
                }
                else if (settings.subSettings[i] is Settings.IntSetting)
                {
                    Log("Found Int Setting");
                    Settings.IntSetting currentInt = (Settings.IntSetting)settings.subSettings[i];
                    GameObject intGO = Instantiate(s_IntTemplate, currentMods[currentModIndex].settingsHolerGO.transform, false);
                    intGO.transform.GetChild(1).GetComponent<Text>().text = currentInt.settingName;
                    currentInt.text = intGO.transform.GetChild(2).GetComponent<Text>();
                    currentInt.text.text = currentInt.value.ToString();
                    intGO.transform.GetChild(3).GetComponent<VRInteractable>().OnInteract.AddListener(delegate {
                        OpenKeyboard(KeyboardType.Int, currentInt.value.ToString(), 32, new UnityAction<string>(currentInt.SetValue));
                    });
                    intGO.SetActive(true);
                    Log($"Spawned Int setting called {currentInt.settingName} at {intGO.transform.position}");

                }
                else if (settings.subSettings[i] is Settings.StringSetting)
                {
                    Log("Found String Setting");
                    Settings.StringSetting currentString = (Settings.StringSetting)settings.subSettings[i];
                    GameObject stringGO = Instantiate(s_StringTemplate, currentMods[currentModIndex].settingsHolerGO.transform, false);
                    stringGO.transform.GetChild(1).GetComponent<Text>().text = currentString.settingName;
                    currentString.text = stringGO.transform.GetChild(2).GetComponentInChildren<Text>();
                    currentString.text.text = currentString.value;
                    stringGO.transform.GetChild(3).GetComponent<VRInteractable>().OnInteract.AddListener(delegate { 
                        OpenKeyboard(KeyboardType.String, currentString.value, 32, new UnityAction<string>(currentString.SetValue)); 
                    });
                    stringGO.SetActive(true);
                    Log($"Spawned String setting called {currentString.settingName} at {stringGO.transform.position}");
                }
                else if (settings.subSettings[i] is Settings.CustomLabel)
                {
                    Log("Found a custom label");
                    Settings.CustomLabel currentLabel = (Settings.CustomLabel)settings.subSettings[i];
                    GameObject label = Instantiate(s_CustomLabel, currentMods[currentModIndex].settingsHolerGO.transform, false);
                    label.GetComponentInChildren<Text>().text = currentLabel.settingName;
                    label.SetActive(true);
                    Log($"Spawned a custom label with the text:\n{currentLabel.settingName}");
                }
            }
            currentMods[currentModIndex].settingsHolerGO.SetActive(false);
            Debug.Log("Done spawning " + settings.subSettings.Count + " settings");
            RefreashSettings();
        }

        private void RefreashSettings()
        {
            settingsScrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + settingsScrollView.content.childCount) * buttonHeight);
            settingsScrollView.ClampVertical();
            settingsScrollBoxView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + 1) * 100);
            settingsScrollBoxView.ClampVertical();
            InteractableCanvasScript.RefreshInteractables();
        }
        public void OpenSetting(int id)
        {
            if (id > currentMods.Count - 1)
            {
                LogError("Open Mods tried to open a number too high.");
                return;
            }
            Log("Opening Settings for mod " + id);
            Mod selectedMod = currentMods[id];
            settingsScrollView.ViewContent((RectTransform)selectedMod.settingsGO.transform);
            settingsSelection.position = selectedMod.settingsGO.transform.position;
            settingsSelection.GetComponent<Image>().color = new Color(0.3529411764705882f, 0.196078431372549f, 0);

            if(currentSelectedSetting != string.Empty) 
            {
                //There already is something on the content of the settings scroll box.
                MoveBackToPool(currentSelectedSetting);
            }
            MoveToSettingsView(selectedMod.settingsHolerGO.transform);
            RefreashSettings();
        }
        private void MoveToSettingsView(Transform parent)
        {
            currentSelectedSetting = parent.name;
            //They need to be stored in a temp array so that we can move them all.
            Transform[] children = new Transform[parent.childCount];
            for (int i = 0; i < parent.childCount; i++)
            {
                children[i] = parent.GetChild(i);
            }
            for (int i = 0; i < children.Length; i++)
            {
                children[i].SetParent(settingsScrollBoxView.content, false);
            }
        }
        private void MoveBackToPool(string name)
        {
            int modIndex = FindModIndex(name);
            Transform holder = s_Holder.transform.Find(currentMods[modIndex].settingsHolerGO.name);
            if (holder == null)
            {
                //Couldn't find the holder for some reason in the pool.
                holder = new GameObject(currentMods[modIndex].name, typeof(RectTransform)).transform;
                holder.SetParent(s_Holder.transform, false);
                currentMods[modIndex].settingsHolerGO = holder.gameObject;
            }

            for (int i = 0; i < settingsScrollBoxView.content.childCount; i++)
            {
                Debug.LogWarning($"Moving {settingsScrollBoxView.content.GetChild(i)} Back to holder");
                settingsScrollBoxView.content.GetChild(i).SetParent(holder, false);
            }
        }
        /// <summary>
        /// Finds the index of the current mod by its name.
        /// </summary>
        /// <param name="name">Mod.name value of what you want to find</param>
        /// <returns>The index of where this mod is in the currentMods list</returns>
        private int FindModIndex(string name)
        {
            int returnValue = -1;
            for (int i = 0; i < currentMods.Count; i++)
            {
                if (currentMods[i].name.Equals(name))
                {
                    returnValue = i;
                    break;
                }
            }
            return returnValue;
        }
        public void OpenKeyboard(KeyboardType keyboardType,string startingText, int maxChars, UnityAction<string> onEntered, UnityAction onCancelled = null)
        {
            OpenKeyboard(KeyboardType.DisableAll); //Closing them all first incase one is opened
            if (keyboardType == KeyboardType.DisableAll)
                return;
            switch (keyboardType)
            {
                case KeyboardType.Int:
                    intKeyboard.Display(startingText, maxChars, onEntered, onCancelled);
                    break;
                case KeyboardType.Float:
                    floatKeyboard.Display(startingText, maxChars, onEntered, onCancelled);
                    break;
                case KeyboardType.String:
                    stringKeyboard.Display(startingText, maxChars, onEntered, onCancelled);
                    break;
            }
        }
        public void OpenKeyboard(KeyboardType keyboardType)
        {
            if (keyboardType != KeyboardType.DisableAll)
                return;
            stringKeyboard.gameObject.SetActive(false);
            intKeyboard.gameObject.SetActive(false);
            floatKeyboard.gameObject.SetActive(false);
        }
    }
}
