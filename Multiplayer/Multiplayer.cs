using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer
{
    public class Load
    {
        public static void Init()
        {
            new GameObject("Multiplayer", typeof(Multiplayer));
        }
    }
    public class Multiplayer : MonoBehaviour
    {
        private MultiplayerMenu multiplayerMenu;

        private void Awake()
        {
            CreateItems();
        }

        private void CreateItems()
        {
            multiplayerMenu = gameObject.AddComponent<MultiplayerMenu>();
        }

        private void OnGUI()
        {
            if (!multiplayerMenu)
                return;
            //multiplayerMenu.ipAddress = GUI.TextField(new Rect(10, 10, 150, 100), "127.0.0.1");
            //multiplayerMenu.portNumber = GUI.TextField(new Rect(10, 120, 150, 100), "15937");

            if (GUI.Button(new Rect(10,10,150,100),"Host"))
            {
                multiplayerMenu.Host();
            }
        }

    }
}
