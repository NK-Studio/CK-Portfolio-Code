using System;
using System.Collections.Generic;
using EnumData;
using ManagerX;
using Sirenix.OdinInspector;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;


public enum ControllerType
{
    KeyboardMouse,
    Gamepad,
    KeyboardWASD,
}

namespace Managers
{
    [ManagerDefaultPrefab("InputManager")]
    public class InputManager : SerializedMonoBehaviour, AutoManager
    {
        public static InputManager Instance => AutoManager.Get<InputManager>();

        [field: SerializeField]
        public KeyboardMoveType KeyboardMoveType { get; private set; } = KeyboardMoveType.Keyboard;

        /// <summary>
        /// 컨트롤러를 반환합니다. [Read Only]
        /// </summary>
        public Controller Controller { get; private set; }

        public UnityEvent OnAnyButtonPressed;

        private ReactiveProperty<ControllerType> _currentController = new(ControllerType.KeyboardWASD);
        /// <summary>
        /// 현재 컨트롤러 타입을 반환합니다. [Read Only]
        /// </summary>
        public ControllerType CurrentController { 
            get => _currentController.Value;
            private set => _currentController.Value = value;
        }

        public IObservable<ControllerType> CurrentControllerObservable => _currentController.AsObservable();

        public struct KeychronSettings
        {
            public VariablesGroupAsset Keychron;
            public TMP_SpriteAsset SpriteAsset;
        }
        [field: SerializeField]
        public Dictionary<ControllerType, KeychronSettings> KeychronByControllerType { get; private set; } = new();

        public KeychronSettings CurrentKeychronSettings => KeychronByControllerType[CurrentController];
        
        private PlayerInput _playerInput;

        [SerializeField] private bool showDebug; 
        
        private void Awake()
        {
            Controller = new Controller();
            _playerInput = GetComponent<PlayerInput>();
            
        }

        private void OnEnable()
        {
            Controller.Enable();
        }

        private void Start()
        {
            CurrentControllerObservable.Subscribe((type) =>
            {
                if (showDebug) 
                    Debug.Log($"Controller Changed: {type.ToString()}");
                
                const string keychron = "keychron";
                var extension = LocalizationSettings.StringDatabase.SmartFormatter.GetSourceExtension<PersistentVariablesSource>();
                extension.Remove(keychron);
                extension[keychron] = CurrentKeychronSettings.Keychron;
            });
            _playerInput.onControlsChanged += SwitchControls;
            SwitchControls(_playerInput);

            InputSystem.onAnyButtonPress.Call(_ => OnAnyButtonPressed?.Invoke());
        }

        private void SwitchControls(PlayerInput obj)
        {
            var currentControlScheme = obj.currentControlScheme;

            switch (currentControlScheme)
            {
                case "Gamepad":
                    CurrentController = ControllerType.Gamepad;
                    return;
                case "KeyboardMouse":
                    CurrentController = ControllerType.KeyboardWASD;
                    return;
                // case "KeyboardWASD": CurrentController = ControllerType.KeyboardWASD; return;
            }
        }

        private void Update()
        {
            if (Keyboard.current.oKey.wasPressedThisFrame)
            {
                var json = Controller.SaveBindingOverridesAsJson();
                Debug.Log(json);
            }
        }

        private void OnDisable()
        {
            Controller.Disable();
        }
    }
}