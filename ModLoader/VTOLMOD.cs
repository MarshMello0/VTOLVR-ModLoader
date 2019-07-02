using System;
using UnityEngine;
public class VTOLMOD : MonoBehaviour
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Info : Attribute
    {
        public string name { private set; get; }
        public string version { private set; get; }
        public string description { private set; get; }
        public string downloadURL { private set; get; }

        public Info(string name, string description, string downloadURL, string version)
        {
            this.name = name;
            this.description = description;
            this.downloadURL = downloadURL;
            this.version = version;
        }
    }
}
