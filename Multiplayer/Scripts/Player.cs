using System;
using UnityEngine;

public class Player
{
    public ushort id { get; }
    public string pilotName { get; }
    public MultiplayerMod.Vehicle vehicle { get; }

    private float positionX, positionY, positionZ, rotationX, rotationY, rotationZ;
    public float speed;
    public bool landingGear;
    public float flaps;
    public float thrusterAngle = -1;
    public Player (ushort id, string pilotName, MultiplayerMod.Vehicle vehicle)
    {
        this.id = id;
        this.pilotName = pilotName;
        this.vehicle = vehicle;
    }

    public void SetPosition(float x, float y, float z)
    {
        positionX = x;
        positionY = y;
        positionZ = z;
    }

    public void SetRotation(float x, float y, float z)
    {
        rotationX = x;
        rotationY = y;
        rotationZ = z;
    }

    public Vector3 GetPosition()
    {
        return new Vector3(positionX, positionY, positionZ);
    }

    public Quaternion GetRotation()
    {
        return Quaternion.Euler(rotationX, rotationY, rotationZ);
    }
}