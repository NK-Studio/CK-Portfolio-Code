using System;
using System.Collections.Generic;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    public class CanvasToggler : MonoBehaviour
    {
        public Key Key = Key.Home;
        public List<Canvas> Target = new();
        [ReadOnly]
        public bool Enabled = true;

        private void Update()
        {
            if(!GameManager.Instance.CheatMode) return;
            if (Keyboard.current[Key].wasPressedThisFrame)
            {
                Enabled = !Enabled;
                foreach (var c in Target)
                {
                    c.enabled = Enabled;
                }
            }
        }
    }
}