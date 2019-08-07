using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using ModLoader;

public enum VTOLVehicles { None, AV42C, FA26B, F45A }
public class VTOLAPI : MonoBehaviour
{
    public static VTOLAPI instance { get; private set; }
    private string gamePath;
    private string modsPath = @"\VTOLVR_ModLoader\mods";

    private void Awake()
    {
        if (instance)
            Destroy(this.gameObject);
        DontDestroyOnLoad(this.gameObject);
        instance = this;
        gamePath = Directory.GetCurrentDirectory();
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
    public VTOLVehicles GetPlayersVehicleEnum()
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
    /// Saves a file to your mods mod data folder, please use this instead of creating your own location.
    /// </summary>
    /// <param name="path">This is the path including the file name and extention where the file will be saved in your mod data folder. EG "\folder\file.txt"</param>
    /// <param name="contents">The contents of the file</param>
    /// <returns></returns>
    public bool SaveFile(Type mod, string path, string contents)
    {
        string modDataFolder = @"\" + mod.GetModName();
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
    public bool GetFile(Type mod, string path, out FileInfo file)
    {
        string modDataFolder = @"\" + mod.GetModName();
        Directory.CreateDirectory(gamePath + modsPath + modDataFolder);
        if (!path.StartsWith(@"\") || !path.StartsWith(@"/"))
        {
            string oldPath = path;
            path = @"\" + oldPath;
        }

        file = null;
        try
        {
            file = new FileInfo(gamePath + modsPath + modDataFolder + path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(mod.GetModName() + ": Failed to read file \"" + path + "\"\n" + e.Message + "\n" + e.StackTrace);
            return false;
        }
    }

    public bool CreateFolder(Type mod, string path)
    {
        string modDataFolder = @"\" + mod.GetModName();

        if (!path.StartsWith(@"\") || !path.StartsWith(@"/"))
        {
            string oldPath = path;
            path = @"\" + oldPath;
        }
        try
        {
            Directory.CreateDirectory(gamePath + modsPath + modDataFolder + path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(mod.GetModName() + ": Failed to create folder " + path + "\n" + e.Message + "\n" + e.StackTrace);
            return false;
        }
        
    }
    public bool GetFolder(Type mod, string path, out DirectoryInfo folder)
    {
        try
        {
            string modDataFolder = @"\" + mod.GetModName();

            if (!path.StartsWith(@"\") || !path.StartsWith(@"/"))
            {
                string oldPath = path;
                path = @"\" + oldPath;
            }
            path += @"\";
            folder = Directory.GetParent(gamePath + modsPath + modDataFolder + path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(mod.GetModName() + ": Failed to find folder " + path + "\n" + e.Message + "\n" + e.StackTrace);
            folder = null;
            return false;
        }
    }    
}

