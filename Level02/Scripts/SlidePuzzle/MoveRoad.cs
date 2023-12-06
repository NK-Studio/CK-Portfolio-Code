using DG.Tweening;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using Zenject;

namespace SlidePuzzle
{
    public class MoveRoad : MonoBehaviour
    {
        [Title("이동 시간")] public float Duration = 1f;

        [Title("애니메이션")] public Ease AnimationStyle = Ease.Linear;

        [Inject] private SlidePuzzleSystem _slidePuzzleSystem;

        private BoxCollider _collider;

        [SerializeField] private Transform Point01;
        [SerializeField] private Transform Point02;

        private void Awake()
        {
            if (TryGetComponent(out BoxCollider boxCollider)) _collider = boxCollider;
        }

        private void Start()
        {
            this.UpdateAsObservable().ObserveEveryValueChanged(_ => _slidePuzzleSystem.PlayState)
                .Where(state => state == SlidePuzzleSystem.PuzzlePlayState.Finish)
                .Subscribe(_ => _collider.enabled = false)
                .AddTo(this);
        }

        /// <summary>
        /// 해당 위치로 이동합니다.
        /// </summary>
        /// <param hitName="targetPosition"></param>
        public void ChangePosition(Vector3 targetPosition)
        {
            transform.DOMove(targetPosition, Duration).SetEase(AnimationStyle).SetUpdate(true).onComplete +=
                () => _slidePuzzleSystem.ActiveClick = false;
        }

        public void RotateAI(Transform target, string hitName,Transform oldParent)
        {
            if (hitName == "point01")
                target.parent = Point02;
            else if (hitName == "point02")
                target.parent = Point01;
            else
                return;

            target.localPosition = new Vector3(0, 0, 1.44f);
            target.localRotation = Quaternion.Euler(Vector3.zero);
            target.parent = oldParent;
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}