using System;
using System.IO;
using UnityEngine;
using Steamworks;
public enum VTOLVehicles { None, AV42C, FA26B, F45A }
public class VTOLMOD : MonoBehaviour
{
    private GameObject playerVehicleGO;
    private VTOLVehicles playerVehicleEnum = VTOLVehicles.AV42C;
    private Campaign currentCampaign;
    private CampaignScenario currentScenario;
    private string gamePath;
    private string modsPath = @"\VTOLVR_Modloader\mods";
    private string modDataFolder;

    private void Awake()
    {        
        gamePath = Directory.GetCurrentDirectory();
        modDataFolder = @"\" + gameObject.name;
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
        return playerVehicleGO;
    }
    /// <summary>
    /// Returns which vehicle the player is using in a Enum.
    /// </summary>
    /// <returns></returns>
    public VTOLVehicles GetPlayersVehicleEnum()
    {
        return playerVehicleEnum;
    }
    /// <summary>
    /// Returns the campaign which the player is currently playing, this will be null if there is none
    /// </summary>
    /// <returns></returns>
    public Campaign GetCurrentCampaign()
    {
        return currentCampaign;
    }
    /// <summary>
    /// Returns the Scenario which the player is currently player, this will be null if there is none
    /// </summary>
    /// <returns></returns>
    public CampaignScenario GetCurrentScenario()
    {
        return currentScenario;
    }
    /// <summary>
    /// Saves a file to your mods mod data folder, please use this instead of creating your own location.
    /// </summary>
    /// <param name="path">This is the path including the file name and extention where the file will be saved in your mod data folder. EG "\folder\file.txt"</param>
    /// <param name="contents">The contents of the file</param>
    /// <returns></returns>
    public bool SaveFile(string path, string contents)
    {
        Directory.CreateDirectory(gamePath + modsPath + modDataFolder);
        if (!path.StartsWith(@"\") || !path.StartsWith(@"/"))
        {
            string oldPath = path;
            path = @"\" + oldPath;
        }

        try
        {
            File.WriteAllText(gamePath + modsPath + modDataFolder + path, contents);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(gameObject.name + ": Failed to save file \"" + path + "\"\n" + e.Message + "\n" + e.StackTrace);
            return false;
        }
        
    }
    /// <summary>
    /// Returns a bool if reading the files worked and out puts a array of lines for each line in the file, please use this instead of your own method.
    /// </summary>
    /// <param name="path">This is the path including the file name and extention where the file will be in your mod data folder. EG "\folder\file.txt"</param>
    /// <param name="lines">The array which the contents of the file will be stored in each line</param>
    /// <returns></returns>
    public bool GetFile(string path, out string[] lines)
    {
        Directory.CreateDirectory(gamePath + modsPath + modDataFolder);
        if (!path.StartsWith(@"\") || !path.StartsWith(@"/"))
        {
            string oldPath = path;
            path = @"\" + oldPath;
        }

        lines = new string[0];
        try
        {
            lines = File.ReadAllLines(gamePath + modsPath + modDataFolder + path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(gameObject.name + ": Failed to read file \"" + path + "\"\n" + e.Message + "\n" + e.StackTrace);
            return false;
        }
    }

    public bool GetCurrentPilotName(out string name)
    {
        name = "";
        try
        {
            name = PilotSaveManager.current.pilotName;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public void Log(object message)
    {
        Debug.Log("Mod Loader: " + message);
    }
    public void LogWarning(object message)
    {
        Debug.LogWarning("Mod Loader: " + message);
    }
    public void LogError(object message)
    {
        Debug.LogError("Mod Loader: " + message);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class Info : Attribute
    {
        public string name { private set; get; }
        public string version { private set; get; }
        public string description { private set; get; }
        public string downloadURL { private set; get; }

        public Info(string name, string description, string downloadURL, string version)
        {
            this.name = name;
            this.description = description;
            this.downloadURL = downloadURL;
            this.version = version;
        }
    }
}
