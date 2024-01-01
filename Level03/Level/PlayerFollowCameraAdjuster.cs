using Character.Presenter;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Level
{
    public class PlayerFollowCameraAdjuster : MonoBehaviour
    {
        public Quaternion Rotation = Quaternion.identity;

        private PlayerPresenter _player;
        private void Start()
        {
            _player = GameManager.Instance.Player;
        }

        private void OnEnter()
        {
            _player.CameraRoot.rotation = Rotation * _player.CameraRoot.rotation;
        }

        private void OnExit()
        {
            _player.CameraRoot.rotation = Quaternion.Inverse(Rotation) * _player.CameraRoot.rotation;
        }
        
        [field: SerializeField, ReadOnly]
        private bool _isInArea;
        private void OnTriggerEnter(Collider other)
        {
            if(!_isInArea && other.CompareTag("Player"))
            {
                _isInArea = true;
                OnEnter();
            }    
        }

        private void OnTriggerExit(Collider other)
        {
            if(_isInArea && other.CompareTag("Player"))
            {
                _isInArea = false;
                OnExit();
            }    
        }
    }
}