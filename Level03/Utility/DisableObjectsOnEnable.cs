using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    public class DisableObjectsOnEnable : RunOnEnable
    {
        public List<GameObject> Objects = new();

        public override void Execute()
        {
            foreach (var o in Objects)
            {
                o.SetActive(false);
            }
        }
    }
}