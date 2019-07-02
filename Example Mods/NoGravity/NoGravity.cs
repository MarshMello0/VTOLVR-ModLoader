using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
[Info("No Gravity",
    "Adds a basic button to disable/enable gravity",
    "https://github.com/MarshMello0/VTOLVR-ModLoader/raw/dev/Example%20Mods/NoGravity/NoGravity.dll",
    "1.0")]
public class NoGravity : VTOLMOD
{
    private bool isDisabled;
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