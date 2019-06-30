using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
[Info("No Gravity", "Adds a basic button to disable/enable gravity","", "1.0")]
public class NoGravity : VTOLMOD
{
    public static NoGravity _instance;
    private bool isDisabled;

    private void Awake()
    {
        if (!_instance)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            Debug.Log("No Gravity Mod Loaded");
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
    }
    private void OnGUI()
    {
        if (isDisabled)
        {
            if (GUI.Button(new Rect(10, 10, 150, 100), "Enable Gravity"))
            {
                Physics.gravity = new Vector3(0, -9.3f, 0);
                Debug.Log("Gravity has been enabled");
                isDisabled = false;
            }
        }
        else
        {
            if (GUI.Button(new Rect(10, 10, 150, 100), "Disable Gravity"))
            {
                Physics.gravity = new Vector3(0, 0, 0);
                Debug.Log("Gravity has been disabled");
                isDisabled = true;
            }
        }

    }
}