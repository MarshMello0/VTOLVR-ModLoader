using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
