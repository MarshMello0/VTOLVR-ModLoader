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
    private Dictionary<string, Action<string>> commands = new Dictionary<string, Action<string>>();

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

