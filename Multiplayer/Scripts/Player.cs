using System;

public class Player
{
    ushort id { get; }
    string pilotName { get; }
    MultiplayerMod.Vehicle vehicle { get; }

    public Player (ushort id, string pilotName, MultiplayerMod.Vehicle vehicle)
    {
        this.id = id;
        this.pilotName = pilotName;
        this.vehicle = vehicle;
    }
}