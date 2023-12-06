using System;
using AutoManager;
using Character.Model;
using Character.View;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Managers;
using Sirenix.OdinInspector;
using UITweenAnimation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlidePuzzle
{
    public class SlidePuzzleSystem : MonoBehaviour
    {
        private enum PuzzleCameraType
        {
            None,
            Puzzle,
            Origin
        }

        public enum PuzzlePlayState
        {
            None,
            Play,
            Finish
        }

        [ReadOnly] public PuzzlePlayState PlayState;
        public bool ActiveClick { get; set; }

        [SerializeField, Tooltip("비어있는 공간과 퍼즐 맵을 스왑하는 용도로 사용합니다.")]
        private Transform EmptySpace;

        [SerializeField, FoldoutGroup("카메라"), ValidateInput("@PuzzleCamera != null", "퍼즐용 시네머신 카메라 오브젝트가 비어있습니다.")]
        private GameObject PuzzleCamera;

        [SerializeField, FoldoutGroup("캔버스"), ValidateInput("@PuzzleCanvas != null", "퍼즐 캔버스가 비어있습니다.")]
        private GameObject PuzzleCanvas;

        [SerializeField] private EventReference[] SFXClips;
        [SerializeField] private Transform[] Puzzles;
        [SerializeField] private Transform[] CompletePuzzles;

        [SerializeField] private MeshRenderer[] PuzzleWall01MeshRenderers;
        [SerializeField] private MeshRenderer[] PuzzleWall02MeshRenderers;

        [SerializeField] private MeshRenderer[] PuzzleGroundMeshRenderers;

        [FoldoutGroup("머티리얼")] [SerializeField]
        private Material SuccessWall01Material;

        [FoldoutGroup("머티리얼")] [SerializeField]
        private Material SuccessWall02Material;

        [FoldoutGroup("머티리얼")] [SerializeField]
        private Material SuccessGroundMaterial;

        [FoldoutGroup("머티리얼")] [SerializeField]
        private Material Material11;

        [FoldoutGroup("머티리얼")] [SerializeField]
        private Material MaterialBend;

        private Camera _camera;

        private Vector3[] _completePuzzleInitPosition;
        private Vector3[] _puzzleInitPosition;

        private PlayerView _playerView;
        private PlayerModel _playerModel;

        public InputAction ReplaceKey;
        public InputAction AutoSuccessKey;

        private void Awake()
        {
            _camera = Camera.main;
            _puzzleInitPosition = new Vector3[Puzzles.Length];
            _completePuzzleInitPosition = new Vector3[Puzzles.Length];

            for (int i = 0; i < Puzzles.Length; i++)
                _puzzleInitPosition[i] = Puzzles[i].transform.position;

            for (int i = 0; i < CompletePuzzles.Length; i++)
                _completePuzzleInitPosition[i] = CompletePuzzles[i].transform.position;

            _playerView = FindObjectOfType<PlayerView>();
            _playerModel = FindObjectOfType<PlayerModel>();

            ReplaceKey.performed += _ => ReplacePuzzle();
            AutoSuccessKey.performed += _ => AutoSuccessPuzzle();
        }

        private void OnEnable()
        {
            ReplaceKey.Enable();
            AutoSuccessKey.Enable();
        }

        private void Update()
        {
            if (ActiveClick) return;

            //플레이 중이 아니라면, 리턴
            if (PlayState != PuzzlePlayState.Play) return;

            //이미 끝난 상태라면, 리턴
            if (PlayState == PuzzlePlayState.Finish) return;

            //일시정지 UI가 떳다면, 리턴
            if (UIController.Instance.IsPause) return;

            //마우스 클릭을 했을 경우
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                int layerMask = 1 << LayerMask.NameToLayer("Puzzle");

                Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
                {
                    //선택한 퍼즐이 비어있는 공간과 인접한지 확인합니다.
                    bool isInRange = Vector3.Distance(EmptySpace.position, hit.transform.position) < 15;

                    if (isInRange)
                        if (hit.transform.TryGetComponent(out MoveRoad moveRoad))
                        {
                            ActiveClick = true;
                            ChangePositionPuzzle(moveRoad);
                        }
                }
            }
        }

        private void OnDisable()
        {
            ReplaceKey.Disable();
            AutoSuccessKey.Disable();
        }

        /// <summary>
        /// 퍼즐 상태를 트리거합니다.
        /// </summary>
        public void OnTriggerPuzzleMode()
        {
            Time.timeScale = 0;
            Manager.Get<GameManager>().IsPlayPuzzle = true;
            PlayState = PuzzlePlayState.Play;
            SetActivePuzzle(true);
        }

        /// <summary>
        /// 원래 상태로 트리거합니다.
        /// </summary>
        public void OnTriggerOriginMode()
        {
            Manager.Get<GameManager>().IsPlayPuzzle = false;
            PlayState = PuzzlePlayState.Finish;

            DeleteAllNavAI();
            ChangeAllSuccessMaterial();
            ChangeToMainCamera().Forget();
        }

        /// <summary>
        /// 퍼즐 재배치
        /// </summary>
        private void ReplacePuzzle(bool playSound = true)
        {
            if (PlayState == PuzzlePlayState.Play)
            {
                for (int i = 0; i < Puzzles.Length; i++)
                    Puzzles[i].transform.position = _puzzleInitPosition[i];

                // @ 퍼즐 리셋 사운드
                if (playSound)
                    Manager.Get<AudioManager>().PlayOneShot(SFXClips[2], transform.position);
            }
        }

        /// <summary>
        /// 자동으로 퍼즐을 성공시킵니다.
        /// </summary>
        private void AutoSuccessPuzzle()
        {
            if (PlayState == PuzzlePlayState.Play)
            {
                for (int i = 0; i < Puzzles.Length; i++)
                {
                    Puzzles[i] = CompletePuzzles[i];
                    Puzzles[i].transform.position = _completePuzzleInitPosition[i];
                    ReplacePuzzle(false);
                }

                Manager.Get<AudioManager>().PlayOneShot(SFXClips[2], transform.position);
            }
        }

        /// <summary>
        /// 퍼즐의 위치를 이동시킵니다.
        /// </summary>
        /// <param name="moveRoad"></param>
        private void ChangePositionPuzzle(MoveRoad moveRoad)
        {
            Vector3 lastEmptySpacePosition = EmptySpace.position;

            EmptySpace.position = moveRoad.transform.position;
            moveRoad.ChangePosition(lastEmptySpacePosition);

            // @ 퍼즐 이동 사운드
            Manager.Get<AudioManager>().PlayOneShot(SFXClips[0], transform.position);
        }

        /// <summary>
        /// True가 되면 퍼즐 상태를 트리거하고, False시 일반 상태로 되돌립니다.
        /// </summary>
        /// <param name="active"></param>
        private void SetActivePuzzle(bool active)
        {
            _playerView.FreezeRotationCamera(active);
            _playerView.SetActiveCursor(!active);
            _playerView.SetActiveCrossHire(!active);

            _playerModel.IsStop = active;

            PuzzleCamera.SetActive(active);
            PuzzleCanvas.SetActive(active);

            if (active)
            {
                _playerView.GetInput().axisHorizontal = 0;
                _playerView.GetInput().axisVertical = 0;
            }
        }

        /// <summary>
        /// 메인 카메라로 전환합니다.
        /// </summary>
        private async UniTaskVoid ChangeToMainCamera()
        {
            // @ 퍼즐 완성 사운드
            Manager.Get<AudioManager>().PlayOneShot(SFXClips[1], transform.position);
            await UniTask.Delay(TimeSpan.FromSeconds(2), DelayType.UnscaledDeltaTime);

            Time.timeScale = 1;
            SetActivePuzzle(false);
        }

        /// <summary>
        /// 씬에 있는 NavAI를 모두 제거합니다.
        /// </summary>
        private void DeleteAllNavAI()
        {
            //모두 제거
            GameObject[] navAI = GameObject.FindGameObjectsWithTag("NavAI");

            foreach (GameObject ai in navAI)
                Destroy(ai);
        }

        /// <summary>
        /// 머티리얼을 모두 성공한 상태로 변경합니다.
        /// </summary>
        private void ChangeAllSuccessMaterial()
        {
            foreach (MeshRenderer meshRenderer in PuzzleWall01MeshRenderers)
                meshRenderer.sharedMaterial = SuccessWall01Material;

            foreach (MeshRenderer meshRenderer in PuzzleWall02MeshRenderers)
                meshRenderer.sharedMaterial = SuccessWall02Material;

            foreach (MeshRenderer meshRenderer in PuzzleGroundMeshRenderers)
                meshRenderer.sharedMaterial = SuccessGroundMaterial;
        }

        [Button("퍼즐 머티리얼 자동 적용01", ButtonSizes.Large), PropertySpace(20)]
        public void ApplyMaterial11()
        {
            foreach (MeshRenderer puzzleWall01MeshRenderer in PuzzleWall01MeshRenderers)
                puzzleWall01MeshRenderer.sharedMaterial = Material11;
        }

        [Button("퍼즐 머티리얼 자동 적용02", ButtonSizes.Large), PropertySpace(20)]
        public void ApplyMaterialBend()
        {
            foreach (MeshRenderer puzzleWall02MeshRenderer in PuzzleWall02MeshRenderers)
                puzzleWall02MeshRenderer.sharedMaterial = MaterialBend;
        }
    }
}