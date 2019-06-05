using System;

public class Player
{
    public ushort id { get; }
    public string pilotName { get; }
    public MultiplayerMod.Vehicle vehicle { get; }

    public Player (ushort id, string pilotName, MultiplayerMod.Vehicle vehicle)
    {
        this.id = id;
        this.pilotName = pilotName;
        this.vehicle = vehicle;
    }
}