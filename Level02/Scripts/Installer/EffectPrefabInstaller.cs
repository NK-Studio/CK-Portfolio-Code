using Sirenix.OdinInspector;
using UnityEngine;
using Utility;
using Zenject;

namespace Installer
{
    public class EffectPrefabInstaller : MonoInstaller
    {
        [ValidateInput("@getItem != null", "아이템 섭취 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject getItem;

        [ValidateInput("@hpRecovery != null", "회복 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject hpRecovery;

        [ValidateInput("@landDust != null", "착지 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject landDust;

        [ValidateInput("@chocolateBomb != null", "초코 폭발 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject chocolateBomb;

        [ValidateInput("@ropeRush != null", "로프 러쉬 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject ropeRush;

        [ValidateInput("@attack01Red != null", "공격01Red 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject attack01Red;

        [ValidateInput("@attack01Blue != null", "공격01Blue 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject attack01Blue;

        [ValidateInput("@attack02Red != null", "공격02Red 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject attack02Red;

        [ValidateInput("@attack02Blue != null", "공격02Blue 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject attack02Blue;

        [ValidateInput("@hit != null", "피격 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject hit;

        [ValidateInput("@monsterHit != null", "몬스터 피격 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject monsterHit;
        
        [ValidateInput("@candyBombGround != null", "캔디 땅 폭발 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject candyBombGround;

        [ValidateInput("@candyBombAir != null", "캔디 공중 폭발 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject candyBombAir;

        [ValidateInput("@ropePang != null", "로프 팡 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject ropePang;

        [ValidateInput("@WaterSplash != null", "풍덩 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject WaterSplash;
        
        [ValidateInput("@wolfBossDeath != null", "늑대 사망 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject wolfBossDeath;
        
        [ValidateInput("@wolfBossDeath02 != null", "늑대 사망 이펙트02가 비어있습니다.")] [SerializeField]
        private GameObject wolfBossDeath02;
        
        [ValidateInput("@wolfBossGroggy != null", "늑대 기절 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject wolfBossGroggy;

        [ValidateInput("@wolfBossRush != null", "늑대 돌진 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject wolfBossRush;

        [ValidateInput("@wolfBossRushPrepare != null", "늑대 돌진 준비 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject wolfBossRushPrepare;

        [ValidateInput("@wolfBossScratch != null", "늑대 할퀴기 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject wolfBossScratch;

        [ValidateInput("@wolfBossJumpAttackStart != null", "늑대 점프공격 시작 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject wolfBossJumpAttackStart;

        [ValidateInput("@wolfBossJumpAttackSlash != null", "늑대 점프공격 슬래시 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject wolfBossJumpAttackSlash;

        [ValidateInput("@wolfBossJumpAttackGround != null", "늑대 점프공격 바닥 이펙트가 비어있습니다.")] [SerializeField]
        private GameObject wolfBossJumpAttackGround;

        public override void InstallBindings()
        {
            Container.Bind<GameObject>().WithId(EffectType.GetItem).FromInstance(getItem);
            Container.Bind<GameObject>().WithId(EffectType.HpRecovery).FromInstance(hpRecovery);
            Container.Bind<GameObject>().WithId(EffectType.LandDust).FromInstance(landDust);
            Container.Bind<GameObject>().WithId(EffectType.ChocolateBomb).FromInstance(chocolateBomb);
            Container.Bind<GameObject>().WithId(EffectType.RopeRush).FromInstance(ropeRush);
            Container.Bind<GameObject>().WithId(EffectType.Attack01Red).FromInstance(attack01Red);
            Container.Bind<GameObject>().WithId(EffectType.Attack01Blue).FromInstance(attack01Blue);
            Container.Bind<GameObject>().WithId(EffectType.Attack02Red).FromInstance(attack02Red);
            Container.Bind<GameObject>().WithId(EffectType.Attack02Blue).FromInstance(attack02Blue);
            Container.Bind<GameObject>().WithId(EffectType.Hit).FromInstance(hit);
            Container.Bind<GameObject>().WithId(EffectType.MonsterHit).FromInstance(monsterHit);
            Container.Bind<GameObject>().WithId(EffectType.CandyBombGround).FromInstance(candyBombGround);
            Container.Bind<GameObject>().WithId(EffectType.CandyBombAir).FromInstance(candyBombAir);
            Container.Bind<GameObject>().WithId(EffectType.RopePang).FromInstance(ropePang);
            Container.Bind<GameObject>().WithId(EffectType.WaterSplash).FromInstance(WaterSplash);
            Container.Bind<GameObject>().WithId(EffectType.WolfBossDeath).FromInstance(wolfBossDeath);
            Container.Bind<GameObject>().WithId(EffectType.WolfBossDeath02).FromInstance(wolfBossDeath02);
            Container.Bind<GameObject>().WithId(EffectType.WolfBossGroggy).FromInstance(wolfBossGroggy);
            Container.Bind<GameObject>().WithId(EffectType.WolfBossRush).FromInstance(wolfBossRush);
            Container.Bind<GameObject>().WithId(EffectType.WolfBossRushPrepare).FromInstance(wolfBossRushPrepare);
            Container.Bind<GameObject>().WithId(EffectType.WolfBossScratch).FromInstance(wolfBossScratch);
            Container.Bind<GameObject>().WithId(EffectType.WolfBossJumpAttackStart)
                .FromInstance(wolfBossJumpAttackStart);
            Container.Bind<GameObject>().WithId(EffectType.WolfBossJumpAttackSlash)
                .FromInstance(wolfBossJumpAttackSlash);
            Container.Bind<GameObject>().WithId(EffectType.WolfBossJumpAttackGround)
                .FromInstance(wolfBossJumpAttackGround);
        }
    }
}