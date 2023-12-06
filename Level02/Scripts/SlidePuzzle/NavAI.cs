using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Zenject;

namespace SlidePuzzle
{
    public class NavAI : MonoBehaviour
    {
        [Title("생명 주기")] [Tooltip("삭제 타이머")] public float LifeTime = 4;

        [Title("이동 속도")] [Tooltip("이동 속도")] public float MoveSpeed = 50;

        [Title("디버그 모드")]
        [SerializeField] private bool DebugMode;
        
        [Inject] private SlidePuzzleSystem _puzzleSystem;

        private void Awake()
        {
            if (DebugMode)
                transform.GetChild(0).gameObject.SetActive(true);
        }

        private void Start()
        {
            Init();

            this.UpdateAsObservable()
                .Subscribe(_ =>
                {
                    Move();
                    ChangeRotation();
                    ExitPuzzle();
                    EnterWall();
                })
                .AddTo(this);

            Destroy(gameObject, LifeTime);
        }

        private void Init()
        {
            transform.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));
        }

        private void Move()
        {
            transform.Translate(Vector3.forward * MoveSpeed * Time.unscaledDeltaTime);
        }

        private void ExitPuzzle()
        {
            Ray ray = new(transform.position, transform.forward);

            int layerMask = 1 << LayerMask.NameToLayer("PuzzleExit");

            if (Physics.Raycast(ray, 1, layerMask))
            {
                _puzzleSystem.OnTriggerOriginMode();
                Destroy(gameObject);
            }
        }

        private void EnterWall()
        {
            Ray ray = new(transform.position, transform.forward);

            int layerMask = 1 << LayerMask.NameToLayer("Default");

            if (Physics.Raycast(ray, 1, layerMask))
                Destroy(gameObject);
        }

        private void ChangeRotation()
        {
            Ray ray = new(transform.position, transform.forward);

            int layerMask = 1 << LayerMask.NameToLayer("PuzzleRotater");

            if (Physics.Raycast(ray, out RaycastHit hit, 1, layerMask))
            {
                MoveRoad moveRoad = hit.collider.gameObject.GetComponentInParent<MoveRoad>();

                moveRoad.RotateAI(transform, hit.collider.name, transform.parent);
            }
        }
    }
}