using System;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Frame;
public class NetworkObjectFactory : INetworkObjectFactory
{
    public void NetworkCreateObject(NetWorker networker, int identity, uint id, FrameStream frame, Action<NetworkObject> callback)
    {
        bool flag = false;
        NetworkObject obj = null;
        if (!flag && callback != null)
        {
            callback(obj);
        }
    }
}