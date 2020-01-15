using System;

[Serializable]
public class SettingsSave
{
    public bool devConsole { get; set; }
    public Pilot previousPilot { get; set; }
    public Scenario previousScenario { get; set; }
    public string[] previousModsLoaded { get; set; }
}
[Serializable]
public class Pilot
{
    public string Name { get; set; }
    public Pilot(string name)
    {
        Name = name;
    }

    public Pilot()
    {
    }
}
[Serializable]
public class Scenario
{
    public string Name { get; set; }
    public string ID;
    public string cID;
    public Scenario(string name, string cID, string iD)
    {
        Name = name;
        ID = iD;
        this.cID = cID;
    }

    public Scenario()
    {
    }
}