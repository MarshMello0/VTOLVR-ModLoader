using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModLoader
{
    public class Load
    {
        public static void Init()
        {
            new GameObject("Mod Loader", typeof(ModLoader));
        }
    }

    public class ModLoader : MonoBehaviour
    {
        public static ModLoader _instance;

        private void Awake()
        {
            //This is to make sure we only have one of these in the game
            if (!_instance)
            {
                _instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Destroy(this.gameObject);
                return;
            }
        }
    }
}
