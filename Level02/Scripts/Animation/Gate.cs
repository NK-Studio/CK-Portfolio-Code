using System;
using System.Threading;
using AutoManager;
using Character.Model;
using Cysharp.Threading.Tasks;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace Animation
{
    public class Gate : MonoBehaviour
    {
        [Title("열쇠가 들어갈 부모 타겟")] [SerializeField]
        private Transform[] targets;

        public Transform RedKey;
        public Transform YellowKey;
        public Transform GreenKey;

        [Title("애니메이터")] [SerializeField] private PlayableDirector CutScene;

        private static readonly int OnOpen = Animator.StringToHash("OnOpen");

        [Title("옵션")] [Tooltip("열쇠 3개가 모이면 트리거 한다.")]
        public bool AutoPlay = true;

        [Tooltip("열쇠 3개를 모이면 딜레이 후에 애니메이션을 실행합니다.")]
        public float Delay = 1f;

        [Tooltip("true로 체크하면 3개의 열쇠 캔디 타겟을 바인딩하지 않아도 경고를 띄우지 않습니다.")]
        public bool IgnoreBugMessage;

        [Tooltip("열릴때 추가적인 이벤트를 실행합니다.")] public UnityEvent OnOpenEvent;

        [SerializeField] private InputAction RedKeyInputKey;
        [SerializeField] private InputAction YellowKeyInputKey;

        [FormerlySerializedAs("BlueKeyInputKey")] [SerializeField]
        private InputAction GreenKeyInputKey;

        private CancellationTokenSource _ct;
        private PlayerModel _playerModel;

        private void Awake()
        {
            _ct = new CancellationTokenSource();
            _playerModel = FindObjectOfType<PlayerModel>();
        }

        private void OnEnable()
        {
            RedKeyInputKey.Enable();
            YellowKeyInputKey.Enable();
            GreenKeyInputKey.Enable();
        }

        private void Start()
        {
            if (targets.Length != 3)
            {
                if (!IgnoreBugMessage)
                    DebugX.LogError("열쇠 타겟이 3개가 아닙니다.");
                return;
            }

            TriggerGate().Forget();

            RedKeyInputKey.performed += _ => SetRedCandyKeyOnStand();
            YellowKeyInputKey.performed += _ => SetYellowKeyCandyKeyOnStand();
            GreenKeyInputKey.performed += _ => SetGreenCandyKeyOnStand();
        }

        public void SetRedCandyKeyOnStand() => SetCandyKeyOnStand(RedKey, 2);
        public void SetYellowKeyCandyKeyOnStand() => SetCandyKeyOnStand(YellowKey, 1);
        public void SetGreenCandyKeyOnStand() => SetCandyKeyOnStand(GreenKey, 0);

        private void SetCandyKeyOnStand(Transform targetKey, int standIndex)
        {
            targetKey.GetComponent<Rigidbody>().isKinematic = true;

            //손에 있는 별사탕을 제거 합니다.
            targetKey.parent = targets[standIndex];

            //위치 재 조정
            targetKey.localPosition = new Vector3(0, 0.739f, 0);
            targetKey.localRotation = Quaternion.identity;
            targetKey.localScale = Vector3.one;
            targetKey.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            //Pin을 숨깁니다.
            targetKey.GetChild(1).gameObject.SetActive(false);
        }

        private void Update()
        {
            if (CutScene.state == PlayState.Playing)
            {
                _playerModel.IsStop = true;
            }
        }

        private async UniTaskVoid TriggerGate()
        {
            // parallel with cancellation

            await UniTask.WaitUntil(() => targets[0].childCount == 1, cancellationToken: _ct.Token);
            await UniTask.WaitUntil(() => targets[1].childCount == 1, cancellationToken: _ct.Token);
            await UniTask.WaitUntil(() => targets[2].childCount == 1, cancellationToken: _ct.Token);

            //자동 실행이 아니면 return한다.
            if (!AutoPlay) return;

            await UniTask.Delay(TimeSpan.FromSeconds(Delay), cancellationToken: _ct.Token);

            OnTriggerGate();
        }

        private void OnDisable()
        {
            RedKeyInputKey.Disable();
            YellowKeyInputKey.Disable();
            GreenKeyInputKey.Disable();
        }

        private void OnDestroy()
        {
            if (_ct == null) return;

            _ct.Cancel();
            _ct.Dispose();
        }

        [Button("열기")]
        private void OnTriggerGate()
        {
            if (_ct == null) return;

            OnOpenEvent?.Invoke();
            CutScene.gameObject.SetActive(true);
            Manager.Get<GameManager>().IsNotAttack = true;
            Manager.Get<AudioManager>().SetParameterByBGM("Next", 1f);
        }

        /// <summary>
        /// 스탠드에 캔디가 있다면 그에 따라 값을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetHasCandyInTarget()
        {
            Vector3 result = Vector3.zero;

            bool hasRedCandy = targets[2].childCount > 0;
            bool hasYellowCandy = targets[1].childCount > 0;
            bool hasGreenCandy = targets[0].childCount > 0;

            if (hasRedCandy)
                result.x = 1;

            if (hasYellowCandy)
                result.y = 1;

            if (hasGreenCandy)
                result.z = 1;

            return result;
        }
    }
}