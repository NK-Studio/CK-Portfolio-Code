using System;
using AutoManager;
using Managers;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input.Camera
{
    public class CameraMouseInput : MonoBehaviour
    {
        [Header("입력 옵션 반전")] public bool invertHorizontalInput;
        public bool invertVerticalInput;

        // 마우스 감도에 곱해지는 상수
        private const float MouseInputMultiplier = 0.002f;
        // 설정으로 바꾸는 [0, 1] 구간의 감도값
        private float _normalizedMouseSensitivity;

        private float _currentMouseHorizontalAxis;
        private float _currentMouseVerticalAxis;

        private GameManager GameManager => Manager.Get<GameManager>();
        private void Start() {
            var gameManager = GameManager;
            // 게임매니저에서 감도 읽어옴
            _normalizedMouseSensitivity = gameManager.NormalizedMouseSensitivity.Value;
            // 어딘가에서 감도 바꾸면 여기에도 반영
            gameManager.NormalizedMouseSensitivity.Subscribe(value => {
                _normalizedMouseSensitivity = value;
            }).AddTo(this);
        }

        public void MouseAxisCallBack(InputAction.CallbackContext callbackContext)
        {
            Vector2 mouseInput = callbackContext.ReadValue<Vector2>();

            _currentMouseHorizontalAxis = mouseInput.x;
            _currentMouseVerticalAxis = mouseInput.y;
        }

        public float GetHorizontalCameraInput()
        {
            float input = _currentMouseHorizontalAxis;
            
            if (Time.timeScale > 0f && Time.deltaTime > 0f)
            {
                input /= Time.deltaTime;
                input *= Time.timeScale;
            }
            else
                input = 0f;

            //마우스 감도 적용;
            input *= MouseInputMultiplier * _normalizedMouseSensitivity;

            //입력 반전;
            if (invertHorizontalInput)
                input *= -1f;

            return input;
        }

        public float GetVerticalCameraInput()
        {
            //원시 마우스 입력 받기;
            float input = -_currentMouseVerticalAxis;
            
            if (Time.timeScale > 0f && Time.deltaTime > 0f)
            {
                input /= Time.deltaTime;
                input *= Time.timeScale;
            }
            else
                input = 0f;

            //마우스 감도 적용
            input *= MouseInputMultiplier * _normalizedMouseSensitivity;

            //입력 반전
            if (invertVerticalInput)
                input *= -1f;

            return input;
        }
    }
}