using System;
using Doozy.Runtime.Nody;
using Doozy.Runtime.Signals;
using Doozy.Runtime.UIManager.Input;
using NKStudio;
using Managers;
using ManagerX;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    public class MenuViewController : MonoBehaviour
    {
        public FlowController MenuController;
        private InputManager _inputManager;

        public GameObject BlurOverlay;

        private void Start()
        {
            _inputManager = AutoManager.Get<InputManager>();
            _inputManager.Controller.System.Back.performed += BackEvent;

            Messager.RegisterMessage("ShowBlur", () => BlurOverlay.SetActive(true));
            Messager.RegisterMessage("HideBlur", () => BlurOverlay.SetActive(false));
        }

        /// <summary>
        /// ESC키 누르면
        /// </summary>
        /// <param name="input"></param>
        private void BackEvent(InputAction.CallbackContext input)
        {
            if (MenuController)
                if (GameManager.Instance.CanActiveMenu && MenuController.flow.activeNode.nodeName.Equals("InGameView"))
                {
                    GameManager.Instance.IsActiveMenu = true;
                    SignalsService.SendSignal("InGame", "Menu");
                }
                else if (MenuController.flow.activeNode.nodeName.Equals("MenuView"))
                {
                    MenuController.SetActiveNodeByName("Exit");
                }
        }

        /// <summary>
        /// Esc를 누른 것처럼 Back을 합니다.
        /// </summary>
        public void Back()
        {
            BackButton.Fire();
        }

        private void OnDestroy()
        {
            Messager.RemoveAllMessages();
        }
    }
}
