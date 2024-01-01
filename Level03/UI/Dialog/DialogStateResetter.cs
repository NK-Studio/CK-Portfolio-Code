using System;
using Managers;
using UnityEngine;

namespace Tutorial.Helper
{
    // Resetter는 뭐고 ㅋㅋ
    public class DialogStateResetter : MonoBehaviour
    {
        public bool ResetOnStart = true;

        private void Start()
        {
            if (ResetOnStart)
            {
                ResetEvents();
            }
        }

        public void ResetEvents()
        {
            DialogManager.Instance.ResetEvents();
        }
    }
}