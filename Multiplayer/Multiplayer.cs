using System;
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

    }
}
