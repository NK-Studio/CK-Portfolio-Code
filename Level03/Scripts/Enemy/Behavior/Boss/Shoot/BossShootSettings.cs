using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EnumData;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    public abstract class BossShootSettings : BossShootSubPattern
    {
        [field: SerializeField, BoxGroup("탄환 종류")]
        public EffectType BulletType { get; private set; } = EffectType.AquusBullet01;

        public override async UniTask Execute(BossAquus boss, CancellationToken token)
        {
            await Shoot(boss, boss.ShootPosition.position, boss.PlayerPresenter.transform, token);
        }

        /// <summary>
        /// 특정 위치에서 목표를 향해 발사합니다.
        /// </summary>
        /// <param name="shooter">발사한 오브젝트입니다.</param>
        /// <param name="shootPosition">발사 위치입니다.</param>
        /// <param name="target">목표입니다.</param>
        public abstract UniTask Shoot(BossAquus shooter, Vector3 shootPosition, Transform target, CancellationToken token);

        public virtual void OnDrawGizmoDebug(Transform transform) { }
        public virtual void OnGUIDebug(Transform transform) { }
        
        
        protected void AddBulletInfoIfExists(List<string> debugTexts)
        {
            var bulletInfo = EffectManager.Instance?.PrefabSettingsMap?[BulletType].Prefab?.GetComponent<BossBullet>()?.Settings;

            if (bulletInfo)
            {
                debugTexts.Add($"탄환 이름: {bulletInfo.name}");
                debugTexts.Add($"탄환 분류: {bulletInfo.BulletType}");
                debugTexts.Add($"탄환 속도: {bulletInfo.BulletSpeed}m/s");
                debugTexts.Add(bulletInfo.PreventFreeze ? "빙결 불가능" : "빙결 가능");
                debugTexts.Add($"탄환 피해량: {bulletInfo.BulletDamage}");
                if(bulletInfo.BulletType == BossBulletSettings.Type.Guided)
                    debugTexts.Add($"탄환 회전속도: {bulletInfo.GuideRotationSpeed}º/s");
            }
        }
        
    }
}