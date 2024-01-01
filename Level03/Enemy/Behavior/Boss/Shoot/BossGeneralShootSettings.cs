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
    [CreateAssetMenu(fileName = "GeneralShootSettings", menuName = "Settings/Boss/General Shoot Settings", order = 0)]
    public class BossGeneralShootSettings : BossShootSettings
    {
        public enum ShootPeriod
        {
            Single, Repeat, Burst,
        }

        [field: SerializeField, BoxGroup("패턴"), Tooltip("모든 발사가 끝날 때 까지 패턴 진행을 대기시킬 지 여부입니다. " +
                                                        "대기하지 않으면 발사와 동시에 다음 SubPattern으로 넘어갑니다.")]
        public bool Await { get; private set; } = true;
        
        [field: SerializeField, BoxGroup("단발, 점사, 연발"), Tooltip("발사 종류입니다.")]
        public ShootPeriod Type { get; private set; } = ShootPeriod.Repeat;
        
        [field: SerializeField, BoxGroup("단발, 점사, 연발"), Tooltip("발사 간격입니다."), HideIf("@Type == ShootPeriod.Single")]
        public float Period { get; private set; } = 0.2f;
        
        [field: SerializeField, BoxGroup("단발, 점사, 연발"), Tooltip("발사 횟수입니다."), HideIf("@Type == ShootPeriod.Single")]
        public int Count { get; private set; } = 3;
        
        [field: SerializeField, BoxGroup("단발, 점사, 연발"), Tooltip("점사 간격입니다."), HideIf("@Type != ShootPeriod.Burst")]
        public float BurstPeriod { get; private set; } = 0.75f;
        
        [field: SerializeField, BoxGroup("단발, 점사, 연발"), Tooltip("점사 횟수입니다."), HideIf("@Type != ShootPeriod.Burst")]
        public int BurstCount { get; private set; } = 3;


        [field: SerializeField, BoxGroup("발사 형태"), Tooltip("부채꼴에서 간격 각(degrees)입니다.")]
        public float ShootDirectionAngle { get; private set; } = 10f;
        
        [field: SerializeField, BoxGroup("발사 형태"), Tooltip("부채꼴 탄환 갯수입니다.")]
        public int ShootDirectionCount { get; private set; } = 4;
        
        [field: SerializeField, BoxGroup("발사 형태"), Tooltip("발사 방향 회전 오프셋 각(degrees)입니다. 양수는 시계방향으로 회전합니다.")]
        public float ShootDirectionOffset { get; private set; } = 0f;
        
        
        
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
            if (Type == ShootPeriod.Single)
            {
                ShootOnce(shooter, shootPosition, target);
                return;
            }

            var repeatCount = Type == ShootPeriod.Burst ? BurstCount : 1;
            var period = TimeSpan.FromSeconds(Period);
            var burstPeriod = TimeSpan.FromSeconds(BurstPeriod);
            for (int repeat = 0; repeat < repeatCount; repeat++)
            {
                for (int k = 0; k < Count; k++)
                {
                    ShootOnce(shooter, shootPosition, target);
                    if (k < Count - 1)
                    {
                        await UniTask.Delay(period, cancellationToken: token);
                    }
                }
                
                await UniTask.Delay(burstPeriod, cancellationToken: token);
            }
        }

        private void ShootOnce(BossAquus shooter, Vector3 shootPosition, Transform target)
        {
            var count = ShootDirectionCount;
            var direction = (target.position - shootPosition).Copy(y: 0f).normalized;
            // shooter.transform.forward = direction;
            
            var angleSum = ShootDirectionAngle * (count - 1);
            var leftRotator = Quaternion.AngleAxis(angleSum * 0.5f, Vector3.down);

            var offsetRotation = Quaternion.AngleAxis(ShootDirectionOffset, Vector3.up);
            var rotator = Quaternion.AngleAxis(ShootDirectionAngle, Vector3.up);
            var rotation = leftRotator * Quaternion.LookRotation(offsetRotation * direction, Vector3.up);
            
            for (int i = 0; i < count; i++)
            {
                var bullet = EffectManager.Instance.Get(BulletType);
                bullet.transform.SetPositionAndRotation(shootPosition, rotation);

                if (bullet.TryGetComponent(out BossBullet b))
                {
                    b.Initialize(shooter, target);
                }

                rotation = rotator * rotation;
            }

            if (shooter)
            {
                var effect = EffectManager.Instance.Get(EffectType.AquusBulletSpawnerMuzzle);
                effect.transform.position = shootPosition;
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
                    $"부채꼴 사이각: {ShootDirectionAngle}",
                    $"탄환 개수: {ShootDirectionCount}",
                    "==============================",
                    $"탄환 Enum: {BulletType.ToString()}",
                };
                AddBulletInfoIfExists(debugTexts);
                
                debugTexts.Add("==============================");
                debugTexts.Add($"반복 타입: {Type.ToString()}");
                if (Type != ShootPeriod.Single)
                {
                    debugTexts.Add($"발사 간격: {Period}");
                    debugTexts.Add($"발사 횟수: {Count}");

                    if (Type == ShootPeriod.Burst)
                    {
                        debugTexts.Add($"점사 간격: {BurstPeriod}");
                        debugTexts.Add($"점사 횟수: {BurstCount}");
                    }
                }
                debugTexts.Add("==============================");

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
                GUI.Box(rect, "<color=yellow>"+resultText+"</color>", style);
                
                //
                // var oldZTest = Handles.zTest;
                // Handles.zTest = CompareFunction.Disabled;
                // Handles.Label(transform.position, string.Join('\n', debugTexts), style);
                // Handles.zTest = oldZTest;
            }
            
        }
    }
}