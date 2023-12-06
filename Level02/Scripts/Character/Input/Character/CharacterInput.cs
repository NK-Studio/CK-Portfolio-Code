using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Character.Input.Character
{
    public class CharacterInput : MonoBehaviour
    {
        [ReadOnly] public float axisHorizontal;

        [ReadOnly] public float axisVertical;

        [ReadOnly] public BoolReactiveProperty pressJump = new();

        [ReadOnly] public BoolReactiveProperty pressReadyHook = new();

        [ReadOnly] public BoolReactiveProperty pressAttack = new();

        [ReadOnly] public BoolReactiveProperty pressUseItemAttack = new();

        [ReadOnly] public BoolReactiveProperty pressThrowHook = new();

        [ReadOnly] public BoolReactiveProperty pressHookShot = new();

        [ReadOnly] public BoolReactiveProperty pressInteraction = new();

        [ReadOnly] public BoolReactiveProperty pressPullStyle = new();

        [ReadOnly] public BoolReactiveProperty RopeCancel = new();

        [ReadOnly] public BoolReactiveProperty pressMoveToTargetStyle = new();

        [ReadOnly] public FloatReactiveProperty pressChangeHook = new();

        public void MovementInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            axisHorizontal = input.ReadValue<Vector2>().x;
            axisVertical = input.ReadValue<Vector2>().y;
        }

        public void JumpInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            pressJump.Value = input.ReadValueAsButton();
        }


        public void ReadyHookInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            pressReadyHook.Value = input.ReadValueAsButton();
        }

        public void UseItemInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            pressUseItemAttack.Value = input.ReadValueAsButton();
        }

        public void ChangeHookInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            pressChangeHook.Value = input.ReadValue<float>();
        }

        public void ThrowHookInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            pressThrowHook.Value = input.ReadValueAsButton();
        }

        public void HookShotInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            pressHookShot.Value = input.ReadValueAsButton();
        }

        public void AttackInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            pressAttack.Value = input.ReadValueAsButton();
        }

        public void InteractionInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            pressInteraction.Value = input.ReadValueAsButton();
        }

        public void PullStyleInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            pressPullStyle.Value = input.ReadValueAsButton();
        }

        public void RopeCancelInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            RopeCancel.Value = input.ReadValueAsButton();
        }


        public void MoveToTargetStyleInput(InputAction.CallbackContext input)
        {
            if (Time.timeScale == 0) return;
            pressMoveToTargetStyle.Value = input.ReadValueAsButton();
        }
    }
}