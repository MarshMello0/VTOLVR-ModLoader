using System;
using UnityEngine;
using ModLoader;
public class VTOLMOD : MonoBehaviour
{
    public Mod thisMod { private set; get; } = null;
    public virtual void ModLoaded()
    {
        Log("Loaded!");
    }
    public void Log(object message)
    {
        if (thisMod == null)
            Debug.Log(gameObject.name + ": " + message);
        else
            Debug.Log(thisMod.name + ": " + message);
    }
    public void LogWarning(object message)
    {
        if (thisMod == null)
            Debug.LogWarning(gameObject.name + ": " + message);
        else
            Debug.LogWarning(thisMod.name + ": " + message);
    }
    public void LogError(object message)
    {
        if (thisMod == null)
            Debug.LogError(gameObject.name + ": " + message);
        else
            Debug.LogError(thisMod.name + ": " + message);
    }

    public void SetModInfo(Mod thisMod)
    {
        if (this.thisMod == null)
            this.thisMod = thisMod;
    }
}
