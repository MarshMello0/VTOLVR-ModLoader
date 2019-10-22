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
        private ModLoaderManager manager;
        private VTOLAPI api;
        
        private GameObject modsPage;
        private ScrollRect Scroll_View;
        private Text SelectButton;
        private RectTransform selectionTF;
        private Mod selectedMod;
        private float buttonHeight = 548;
        private List<Mod> currentMods = new List<Mod>();

        
        private GameObject MainScreen;
        private CampaignInfoUI modInfoUI;
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
            Debug.Log("Creating UI for Ready Room");
            GameObject InteractableCanvas = GameObject.Find("InteractableCanvas");
            VRPointInteractableCanvas InteractableCanvasScript = InteractableCanvas.GetComponent<VRPointInteractableCanvas>();
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
            modsInteractable.OnInteract = GenerateEvent(delegate { ModsPageState(true); SetDefaultText(); });

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
            backInteractable.OnInteract = GenerateEvent(delegate { ModsPageState(false); });
            backInteractable.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();

            //Mod Info Page
            modInfoUI = modsPage.transform.GetChild(8).GetComponentInChildren<CampaignInfoUI>();

            Destroy(modsPage.transform.GetChild(8).GetChild(0).GetChild(3).gameObject);//percentCompleteText
            Destroy(modsPage.transform.GetChild(6).gameObject);//ResetButton
            Destroy(modsPage.transform.GetChild(2).gameObject);//PrevButton
            Destroy(modsPage.transform.GetChild(1).gameObject);//NextButton
            modsPage.transform.GetChild(0).GetComponent<Text>().text = "Select a Mod";

            //Getting the selection colour transform
            selectionTF = (RectTransform)modsPage.transform.GetChild(5).GetChild(0).GetChild(0).GetChild(0).transform;

            //Storing the prefab button for each item
            GameObject CampaignListTemplate = modsPage.transform.GetChild(5).GetChild(0).GetChild(0).GetChild(1).gameObject;
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
                Button currentButton = currentMods[i].listGO.transform.GetChild(2).GetComponent<Button>();
                currentButton.onClick.RemoveAllListeners(); //Trying to remove the existing button click
                Log("Added Mod:\n" + currentMods[i].name + "\n" + currentMods[i].description);
            }

            Log("Loaded " + currentMods.Count + " mods");
            Scroll_View.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + currentMods.Count) * buttonHeight);
            Scroll_View.ClampVertical();
            InteractableCanvasScript.RefreshInteractables();
            CampaignDisplay.SetActive(false);
            CampaignListTemplate.SetActive(false);
            modsPage.SetActive(false);

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

            IEnumerable<Type> source = from t in Assembly.Load(File.ReadAllBytes(selectedMod.dllPath)).GetTypes() where t.IsSubclassOf(typeof(VTOLMOD)) select t;
            if (source != null && source.Count() == 1)
            {
                GameObject newModGo = new GameObject(selectedMod.name, source.First());
                newModGo.name = selectedMod.name;
                DontDestroyOnLoad(newModGo);
                selectedMod.isLoaded = true;
                SelectButton.text = "Loaded!";
                Log("Loaded Mod " + selectedMod.name);

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
            if (selectedMod.image != null)
            {
                modInfoUI.campaignImage.color = Color.white;
                modInfoUI.campaignImage.material.SetTexture("_MainTex", selectedMod.image);
            }
        }
        private void SetDefaultText()
        {
            Debug.Log("Setting Default Text for mod");
            modInfoUI.campaignName.text = "";
            modInfoUI.campaignDescription.text = "";
            modInfoUI.campaignImage.color = new Color(0, 0, 0, 0);
            selectionTF.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        }
        /// <summary>
        /// Opens or closes the mods page
        /// </summary>
        /// <param name="state"></param>
        public void ModsPageState(bool state)
        {
            Log("Setting Mods page state to " + state);
            modsPage.SetActive(state);
            MainScreen.SetActive(!state);
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
    }
}
