using System.Collections.Generic;
using UnityEngine;

namespace SceneSystem
{
    public class LevelCheckPointHandler : MonoBehaviour
    {
        public List<CheckPointTrigger> Triggers { get; private set; } = new();
    }
}