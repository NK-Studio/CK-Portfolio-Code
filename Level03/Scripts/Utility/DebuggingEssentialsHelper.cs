using System;
using DebuggingEssentials;
using Managers;
using UnityEngine;

namespace Utility
{
    [RequireComponent(typeof(WindowManager))]
    public class DebuggingEssentialsHelper : MonoBehaviour
    {
        private void Start()
        {
            var wm = GetComponent<WindowManager>();
            GameManager.Instance.DebuggingEssentials = wm;
            gameObject.SetActive(false);
        }
    }
}