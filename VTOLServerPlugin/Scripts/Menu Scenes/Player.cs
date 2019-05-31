using System;
using System.Collections.Generic;
using System.Text;

class Player
{
    public ushort ID { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Player(ushort ID, float X, float Y, float Z)
    {
        this.ID = ID;
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }

}