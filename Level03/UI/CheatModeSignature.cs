using System;
using Managers;
using UnityEngine;

namespace UI
{
    public class CheatModeSignature : MonoBehaviour
    {
        public GameObject Target;

        private void Start()
        {
            if(GameManager.Instance.CheatMode)
                Target.SetActive(true);
            GameManager.Instance.CheatModeSignature = Target;
        }
    }
}