using System;
using Managers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Utility
{
    public class SkippableTimelineHandler : MonoBehaviour
    {
        [Header("Timeline Setting")]
        [SerializeField]
        private PlayableDirector _player;

        [Header("Skip")] 
        [SerializeField] private Image _skipUI;
        [SerializeField] private CanvasGroup _skipCanvasGroup;
        [SerializeField] private float _skipAlphaTime = 0.5f;
        [SerializeField] private float _skipPressTime = 3f;
        [SerializeField] private UnityEvent _onSkip;

        private bool _skipped;
        private float _skipAmount;
        private float _skipAmountSpeed;
        private float _skipAlphaSpeed;


        private bool IsAnyKeyPressed()
        {
            return InputManager.Instance.Controller.System.Skip.IsPressed();
            // return Keyboard.current.anyKey.isPressed;
        }

        private void Start()
        {
            _skipped = false;
            _skipAmount = 0f;
            _skipAmountSpeed = 1f / _skipPressTime;
            _skipAlphaSpeed = 1f / _skipAlphaTime;

            if (!_player)
            {
                DebugX.LogWarning("Playable Director가 없습니다!", gameObject);
                return;
            }
        }


        private void Update()
        {
            if (_skipped || _player.state != PlayState.Playing)
            {
                return;
            }

            var time = _player.time;
            var duration = _player.duration;
            var leftDuration = duration - time;

            // 남은 시간 충분하고, 키 누르면 증가
            if (leftDuration >= _skipAlphaTime && IsAnyKeyPressed())
            {
                _skipAmount += _skipAmountSpeed * Time.deltaTime;
                if (_skipAmount >= 1f)
                {
                    _skipped = true;
                    _onSkip.Invoke();
                    return;
                }
                UpdateAlpha(_skipAlphaSpeed);
            }
            // 안 누르면 스킵 시간 초기화
            else
            {
                _skipAmount = 0f;
                UpdateAlpha(-_skipAlphaSpeed);
            }

            if (_skipUI)
                _skipUI.fillAmount = _skipAmount;

            return;
            
            void UpdateAlpha(float speed) 
                => _skipCanvasGroup.alpha = Mathf.Clamp01(_skipCanvasGroup.alpha + speed * Time.deltaTime);
        }
    }
}