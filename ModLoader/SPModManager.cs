using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine.UI;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using UnityEngine.Networking;
using System.Text;


public class SPModManager : MonoBehaviour
{
    public AssetBundle assets;
    public ModLoader.ModLoader modloader;
    public DiscordController discord;
    
    //This script needs to be attached onto "List" gameobject
    private List<ModItem> localMods = new List<ModItem>();
    private List<ModItem> onlineMods = new List<ModItem>();

    private string mods = @"\mods";
    private string root;
    private string apiURL = "http://vtolapi.kevinjoosten.nl/availableMods";
    private bool onLocal = true;

    private ModSlot[] modSlots = new ModSlot[8];
    private List list;
    private VRInteractable nextInteractable, previousInteractable, loadInteractable;
    private Text modTitleText, modDescriptionText, loadModText;
    private Material nextMaterial, previousMaterial, loadModMaterial, redMaterial, greenMaterial;
    private APIMod[] apimods = new APIMod[1];

    private void Awake()
    {
        root = Directory.GetCurrentDirectory() + @"\VTOLVR_ModLoader";
        if (!Directory.Exists(root + mods))
            Directory.CreateDirectory(root + mods);

        modSlots[0] = new ModSlot(transform.GetChild(3).gameObject, transform.GetChild(3).GetChild(1).GetComponent<Text>(), transform.GetChild(3).GetChild(0).gameObject.AddComponent<VRInteractable>());
        modSlots[1] = new ModSlot(transform.GetChild(4).gameObject, transform.GetChild(4).GetChild(1).GetComponent<Text>(), transform.GetChild(4).GetChild(0).gameObject.AddComponent<VRInteractable>());
        modSlots[2] = new ModSlot(transform.GetChild(5).gameObject, transform.GetChild(5).GetChild(1).GetComponent<Text>(), transform.GetChild(5).GetChild(0).gameObject.AddComponent<VRInteractable>());
        modSlots[3] = new ModSlot(transform.GetChild(6).gameObject, transform.GetChild(6).GetChild(1).GetComponent<Text>(), transform.GetChild(6).GetChild(0).gameObject.AddComponent<VRInteractable>());
        modSlots[4] = new ModSlot(transform.GetChild(7).gameObject, transform.GetChild(7).GetChild(1).GetComponent<Text>(), transform.GetChild(7).GetChild(0).gameObject.AddComponent<VRInteractable>());
        modSlots[5] = new ModSlot(transform.GetChild(8).gameObject, transform.GetChild(8).GetChild(1).GetComponent<Text>(), transform.GetChild(8).GetChild(0).gameObject.AddComponent<VRInteractable>());
        modSlots[6] = new ModSlot(transform.GetChild(9).gameObject, transform.GetChild(9).GetChild(1).GetComponent<Text>(), transform.GetChild(9).GetChild(0).gameObject.AddComponent<VRInteractable>());
        modSlots[7] = new ModSlot(transform.GetChild(10).gameObject, transform.GetChild(10).GetChild(1).GetComponent<Text>(), transform.GetChild(10).GetChild(0).gameObject.AddComponent<VRInteractable>());

        modloader.SetDefaultInteractable(modSlots[0].interactable);
        modloader.SetDefaultInteractable(modSlots[1].interactable);
        modloader.SetDefaultInteractable(modSlots[2].interactable);
        modloader.SetDefaultInteractable(modSlots[3].interactable);
        modloader.SetDefaultInteractable(modSlots[4].interactable);
        modloader.SetDefaultInteractable(modSlots[5].interactable);
        modloader.SetDefaultInteractable(modSlots[6].interactable);
        modloader.SetDefaultInteractable(modSlots[7].interactable);

        FindLocalMods();

        //Adding the materials from the asset bundle
        //redMaterial = assets.LoadAsset<Material>("Red");
        //greenMaterial = assets.LoadAsset<Material>("Green");
        redMaterial = new Material(Shader.Find("Diffuse"));
        redMaterial.color = Color.red;
        greenMaterial = new Material(Shader.Find("Diffuse"));
        greenMaterial.color = Color.green;

        UpdateList(true);
    }
    private void Start()
    {
        //modloader.onPageChanged += OnPageChanged;
    }

    private void FindLocalMods()
    {
        DirectoryInfo folder = new DirectoryInfo(root + mods);
        FileInfo[] files = folder.GetFiles("*.dll");
        localMods = new List<ModItem>(files.Length);

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
                localMods.Add(item);
            }            
        }
        
    }

    private IEnumerator FindOnlineMods()
    {
        //Featch the information and fill it into the list
        using (UnityWebRequest request = UnityWebRequest.Get(apiURL))
        {
            yield return request.SendWebRequest();

            string returnedJson = "{\"Items\":" + request.downloadHandler.text + "}";

            Debug.Log(returnedJson);
            //This for some reason keeps returning null as a mod in VTOL but in a new Unity Project its fine
            apimods = JsonHelper.FromJson<APIMod>(returnedJson);

            if (apimods == null)
                Debug.LogError("API is Null");
        
        onlineMods = new List<ModItem>(apimods.Length);
            foreach (APIMod mod in apimods)
            {
                onlineMods.Add(new ModItem(mod.Name, mod.Description, mod.URL, mod.Version, false));
                Debug.Log("Added mod " + mod.Name);
            }
            UpdateList(false);
        }
    }
    public void OnPageChanged(ModLoader.ModLoader.Page newPage)
    {
        if (newPage == ModLoader.ModLoader.Page.spList)
            UpdateList(true);
    }
    private void UpdateList(bool local)
    {
        List<ModItem> items = local ? localMods : onlineMods;
        list = new List(items);
        for (int i = 0; i < 8; i++)
        {
            if (list.mods.Count > i)
            {
                ModItem currentItem = list.mods[(list.currentPage * 8) + i];
                modSlots[i].slot.SetActive(true);
                modSlots[i].slotText.text = currentItem.name + " " + (currentItem.isLoaded ? "[Loaded]":"");
                Debug.Log("This mod is " + currentItem.isLoaded);
                modSlots[i].interactable.interactableName = "View " + currentItem.name + "[Try Grip if trigger doesn't work]";
                modSlots[i].interactable.button = VRInteractable.Buttons.Trigger;
                modSlots[i].interactable.OnInteract.AddListener(delegate { OpenMod(currentItem, local); });
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

    public void OpenMod(ModItem item, bool isLocal)
    {
        Debug.Log("Opening Mod " + item.name);
        
        modTitleText.text = item.name;
        modDescriptionText.text = item.description;
        loadInteractable.OnInteract.RemoveAllListeners();
        modloader.SwitchPage(ModLoader.ModLoader.Page.spMod);
        if (isLocal)
        {
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
        else
        {
            //loadModText.text = "Download";
            //loadModMaterial.color = Color.green;
            //loadInteractable.OnInteract.AddListener(delegate { DownloadMod(item); });
        }

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
            localMods.Find(x => x.name == item.name).isLoaded = true;

            loadInteractable.OnInteract.RemoveAllListeners();
            loadModText.text = "Loaded!";
            loadModMaterial.color = Color.red;
            modloader.loadedModsCount++;
            discord.UpdatePresence(modloader.loadedModsCount, modloader.discordDetail, modloader.discordState);
        }
        else
        {
            Debug.LogError("Source is null");
        }
    }
    public void DownloadMod(ModItem item)
    {
        //This handles the download and placing the file in the correct location
    }

    public void SwitchButton()
    {
        //This is getting disabled till the json issue gets fixed after release of 2.0.0
        return;
        onLocal = !onLocal;
        if (onLocal)
            FindLocalMods();
        else
            StartCoroutine(FindOnlineMods());
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

    
}

public class ModItem
{
    public string name { private set; get; }
    public string version { private set; get;}
    public string description { private set; get; }
    public string downloadURL { private set; get; }
    public Assembly assembly { private set; get; }
    public string path { private set; get; }
    public bool isLoaded = false;
    public bool isLocal = false;
    public ModItem(string name, string description, string downloadURL, string version, bool isLocal)
    {
        this.name = name;
        this.description = description;
        this.downloadURL = downloadURL;
        this.version = version;
        this.isLocal = isLocal;
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

[Serializable]
public class APIMod
{
    public string Name;
    public string Description;
    public string Creator;
    public string URL;
    public string Version;
}