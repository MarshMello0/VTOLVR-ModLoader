using System;

[Serializable]
public class SettingsSave
{
    public bool devConsole { get; set; }
    public string previousPilot { get; set; }
    public string previousScenario { get; set; }
    public string[] previousModsLoaded { get; set; }
}