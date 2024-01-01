using Managers;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Character.Input
{
    public class CharacterInput : MonoBehaviour
    {
        [ReadOnly] public Vector2ReactiveProperty MoveAxis = new();
        [ReadOnly] public Vector2ReactiveProperty AimAxis = new();
        [ReadOnly] public BoolReactiveProperty PressAttack = new();
        [ReadOnly] public BoolReactiveProperty PressDodgeFlash = new();
        [ReadOnly] public BoolReactiveProperty MouseRightClick = new();
        [ReadOnly] public FloatReactiveProperty ScrollY = new(); // Mouse Wheel
        [ReadOnly] public BoolReactiveProperty Reload = new();  // R
        [ReadOnly] public BoolReactiveProperty Interaction = new();  // F
        
        private void Start()
        {
            var playerActions = ManagerX.AutoManager.Get<InputManager>().Controller.Player;

            playerActions.Move.performed += MoveInput;
            playerActions.Move.canceled += MoveInput;
            playerActions.Aim.performed += AimInput;
            playerActions.Aim.canceled += AimInput;

            playerActions.Attack.performed += AttackInput;
            playerActions.Attack.canceled += AttackInput;
            playerActions.DodgeFlash.performed += DodgeFlashInput;
            playerActions.HammerKey.performed += RightClickInput;
            playerActions.HammerKey.canceled += RightClickInput;
            playerActions.InterAction.performed += InterActionInput;
            playerActions.Reload.performed += ReloadInput;
        }

        public Subject<bool> UpdateTriggerPress = new(); 
        public Subject<bool> UpdateHammerPress = new(); 
        
        public bool IsTriggerPressing => InputManager.Instance.Controller.Player.Attack.IsPressed();
        public bool IsHammerPressing => InputManager.Instance.Controller.Player.HammerKey.IsPressed();
        private void Update()
        {
            UpdateTriggerPress.OnNext(IsTriggerPressing);
            UpdateHammerPress.OnNext(IsHammerPressing);
        }
        
        private void MoveInput(InputAction.CallbackContext input)
        {
            // if (Time.timeScale == 0) return;
            MoveAxis.Value = input.ReadValue<Vector2>();
        }
        
        private void AimInput(InputAction.CallbackContext input)
        {
            // if (Time.timeScale == 0) return;
            AimAxis.Value = input.ReadValue<Vector2>();
        }

        private void AttackInput(InputAction.CallbackContext input)
        {
            // if (Time.timeScale == 0) return;
            PressAttack.Value = input.ReadValueAsButton();
        }

        private void DodgeFlashInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            PressDodgeFlash.Value = input.ReadValueAsButton();
        }

        private void RightClickInput(InputAction.CallbackContext input)
        {
            // if (Time.timeScale == 0) return;
            MouseRightClick.Value = input.ReadValueAsButton();
        }
        
        private void InterActionInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            Interaction.Value = input.ReadValueAsButton();
        }
  
        private void ReloadInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            Reload.Value = input.ReadValueAsButton();
        }

        /// <summary>
        /// 플레이어 입력을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Controller.PlayerActions GetPlayerAction()
        {
            return ManagerX.AutoManager.Get<InputManager>().Controller.Player;
        }

        /// <summary>
        /// 콘트롤러 타입을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public ControllerType GetControllerType()
        {
            return ManagerX.AutoManager.Get<InputManager>().CurrentController;
        }
    }
}