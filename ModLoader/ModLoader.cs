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
        public static ModLoader instance { get; private set; }
        private ModLoaderManager manager;
        private VTOLAPI api;
        
        private GameObject modsPage, settingsPage, CampaignListTemplate, settingsCampaignListTemplate,settingsScrollBox;
        private GameObject s_StringTemplate, s_BoolTemplate, s_FloatTemplate, s_IntTemplate;
        private ScrollRect Scroll_View,settingsScrollView,settingsScrollBoxView;
        private Text SelectButton;
        private RectTransform selectionTF, settingsSelection;
        private Mod selectedMod;
        private float buttonHeight = 548;
        private List<Mod> currentMods = new List<Mod>();
        private List<Settings> currentSettings = new List<Settings>();
        private VRPointInteractableCanvas InteractableCanvasScript;


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
        private void CreateUI()
        {
            Debug.Log("Creating UI for Ready Room");
            GameObject InteractableCanvas = GameObject.Find("InteractableCanvas");
            InteractableCanvasScript = InteractableCanvas.GetComponent<VRPointInteractableCanvas>();
            GameObject NewPilotButton = GameObject.Find("NewPilotButton");
            GameObject CampaignDisplay = InteractableCanvas.transform.GetChild(0).GetChild(7).GetChild(0).GetChild(0).gameObject;
            MainScreen = GameObject.Find("MainScreen");

            //Creating Mods Button on Main Screen
            GameObject ModsButton = Instantiate(NewPilotButton, NewPilotButton.transform.parent);
            Vector3 oldPos = NewPilotButton.transform.position;
            ModsButton.transform.position = new Vector3(oldPos.x, oldPos.y - 0.3035235f, oldPos.z);

            Image ModsButtonImage = ModsButton.GetComponent<Image>();

            ModsButtonImage.color = Color.cyan;
            ModsButton.GetComponentInChildren<Text>().text = "Mods";

            VRInteractable modsInteractable = ModsButton.GetComponent<VRInteractable>();
            modsInteractable.interactableName = "Open Mods";
            modsInteractable.OnInteract = GenerateEvent(delegate { OpenPage(Pages.Mods); SetDefaultText(); });

            modsPage = Instantiate(CampaignDisplay, CampaignDisplay.transform.parent);
            modsPage.SetActive(true);

            //Select Button
            SelectButton = modsPage.transform.GetChild(3).GetComponentInChildren<Text>();
            SelectButton.text = "Load";
            VRInteractable selectVRI = SelectButton.transform.GetComponentInParent<VRInteractable>();
            selectVRI.interactableName = "Load Current Mod";
            selectVRI.OnInteract = GenerateEvent(delegate { LoadMod(); });
            selectVRI.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();

            //Back Button
            Button backButton = modsPage.transform.GetChild(4).GetComponent<Button>();
            VRInteractable backInteractable = modsPage.transform.GetChild(4).GetComponent<VRInteractable>();
            backInteractable.interactableName = "Back to main menu";
            backInteractable.OnInteract = GenerateEvent(delegate { OpenPage(Pages.MainMenu); });
            backInteractable.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();

            //Mod Info Page
            modInfoUI = modsPage.transform.GetChild(8).GetComponentInChildren<CampaignInfoUI>();

            //Settings Button
            modsPage.transform.GetChild(6).GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            modsPage.transform.GetChild(6).GetComponentInChildren<Text>().text = "Mod\nSettings";
            VRInteractable settingsInteractable = modsPage.transform.GetChild(6).GetComponent<VRInteractable>();
            settingsInteractable.interactableName = "Comming soon!";
            settingsInteractable.OnInteract = GenerateEvent(delegate { Debug.Log("Pressed the mods setting button"); });//settingsInteractable.OnInteract = GenerateEvent(delegate { OpenPage(Pages.Settings); });

            //Creating the Settings Page
            settingsPage = Instantiate(CampaignDisplay,CampaignDisplay.transform.parent);
            settingsPage.SetActive(true);

            settingsPage.transform.GetChild(0).GetComponent<Text>().text = "Mods Settings";

            VRInteractable settingsBackInteractable = settingsPage.transform.GetChild(4).GetComponent<VRInteractable>();
            settingsBackInteractable.interactableName = "Back to mods";
            settingsBackInteractable.OnInteract = GenerateEvent(delegate { OpenPage(Pages.Mods); });
            settingsBackInteractable.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();

            settingsScrollView = settingsPage.transform.GetChild(5).GetComponent<ScrollRect>();
            settingsSelection = (RectTransform)settingsPage.transform.GetChild(5).GetChild(0).GetChild(0).GetChild(0).transform;

            settingsCampaignListTemplate = settingsPage.transform.GetChild(5).GetChild(0).GetChild(0).GetChild(1).gameObject;
            settingsCampaignListTemplate.SetActive(false);

            settingsScrollBox = Instantiate(settingsPage.transform.GetChild(5).gameObject, settingsPage.transform);
            RectTransform settingsScrollBoxRect = settingsScrollBox.GetComponent<RectTransform>();
            settingsScrollBoxRect.localPosition = new Vector3(-176.2f, 1.3f, 0);
            settingsScrollBoxRect.sizeDelta = new Vector2(923.7f, 519.9f);
            settingsScrollBoxView = settingsScrollBox.GetComponent<ScrollRect>();
            settingsScrollBoxView.content.transform.GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(905.2f, 100);
            settingsScrollBoxView.content.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(905.2f, 100);

            s_BoolTemplate = Instantiate(settingsScrollBoxView.content.transform.GetChild(1).gameObject, settingsScrollBoxView.content);
            s_BoolTemplate.transform.GetChild(1).GetComponent<RectTransform>().localPosition = new Vector3(-224,0);
            s_BoolTemplate.transform.GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(446.2f, 80.6f);
            s_BoolTemplate.transform.GetChild(2).GetComponent<RectTransform>().localPosition = new Vector3(226, 0);
            s_BoolTemplate.transform.GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector2(280.7f, 50.7f);
            s_BoolTemplate.transform.GetChild(2).GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            s_BoolTemplate.transform.GetChild(2).GetComponent<VRInteractable>().interactableName = "Toggle Bool";
            Instantiate(s_BoolTemplate.transform.GetChild(1).gameObject, s_BoolTemplate.transform.GetChild(2));
            s_BoolTemplate.transform.GetChild(2).GetComponentInChildren<Text>().text = "Bool";
            s_BoolTemplate.transform.GetChild(2).GetComponentInChildren<RectTransform>().localPosition = new Vector3(0, 0);
            s_BoolTemplate.transform.GetChild(2).GetComponentInChildren<RectTransform>().sizeDelta = new Vector2(280.7f, 50.7f);

            s_BoolTemplate.SetActive(false);


            //Destorying Gameobjects that are not being used
            Destroy(modsPage.transform.GetChild(8).GetChild(0).GetChild(3).gameObject);//percentCompleteText
            Destroy(modsPage.transform.GetChild(2).gameObject);//PrevButton
            Destroy(modsPage.transform.GetChild(1).gameObject);//NextButton
            Destroy(settingsPage.transform.GetChild(8).gameObject);//CampselectMask
            Destroy(settingsPage.transform.GetChild(6).gameObject);//ResetButton
            Destroy(settingsPage.transform.GetChild(3).gameObject);//SelectButton
            Destroy(settingsPage.transform.GetChild(2).gameObject);//PrevButton
            Destroy(settingsPage.transform.GetChild(1).gameObject);//NextButton
            
            //Title 
            modsPage.transform.GetChild(0).GetComponent<Text>().text = "Select a Mod";

            //Getting the selection colour transform
            selectionTF = (RectTransform)modsPage.transform.GetChild(5).GetChild(0).GetChild(0).GetChild(0).transform;

            //Storing the prefab button for each item
            CampaignListTemplate = modsPage.transform.GetChild(5).GetChild(0).GetChild(0).GetChild(1).gameObject;
            Scroll_View = modsPage.transform.GetChild(5).GetComponent<ScrollRect>();

            buttonHeight = ((RectTransform)CampaignListTemplate.transform).rect.height;

            if (currentMods.Count == 0)
            {
                Log("Finding mods");
                currentMods = ModReader.GetMods(ModLoaderManager.instance.rootPath + @"\mods");
            }
            else
            {
                Log("Searching for any new mods\nCurrent Count = " + currentMods.Count);
                if (ModReader.GetNewMods(ModLoaderManager.instance.rootPath + @"\mods",ref currentMods))
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
                currentMods[i].listGO.GetComponent<VRUIListItemTemplate>().Setup(currentMods[i].name,i,OpenMod);
                //Button currentButton = currentMods[i].listGO.transform.GetChild(2).GetComponent<Button>();
                //currentButton.onClick.RemoveAllListeners(); //Trying to remove the existing button click
                Log("Added Mod:\n" + currentMods[i].name + "\n" + currentMods[i].description);
            }

            Log("Loaded " + currentMods.Count + " mods");
            Scroll_View.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + currentMods.Count) * buttonHeight);
            Scroll_View.ClampVertical();
            InteractableCanvasScript.RefreshInteractables();
            CampaignDisplay.SetActive(false);
            CampaignListTemplate.SetActive(false);
            OpenPage(Pages.MainMenu);

            SetDefaultText();
        }
        /// <summary>
        /// Removes existing events and creates a new unity event with an action
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private UnityEvent GenerateEvent(UnityAction action)
        {
            UnityEvent returnValue = new UnityEvent();
            returnValue.AddListener(action);
            return returnValue;
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
            Debug.Log("Setting Default Text for mod");
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
            for (int i = 0; i < currentMods.Count; i++)
            {
                if (currentMods[i].name.Equals(settings.Mod.name))
                {
                    Log("Found " + settings.Mod.name);
                    currentMods[i].settingsGO = Instantiate(settingsCampaignListTemplate, settingsScrollView.content);
                    currentMods[i].settingsGO.SetActive(true);
                    currentMods[i].settingsGO.transform.localPosition = new Vector3(0f, -(settingsScrollView.content.childCount - 5) * buttonHeight, 0f);
                    currentMods[i].settingsGO.GetComponent<VRUIListItemTemplate>().Setup(currentMods[i].name, i, OpenSetting);
                    currentSettings.Add(settings);
                    break;
                }
            }

            GameObject holder = new GameObject(settings.Mod.name);
            holder.transform.SetParent(settingsScrollBoxView.content);

            for (int i = 0; i < settings.subSettings.Count; i++)
            {
                if (settings.subSettings[i] is Settings.BoolSetting)
                {
                    Debug.Log("Spawning Bool Setting");
                    Settings.BoolSetting currentBool = (Settings.BoolSetting)settings.subSettings[i];
                    GameObject boolGO = Instantiate(s_BoolTemplate, holder.transform,true);
                    boolGO.transform.GetChild(1).GetComponent<Text>().text = currentBool.settingName;
                    boolGO.transform.GetChild(2).GetComponentInChildren<Text>().text = currentBool.defaultValue.ToString();
                    for (int j = 0; j < currentBool.callbacks.Length; j++)
                    {
                        boolGO.transform.GetChild(2).GetComponent<VRInteractable>().OnInteract.AddListener(delegate { currentBool.callbacks[j].Invoke(!currentBool.currentValue); });
                    }
                }
            }
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

            for (int i = 0; i < settingsScrollBoxView.content.childCount; i++)
            {
                settingsScrollBoxView.content.GetChild(i).gameObject.SetActive(false);
            }

            settingsScrollBoxView.content.Find(selectedMod.name).gameObject.SetActive(true);
        }
    }
}
