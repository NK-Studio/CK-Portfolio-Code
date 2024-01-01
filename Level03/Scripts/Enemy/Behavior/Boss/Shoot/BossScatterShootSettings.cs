using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EnumData;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

namespace Enemy.Behavior.Boss
{
    [CreateAssetMenu(fileName = "ScatterShootSettings", menuName = "Settings/Boss/Scatter Shoot Settings", order = 0)]
    public class BossScatterShootSettings : BossShootSettings
    {
        public enum ScatterDirection
        {
            ClockWise, // 시계방향 
            CounterClockWise, // 반시계방향
        }
        
        [field: SerializeField, BoxGroup("패턴"), Tooltip("모든 발사가 끝날 때 까지 패턴 진행을 대기시킬 지 여부입니다. " +
                                                        "대기하지 않으면 발사와 동시에 다음 SubPattern으로 넘어갑니다.")]
        public bool Await { get; private set; } = true;
        
        [field: SerializeField, BoxGroup("흩뿌리기"), Tooltip("발사 간격입니다.")]
        public float Period { get; private set; } = 0.2f;
        
        [field: SerializeField, BoxGroup("흩뿌리기"), Tooltip("흩뿌리는 방향입니다.")]
        public ScatterDirection Direction { get; private set; } = ScatterDirection.ClockWise;

        [field: SerializeField, BoxGroup("흩뿌리기"), Tooltip("부채꼴에서 간격 각(degrees)입니다.")]
        public float ShootDirectionAngle { get; private set; } = 10f;
        
        [field: SerializeField, BoxGroup("흩뿌리기"), Tooltip("부채꼴 탄환 갯수입니다.")]
        public int ShootDirectionCount { get; private set; } = 4;
        
        
        public override async UniTask Shoot(BossAquus shooter, Vector3 shootPosition, Transform target, CancellationToken token)
        {
            if (BulletType == EffectType.None)
            {
                return;
            }


            if (Await)
                await ShootSequence(shooter, shootPosition, target, token);
            else 
                ShootSequence(shooter, shootPosition, target, token).Forget();
        }
        
        private async UniTask ShootSequence(BossAquus shooter, Vector3 shootPosition, Transform target, CancellationToken token)
        {
            var count = ShootDirectionCount;
            var direction = (target.position - shootPosition).Copy(y: 0f).normalized;
            // shooter.transform.forward = direction;
            
            var up = Direction == ScatterDirection.ClockWise ? Vector3.up : Vector3.down;
            var angleSum = ShootDirectionAngle * (count - 1);
            var startRotator = Quaternion.AngleAxis(angleSum * 0.5f, -up);

            var rotator = Quaternion.AngleAxis(ShootDirectionAngle, up);
            var rotation = startRotator * Quaternion.LookRotation(direction, Vector3.up);
            
            for (int i = 0; i < count; i++)
            {
                var bullet = EffectManager.Instance.Get(BulletType);
                bullet.transform.SetPositionAndRotation(shootPosition, rotation);

                if (bullet.TryGetComponent(out BossBullet b))
                {
                    b.Initialize(shooter, target);
                }
                
                if (shooter)
                {
                    var effect = EffectManager.Instance.Get(EffectType.AquusBulletSpawnerMuzzle);
                    effect.transform.position = shootPosition;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(Period), cancellationToken: token);

                rotation = rotator * rotation;
            }
        }

        public override void OnDrawGizmoDebug(Transform transform)
        {
            var count = ShootDirectionCount;
            if(count <= 0) return;

            var angleSum = ShootDirectionAngle * (count - 1);
            var leftRotator = Quaternion.AngleAxis(angleSum * 0.5f, Vector3.down);

            var origin = transform.position;
            var forward = transform.forward;
            
            var left = leftRotator * forward;
            var rotator = Quaternion.AngleAxis(ShootDirectionAngle, Vector3.up);
            var drawer = left * 5f;
            
            for (int i = 0; i < count; i++)
            {
                Gizmos.DrawLine(origin, origin + drawer);
                drawer = rotator * drawer;
            }

        }

        public override void OnGUIDebug(Transform transform)
        {

            if (BulletType != EffectType.None)
            {
                var debugTexts = new List<string>()
                {
                    $"[R] 다음 패턴 / [T] 발사",
                    $"이름: {name} [{GetType().Name}]",
                    "==============================",
                    Direction == ScatterDirection.ClockWise ? "시계 방향" : "반시계 방향",
                    $"발사 간격: {Period}",
                    $"부채꼴 사이각: {ShootDirectionAngle}",
                    $"탄환 개수: {ShootDirectionCount}",
                    "==============================",
                    $"탄환 Enum: {BulletType.ToString()}",
                };
                AddBulletInfoIfExists(debugTexts);

                var resultText = string.Join('\n', debugTexts);

                var padding = 20;
                var width = 300;
                var height = 200;
                var rect = new Rect(
                    padding, 
                    Screen.height - padding - height, 
                    width, height
                );
                var style = new GUIStyle
                {
                    alignment = TextAnchor.LowerLeft,
                    richText = true,
                };
                GUI.Box(rect, "<color=cyan>"+resultText+"</color>", style);
                
                //
                // var oldZTest = Handles.zTest;
                // Handles.zTest = CompareFunction.Disabled;
                // Handles.Label(transform.position, string.Join('\n', debugTexts), style);
                // Handles.zTest = oldZTest;
            }
            
        }
    }
}