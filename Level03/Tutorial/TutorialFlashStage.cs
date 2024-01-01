using Character.Presenter;
using Cysharp.Threading.Tasks;
using Damage;
using DG.Tweening;
using Managers;
using UniRx;
using UnityEngine;

namespace Tutorial
{
    public class TutorialFlashStage : TutorialBase
    {
        public TutorialFallProjectile ProjectilePrefab;
        public float FallTime = 3f;
        public float WaitTime = 1f;
        public float Height = 5f;
        public int TargetSuccessCount = 2;

        private float _currentWaitTime;
        private int _leftShootCount = 0; 
        private PlayerPresenter _player;
        public override void Initialize(TutorialController controller)
        {
            _player = GameManager.Instance.Player;
        }

        // 피해입으면 다시 발사 카운트 초기화
        private void OnPlayerDamaged(DamageInfo info)
        {
            info.Amount = 0f;
            _leftShootCount = TargetSuccessCount;
        }

        public override void Enter()
        {
            _leftShootCount = TargetSuccessCount;
            _player.OnDamageEvent += OnPlayerDamaged;
        }

        public override Result Execute()
        {
            // 발사 중이면 대기
            if (_currentWaitTime > 0f)
            {
                _currentWaitTime -= Time.deltaTime;
                return Result.Running;
            }
            // 전부 쐈으면 성공
            if (_leftShootCount <= 0)
            {
                return Result.Done;
            }

            Shoot();
            _leftShootCount -= 1;
            _currentWaitTime = FallTime + WaitTime;
            return Result.Running;
        }

        private void Shoot()
        {
            var target = _player.transform.position;
            var start = target + Vector3.up * Height;
            var obj = Instantiate(
                ProjectilePrefab, 
                start,
                Quaternion.identity
            );
            obj.Initialize(start, target, FallTime);

            ProjectileFallSequence(obj, target).Forget();
        }

        private async UniTaskVoid ProjectileFallSequence(
            TutorialFallProjectile obj, 
            Vector3 target
        )
        {
            var t = obj.transform;
            await t.DOMove(target, FallTime).SetEase(Ease.Linear);
            obj.Explode();
        }

        public override void Exit()
        {
            _player.OnDamageEvent -= OnPlayerDamaged;
        }
    }
}