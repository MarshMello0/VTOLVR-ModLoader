using System;
using UnityEngine;
using ModLoader;
public class VTOLMOD : MonoBehaviour
{
    private string modName;

    private void Awake()
    {
        modName = gameObject.name;
    }
    public void Log(object message)
    {
        Debug.Log(modName + ": " + message);
    }
    public void LogWarning(object message)
    {
        Debug.LogWarning(modName + ": " + message);
    }
    public void LogError(object message)
    {
        Debug.LogError(modName + ": " + message);
    }
}
