using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using ModLoader;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Collections;
/// <summary>
/// Enum of the different vehicles in the game.
/// </summary>
public enum VTOLVehicles { None, AV42C, FA26B, F45A }
/// <summary>
/// All the different scenes in order.
/// </summary>
public enum VTOLScenes { SplashScene, SamplerScene, ReadyRoom, VehicleConfiguration, LoadingScene, MeshTerrain, OpenWater, Akutan, VTEditMenu, VTEditLoadingScene, VTMapEditMenu, CustomMapBase, CommRadioTest, ShaderVariantsScene };
public class VTOLAPI : MonoBehaviour
{
    public static VTOLAPI instance { get; private set; }
    private string gamePath;
    private string modsPath = @"\VTOLVR_ModLoader\mods";
    private Dictionary<string, Action<string>> commands = new Dictionary<string, Action<string>>();

    /// <summary>
    /// This gets invoked when the scene has changed and finished loading. 
    /// This should be the safest way to start running code when a level is loaded.
    /// </summary>
    public static UnityAction<VTOLScenes> SceneLoaded;
    /// <summary>
    /// The current scene which is active.
    /// </summary>
    public static VTOLScenes currentScene { get; private set; }

    private void Awake()
    {
        if (instance)
            Destroy(this.gameObject);
        DontDestroyOnLoad(this.gameObject);
        instance = this;
        gamePath = Directory.GetCurrentDirectory();
        SceneManager.activeSceneChanged += ActiveSceneChanged;
    }

    private void ActiveSceneChanged(Scene current, Scene next)
    {
        switch (next.buildIndex)
        {
            case 0:
                CallSceneLoaded(VTOLScenes.SplashScene);
                break;
            case 1:
                CallSceneLoaded(VTOLScenes.SamplerScene);
                break;
            case 2:
                CallSceneLoaded(VTOLScenes.ReadyRoom);
                break;
            case 3:
                CallSceneLoaded(VTOLScenes.VehicleConfiguration);
                break;
            case 4:
                CallSceneLoaded(VTOLScenes.LoadingScene);
                break;
            case 5:
                CallSceneLoaded(VTOLScenes.MeshTerrain);
                break;
            case 6:
                CallSceneLoaded(VTOLScenes.OpenWater);
                break;
            case 7:
                StartCoroutine(WaitForScenario(VTOLScenes.Akutan));
                break;
            case 8:
                CallSceneLoaded(VTOLScenes.VTEditMenu);
                break;
            case 9:
                CallSceneLoaded(VTOLScenes.VTEditLoadingScene);
                break;
            case 10:
                CallSceneLoaded(VTOLScenes.VTMapEditMenu);
                break;
            case 11:
                StartCoroutine(WaitForScenario(VTOLScenes.CustomMapBase));
                break;
            case 12:
                CallSceneLoaded(VTOLScenes.CommRadioTest);
                break;
            case 13:
                CallSceneLoaded(VTOLScenes.ShaderVariantsScene);
                break;
        }
    }
    private IEnumerator WaitForScenario(VTOLScenes Scene)
    {
        while (VTMapManager.fetch == null || !VTMapManager.fetch.scenarioReady)
        {
            yield return null;
        }
        CallSceneLoaded(Scene);
    }
    private void CallSceneLoaded(VTOLScenes Scene)
    {
        currentScene = Scene;
        if (SceneLoaded != null)
            SceneLoaded.Invoke(Scene);
    }

    /// <summary>
    /// Returns the steam ID of the player which is using this mod.
    /// </summary>
    /// <returns></returns>
    public ulong GetSteamID()
    {
        return SteamUser.GetSteamID().m_SteamID;
    }
    /// <summary>
    /// Returns the current name of the steam user, if they change their name during play session, this doesn't update.
    /// </summary>
    /// <returns></returns>
    public string GetSteamName()
    {
        return SteamFriends.GetPersonaName();
    }
    /// <summary>
    /// Returns the parent gameobject of what vehicle the player is currently flying, it will return null if nothing is found.
    /// </summary>
    /// <returns></returns>
    public GameObject GetPlayersVehicleGameObject()
    {
        VTOLVehicles currentVehicle = GetPlayersVehicleEnum();

        switch (currentVehicle)
        {
            case VTOLVehicles.AV42C:
                return GameObject.Find("VTOL4(Clone)");
            case VTOLVehicles.F45A:
                return GameObject.Find("SEVTF(Clone)");
            case VTOLVehicles.FA26B:
                return GameObject.Find("FA-26B(Clone)");
            default: //It should be none here
                return null;
        }
    }
    /// <summary>
    /// Returns which vehicle the player is using in a Enum.
    /// </summary>
    /// <returns></returns>
    public static VTOLVehicles GetPlayersVehicleEnum()
    {
        if (PilotSaveManager.currentVehicle == null)
            return VTOLVehicles.None;

        string vehicleName = PilotSaveManager.currentVehicle.vehicleName;
        switch (vehicleName)
        {
            case "AV-42C":
                return VTOLVehicles.AV42C;
            case "F/A-26B":
                return VTOLVehicles.FA26B;
            case "F-45A":
                return VTOLVehicles.F45A;
            default:
                return VTOLVehicles.None;
        }
    }
    /// <summary>
    /// Creates a settings page in the `mod settings` tab.
    /// Make sure to fully create your settings before calling this as you 
    /// can't change it onces it's created.
    /// </summary>
    /// <param name="newSettings"></param>
    public static void CreateSettingsMenu(Settings newSettings)
    {
        ModLoader.ModLoader.instance.CreateSettingsMenu(newSettings);
    }

    public void CheckConsoleCommand(string command)
    {
        if (commands.ContainsKey(command.ToLower()))
        {
            commands[command.ToLower()].Invoke(command.ToLower());
        }
    }

    public void CreateCommand(string command, Action<string> callBack)
    {
        commands.Add(command.ToLower(), callBack);
        Debug.Log("ML API: Created command: " + command.ToLower());
    }

    public void RemoveCommand(string command)
    {
        commands.Remove(command.ToLower());
        Debug.LogWarning("ML API: Removed command: " + command.ToLower());
    }

    public void ShowHelp(string input)
    {
        StringBuilder stringBuilder = new StringBuilder("ML API Console Commands\n");
        List<string> list = commands.Keys.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            stringBuilder.AppendLine("Command: " + list[i]);
        }
        Debug.Log(stringBuilder.ToString());
    }
}

