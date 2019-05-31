using System;
using DarkRift.Server;

public class VTOLServerPlugin : Plugin
{
    public override bool ThreadSafe => false;

    public override Version Version => new Version(0, 0, 1);

    public VTOLServerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
    {

    }
}