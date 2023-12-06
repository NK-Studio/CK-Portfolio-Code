using System;
using AutoManager;
using GameplayIngredients;
using Settings;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utility;

namespace Managers
{
    [ManagerDefaultPrefab("GameManager")]
    public class GameManager : Manager
    {
        [ValidateInput("@this.characterSettings != null", "CharacterSettings이 비어있습니다.")]
        public CharacterSettings characterSettings;

        public InputAction FullHP;
        public InputAction PlayerStateInit;

        public InputAction MoveLoading01;
        public InputAction MoveLoading02;
        public InputAction MoveLoading03;

        public bool IsCredit;

        public bool IsNotAttack;
        
        #region HP

        private ReactiveProperty<int> _HP = new();

        public int HP
        {
            get => _HP.Value;
            set
            {
                if (value < 0)
                    value = 0;

                _HP.Value = value;
            }
        }

        private IObservable<int> _HPObservable;
        public IObservable<int> HPObservable => _HPObservable ??= _HP.AsObservable();

        #endregion

        #region Dalgona

        private ReactiveProperty<int> _Dalgona = new();

        public int Dalgana
        {
            get => _Dalgona.Value;
            set => _Dalgona.Value = value;
        }

        private IObservable<int> _DalgonaObservable;
        public IObservable<int> DalganaObservable => _DalgonaObservable ??= _Dalgona.AsObservable();

        #endregion

        public bool IsPlayPuzzle { get; set; }

        public NextSceneInfo NextSceneInfo;

        [Title("마우스 커서")] 
        public Texture2D NormalCursor;
        public Texture2D PressCursor;
        public Vector2 NormalizedCursorOffset = Vector2.zero;

        private Vector2 CursorOffset => new Vector2(
            NormalizedCursorOffset.x * NormalCursor.width,
            NormalizedCursorOffset.y * NormalCursor.height
        );
        
        [Title("마우스 감도")] 
        public FloatReactiveProperty NormalizedMouseSensitivity = new(0.5f);

        private void Awake()
        {
            HP = CharacterSettings.HpMax;
            Dalgana = 0;
            Cursor.SetCursor(NormalCursor, CursorOffset, CursorMode.Auto);

            FullHP.performed += OnTriggerFullHP;
            PlayerStateInit.performed += OnTriggerInitPlayerState;

            MoveLoading01.performed += MoveLoading01Onperformed;
            MoveLoading02.performed += MoveLoading02Onperformed;
            MoveLoading03.performed += MoveLoading03Onperformed;
        }
        
        private void MoveLoading03Onperformed(InputAction.CallbackContext obj)
        {
            Get<GameManager>().NextSceneInfo.NextScene = "Stage_3";
            SceneManager.LoadScene("Loading");
        }

        private void MoveLoading02Onperformed(InputAction.CallbackContext obj)
        {
            Get<GameManager>().NextSceneInfo.NextScene = "Stage_2";
            SceneManager.LoadScene("Loading");
        }

        private void MoveLoading01Onperformed(InputAction.CallbackContext obj)
        {
            Get<GameManager>().NextSceneInfo.NextScene = "Stage_1";
            SceneManager.LoadScene("Loading");
        }

        private void OnTriggerInitPlayerState(InputAction.CallbackContext obj) =>
            Messager.Send("InitPlayer");

        private void OnTriggerFullHP(InputAction.CallbackContext obj) =>
            HP = 3;

        private void OnEnable()
        {
            FullHP.Enable();
            PlayerStateInit.Enable();

            MoveLoading01.Enable();
            MoveLoading02.Enable();
            MoveLoading03.Enable();
        }

        public void ResetGame()
        {
            IsNotAttack = false;
            SetActiveCursor(false);
            IsPlayPuzzle = false;
            HP = 3;
            Dalgana = 0;
        }
        
        private void OnDisable()
        {
            FullHP.Disable();
            PlayerStateInit.Disable();

            MoveLoading01.Disable();
            MoveLoading02.Disable();
            MoveLoading03.Disable();
        }

        private void Update()
        {
            ChangeCursor();
        }

        private void ChangeCursor()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                Cursor.SetCursor(PressCursor, CursorOffset, CursorMode.Auto);

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                Cursor.SetCursor(NormalCursor, CursorOffset, CursorMode.Auto);
        }

        /// <summary>
        /// 카메라 마우스 락을 처리합니다.
        /// </summary>
        /// <param name="active">true시 마우스가 잠기고, false가 되면 마우스가 표시됩니다.</param>
        public void SetActiveCursor(bool active)
        {
            if (active)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
            }
        }

        /// <summary>
        /// 달고나를 체력으로 변경시킵니다.
        /// </summary>
        /// <returns>변경했을 때 true, 아닐 시 false를 반환합니다.</returns>
        public bool IsHpMax()
        {
            if (HP < 3)
                return false;

            return true;
        }

        /// <summary>
        /// HP를 1개 올리고 달고나를 리셋합니다.
        /// </summary>
        public void UpHpAndResetDalgona()
        {
            HP += 1;
            Dalgana = 0;
        }

        /// <summary>
        /// HP를 다시 채웁니다.
        /// </summary>
        public void GameReset()
        {
            HP = 3;
            Dalgana = 0;
        }

        /// <summary>
        /// 스크린 사이즈를 변경합니다.
        /// </summary>
        /// <param name="screenType"></param>
        public void ChangeScreenSize(EScreenType screenType)
        {
            int width = 0;
            int height = 0;

            switch (screenType)
            {
                case EScreenType.FullScreen:
                    width = Display.main.systemWidth;
                    height = Display.main.systemHeight;
                    Screen.SetResolution(width, height, true);
                    return;
                case EScreenType.S1920:
                    width = 1920;
                    height = 1080;
                    break;
                case EScreenType.S1280:
                    width = 1280;
                    height = 720;
                    break;
            }

            Screen.SetResolution(width, height, false);
            Application.targetFrameRate = 60;
        }

        /// <summary>
        /// 현재 씬이 인자와 같은 씬인지 확인합니다.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public bool CompareSceneName(string sceneName)
        {
            return SceneManager.GetActiveScene().name.Equals(sceneName);
        }
    }

    public struct NextSceneInfo
    {
        public bool UseLoading;
        public string NextScene;
    }
}