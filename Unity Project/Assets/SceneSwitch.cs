using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneSwitch : MonoBehaviour
{
    private void OnGUI()
    {
        if (GUI.Button(new Rect(0,0,100,100),"a"))
        {
            SceneManager.LoadScene(1);
        }
    }
}
