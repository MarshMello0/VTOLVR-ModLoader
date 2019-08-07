using System;
using UnityEngine;
using ModLoader;
public class VTOLMOD : MonoBehaviour
{
    private string modName;

    private void Awake()
    {
        modName = GetType().GetModName();
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

    [AttributeUsage(AttributeTargets.Class)]
    public class Info : Attribute
    {
        public string name { private set; get; }
        public string version { private set; get; }
        public string description { private set; get; }

        public Info(string name, string description, string version)
        {
            this.name = name;
            this.description = description;
            this.version = version;
        }
    }
}
