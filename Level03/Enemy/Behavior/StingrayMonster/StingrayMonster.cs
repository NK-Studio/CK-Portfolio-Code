using Character.Presenter;
using Cysharp.Threading.Tasks;
using Damage;
using EnumData;
using Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Behavior.StingrayMonster
{
    public class StingrayMonster : Monster
    {
        private StingrayMonsterSettings _settings;
        public StingrayMonsterSettings StingSettings => _settings ??= (StingrayMonsterSettings)base.Settings;
        
        public float RushMovementSpeed => StingSettings.RushMovementSpeed;
        // 돌진 거리 == 공격 시작 범위
        public float RushRange => StingSettings.RushRange;

        [field: ReadOnly]
        public bool IsRushing { get; private set; } = false;
        
        public void Rush()
        {
            if (IsRushing)
            {
                DebugX.LogWarning("ShieldMonster 돌진 중에 Rush() 호출");
                return;
            }

            // Rush 이벤트가 발생했으나, 피격에 의해 transition 중인 경우 돌진 캔슬
            var current = Animator.GetCurrentAnimatorStateInfo(0); 
            var next = Animator.GetNextAnimatorStateInfo(0);
            if (current.IsName("Attack") && (
                    next.IsName("Hit01") 
                    || next.IsName("Hit02")
                    || next.IsName("Dead")
            ))
            {
                // DebugX.Log("Rush cancelled by Hit Transition");
                return;
            }
            
            // Debug.Log("IsRushing = true");
            IsRushing = true;
            RushSequence().Forget();
        }


        private async UniTaskVoid RushSequence()
        {
            IsRushing = true;

            void PulseAttackRangeSensor()
            {
                AttackRangeSensor.Pulse();
                foreach (GameObject target in AttackRangeSensor.Detections)
                {
                    if (target.CompareTag("Player") && target.TryGetComponent(out PlayerPresenter player))
                    {
                        player.Damage(PlayerDamageInfo.Get(
                            StingSettings.AttackPower,
                            gameObject,
                            reaction: DamageReaction.Stun,
                            knockBack: new KnockBackInfo(
                                Vector3.Cross(transform.forward, Vector3.up),
                                10f, 0.2f
                            ),
                            enemyAttackType: EnemyAttackType.Stingray
                        ));
                    }
                }
            }
            
            var start = transform.position;
            var direction = transform.forward;
            // 15 frame
            var rushTime = StingSettings.RushAnimationFrames / 30f;
            float dt = Time.deltaTime;
            for (float t = 0f; t < rushTime; t += (dt = Time.deltaTime))
            {
                // Debug.Log($"Stingray({t:F1}) - IsRushing={IsRushing}");
                // 돌진 Interrupt
                if (!IsRushing)
                {
                    // Debug.Log($"Stingray({t:F1}) - Interrupted by IsRushing=false");
                    return;
                }

                // NavMeshAgent가 모종의 이유로 중단 (물리로 전환했든 뭐든 ..)
                if (!NavMeshAgent || !NavMeshAgent.enabled)
                {
                    InterruptRush();
                    return;
                }
                PulseAttackRangeSensor();
                var normalizedTime = t / rushTime;

                var warpTarget = start + direction * (normalizedTime * RushRange);
                // 경계선에 충돌하면 돌진 중단
                if (NavMeshAgent.Raycast(warpTarget, out var navHit))
                {
                    // Debug.Log($"Stingray({t:F1}) - Interrupted by Raycast");
                    InterruptRush();
                    NavMeshAgent.Warp(navHit.position);
                    return;
                }
                NavMeshAgent.Warp(warpTarget);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            
            NavMeshAgent.Warp(start + direction * RushRange);
            
            IsRushing = false;
            // Debug.Log($"Stingray Rush End");
        }

        private void InterruptRush(float transitionDuration = 0.25f)
        {
            IsRushing = false;
            // Idle 모션으로 강제 전환
            Animator.CrossFade("Idle", transitionDuration);
        }

        public override EntityHitResult Damage(DamageInfo info)
        {
            var source = info.Source;
            // 돌진 중일 때
            if (IsRushing && source && info is EnemyDamageInfo enemyDamageInfo)
            {
                // 평타 및 SwordAura에 대해서는 경직 및 넉백 애니메이션 무시함
                // switch (enemyDamageInfo.PlayerAttackType)
                // {
                //     case PlayerState.Attack01:
                //     case PlayerState.Attack02:
                //     case PlayerState.Attack03:
                //     case PlayerState.Attack04:
                //     case PlayerState.SwordAura:
                //         // DebugX.Log($"Reaction set to Normal because attackType was {info.PlayerAttackType}");
                //         info.Reaction = DamageReaction.Normal;
                //         break;
                // }
            }
            var result = base.Damage(info);

            // 죽었으면
            if (Health <= 0f)
            {
                // 강제 Rush Interrupt
                IsRushing = false;
            }

            return result;
        }


        protected override void OnKnockBack(DamageInfo info)
        {
            var from = info.Source;
            // 돌진 중일 때
            if (Health > 0 && IsRushing && info is EnemyDamageInfo enemyDamageInfo)
            {
                switch (enemyDamageInfo.PlayerAttackType)
                {
                    case PlayerState.Shoot:
                    case PlayerState.PlayerBullet:
                        return;
                //     case PlayerState.Attack01:
                //     case PlayerState.Attack02:
                //     case PlayerState.Attack03:
                //     case PlayerState.Attack04:
                //     case PlayerState.SwordAura:
                //         return;
                }
                
                // 그 외의 경직, 넉백은 돌진 중단
                InterruptRush(0f);
                DebugX.Log($"KnockBack cancelled due to IsRushing = false (from: {from}, playerState: {PlayerPresenter.Model.OtherState})", gameObject);
            }
            else
            {
                DebugX.Log("KnockBack but IsRushing = false || IsDead -> InterruptRush not executed", gameObject);
            }
            base.OnKnockBack(info);
        }
    }
}