using System;
using System.Collections.Generic;
using System.Text;
using DarkRift.Server;

class Player
{
    public ushort ID { get; set; }
    public IClient client { get; }
    public string vehicle { get; }
    public string name { get; }
    public Player(ushort ID, IClient client, string vehicle, string name)
    {
        this.ID = ID;
        this.client = client;
        this.vehicle = vehicle;
        this.name = name;
    }

}