using FMODUnity;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Utility
{
    public class FastForwardTimeline : MonoBehaviour
    {
        [Header("Timeline Setting")]
        [SerializeField]
        private PlayableDirector _player;

        [field: Header("FastForward")]
        [field: SerializeField]
        public bool CanFastForward { get; set; } = false;
        [SerializeField] 
        public float FastForwardMultiplier = 2f;

        public StudioEventEmitter Emitter;
        public string MusicSkipParameter = "IsSkipping";
        
        private bool IsAnyKeyPressed()
        {
            return InputManager.Instance.Controller.System.Skip.IsPressed();
            // return Keyboard.current.anyKey.isPressed;
        }

        private void Start()
        {
            if (!_player)
            {
                DebugX.LogWarning("Playable Director가 없습니다!", gameObject);
                return;
            }
        }


        private void Update()
        {
            if (!CanFastForward)
            {
                ResetFastForward();
                return;
            }

            if (IsAnyKeyPressed())
            {
                SetFastForward();
            }
            else
            {
                ResetFastForward();
            }
        }

        private void SetFastForward()
        {
            Time.timeScale = FastForwardMultiplier;
            if (Emitter)
            {
                Emitter.SetParameter(MusicSkipParameter, 1f);
            }
        }

        private void ResetFastForward()
        {
            Time.timeScale = 1f;
            if (Emitter)
            {
                Emitter.SetParameter(MusicSkipParameter, 0f);
            }
        }
    }
}