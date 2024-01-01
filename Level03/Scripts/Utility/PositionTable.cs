using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Utility
{
    public class PositionTable : SerializedMonoBehaviour
    {
        public Transform Target;
        public bool IsLocalPosition = true;
        public Dictionary<string, Vector3> Table = new();

        public void SetPosition(string key)
        {
            if (!Target)
            {
                Target = transform;
            }

            if (IsLocalPosition)
            {
                Target.localPosition = Table[key];
            }
            else
            {
                Target.position = Table[key];
            }
        }
    }
}