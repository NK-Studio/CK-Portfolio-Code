using System;
using Character.Presenter;
using Managers;
using UnityEngine;

namespace Platform
{
    public class PlayerFollowCameraDistanceModifier : MonoBehaviour
    {
        public float NewCameraDistance = 16f;
     
        private PlayerPresenter _player;
        private void Start()
        {
            _player = GameManager.Instance.Player;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _player.Model.PlayerFollowCameraDistance = NewCameraDistance;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _player.Model.ResetPlayerFollowCameraDistance();
            }
        }
    }
}