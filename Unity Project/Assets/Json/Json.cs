using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Json : MonoBehaviour
{
    // Start is called before the first frame update
    public APIMod[] mods;
    private string apiURL = "http://vtolapi.kevinjoosten.nl/availableMods";
    private APIMod[] onlineMods = null;
    void Start()
    {
        StartCoroutine(FindOnlineMods());
        return;
        
    }
    private IEnumerator FindOnlineMods()
    {
        //Featch the information and fill it into the list
        using (UnityWebRequest request = UnityWebRequest.Get(apiURL))
        {
            yield return request.SendWebRequest();

            string returnedJson = "{\"Items\":" + request.downloadHandler.text + "}";

            Debug.Log(returnedJson);
            APIMod[] apimods = JsonHelper.FromJson<APIMod>(returnedJson);

            if (apimods == null)
                Debug.LogError("API is Null");
        }
    }
}

[Serializable]
public class APIMod
{
    public string Name;
    public string Description;
    public string Creator;
    public string URL;
    public string Version;
}

public static class JsonHelper
{

    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = UnityEngine.JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return UnityEngine.JsonUtility.ToJson(wrapper);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}