using System;
using Cysharp.Threading.Tasks;
using ManagerX;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Haptics;
using Utility;

namespace Managers
{
    [ManagerDefaultPrefab("GamePadManager")]
    public class GamePadManager : MonoBehaviour, AutoManager
    {
        public static GamePadManager Instance => AutoManager.Get<GamePadManager>();
        
        private Gamepad _gamepad;

        [Serializable, InlineProperty]
        public struct RumbleSettings
        {
            [LabelWidth(50f), Tooltip("저주파: 강한 충격 표현 시 사용")]
            public AnimationCurve Low;
            [LabelWidth(50f), Tooltip("고주파: 작은 액션 / 상황 변화 표현 시 사용")]
            public AnimationCurve High;

            public RumbleSettings(float lowFrequency, float highFrequency, float duration)
            {
                Low = AnimationCurve.Constant(0f, duration, lowFrequency);
                High = AnimationCurve.Constant(0f, duration, highFrequency);
            }

            public void Pulse() => GamePadManager.Instance.RumblePulse(this);
        }

        /// <summary>
        /// 게임 패드에 진동을 일으킵니다.
        /// </summary>
        /// <param name="lowFrequency">강한 충격을 표현하기 위해 사용</param>
        /// <param name="highFrequency">작은 액션이나 게임 내부의 상황 변화를 표현하기 위해 사용</param>
        /// <param name="duration">지속 시간</param>
        public void RumblePulse(float lowFrequency, float highFrequency, float duration) =>
            RumblePulse(new RumbleSettings(lowFrequency, highFrequency, duration));
        public void RumblePulse(in RumbleSettings rumble)
        {
            bool isGamePad = InputManager.Instance.CurrentController == ControllerType.Gamepad;
            if (!isGamePad)
            {
                return;
            }
            
            _gamepad = Gamepad.current;
            if (_gamepad == null)
            {
                return;
            }

            if (!DataManager.Instance.IsEnableVibration)
            {
                return;
            }

            RumbleSequence(_gamepad, rumble).Forget();

            //시작 럼블
            // _gamepad.SetMotorSpeeds(rumble.Low, rumble.High);

            //주어진 시간 후 럼블 중지
            // StopRumble(rumble.Duration, _gamepad).Forget();
        }
        private async UniTaskVoid RumbleSequence(IDualMotorRumble gamepad, RumbleSettings settings)
        {
            float maxLength = Mathf.Max(settings.Low.GetLength(), settings.High.GetLength());
            float t = 0f;
            while (t < maxLength)
            {
                gamepad.SetMotorSpeeds(settings.Low.Evaluate(t), settings.High.Evaluate(t));
                await UniTask.Yield();
                t += Time.deltaTime;
            }
            gamepad.SetMotorSpeeds(0, 0);
        }

        private async UniTaskVoid StopRumble(float duration, IDualMotorRumble gamepad)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration));
            gamepad.SetMotorSpeeds(0, 0);
        }
    }
}