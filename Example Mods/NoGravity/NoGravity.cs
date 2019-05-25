using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

namespace NoGravity
{
    public class Load
    {
        public static void Init()
        {
            new GameObject("No Gravity", typeof(NoGravity));
        }
    }

    public class NoGravity : MonoBehaviour
    {
        private bool isDisabled;

        public static void Init()
        {
            new GameObject("No Gravity", typeof(NoGravity));
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
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

}
