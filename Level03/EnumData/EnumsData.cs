using System;
using UnityEngine;

namespace EnumData
{
    public enum Axis { X, Y, Z }
    public static class AxisExtensions {
        public static float Get(this in Vector3 t, Axis axis)
        {
            return axis switch
            {
                Axis.X => t.x,
                Axis.Y => t.y,
                Axis.Z => t.z,
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
            };
        }
        public static Vector3 GetLocalAxis(this Transform t, Axis axis)
        {
            return axis switch
            {
                Axis.X => t.right,
                Axis.Y => t.up,
                Axis.Z => t.forward,
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
            };
        }
    }
    
    public enum ControllerState
    {
        Grounded, //땅에 있는 상태
        Sliding, //슬라이딩 상태
        Rising, //올라가는 상태
    }
    
    public enum EScreenType
    {
        FullScreen,
        S1920,
        S1280,
    }

    public enum PlayerAnimation
    {
        Idle,
        Flash,
        Attack,
        Behaviour,
        IceSpike,
        IceSpray,
        Shoot,
        ShootEnd,
        Reload,
        ReloadEnd,
        ItemChange,
        ItemChange_Upper,
        ShootAnimationSpeed,
        HammerPrepare,
        Hammer,
        HammerDash,
        HammerCancel,
        TimeCutter,
        FlashAttack,
        ZSlash,
        SwordAura,
        SectorAttack,
        CircleAttack,
        SkillRepeat,
        SlideEnterLeap,
        SlideEnterLand,
        SlideExitLeap,
        SlideExitLand,
        Stun,
        KnockBack,
        Dead,
        Fall,
    }
    
    public enum PlayerState
    {
        None,
        Idle,
        Dash,
        Shoot,
        PlayerBullet,
        PlayerBulletReload,
        BulletChange,
        HammerPrepare,
        Hammer,
        Sliding,
        Stun,
        KnockBack,
        Dead,
        Fall,
    }

    public enum KeyboardMoveType
    {
        MouseRightClick,
        Keyboard,
    }

    public static class PlayerStateExtensions
    {
        public static bool IsIdleState(this PlayerState state)
            => state switch
            {
                PlayerState.Idle => true,
                _ => false
            };
        /// <summary>
        /// 공격 상태인지 체크합니다.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsAttackState(this PlayerState state)
            => state switch
            {
                PlayerState.Shoot => true,
                _ => false
            };
        /// <summary>
        /// 특정 스킬의 준비 상태인지 체크합니다.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsSkillPrepareState(this PlayerState state)
            => state switch
            {
                PlayerState.HammerPrepare => true,
                _ => false
            };
        public static bool IsSkillUsingState(this PlayerState state)
            => state switch
            {
                PlayerState.Hammer => true,
                _ => false
            };

        public static bool IsSkillState(this PlayerState state)
            => state.IsSkillUsingState() || state.IsSkillPrepareState();

        public static bool IsNextSkillState(this PlayerState state, PlayerState prepare)
            => state switch
            {
                PlayerState.Hammer => prepare == PlayerState.HammerPrepare,
                _ => false
            };
        
        /// <summary>
        /// Stun 또는 KnockBack 상태인지 체크합니다.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsDamagingState(this PlayerState state)
            => state switch
            {
                PlayerState.Stun => true,
                PlayerState.KnockBack => true,
                _ => false
            };
    }
    
    
    
    public enum EffectType : uint
    {
        None = 0,
     
        // 넘버링 규칙:
        // XYYZZ - X: 대분류, Y: 중분류, Z: 소분류 
        
        // 10000 : 플레이어 관련 이펙트
        PlayerAttackSplash01    = 10000,
        PlayerAttackSplash02    = 10001,
        PlayerAttackSplash03    = 10002,
        PlayerAttackSplash04    = 10003,
        PlayerAttackSplash05    = 10004,
        PlayerFlash             = 10100,
        PlayerFlashAttack01     = 10200,
        PlayerFlashAttack02     = 10201,
        PlayerZSlash            = 10300,
        PlayerSwordAura         = 10400,
        PlayerSectorAttack      = 10500,
        PlayerCircleAttack      = 10600,
        PlayerIceSpike          = 10700,
        PlayerIceSpikeDrop      = 10701,
        PlayerIceSpikeExplode   = 10702,
        PlayerIceSpray          = 10710,
        PlayerIceBullet         = 10720,
        PlayerIceBulletTrail    = 10721,
        PlayerMuzzleFlash       = 10722,
        PlayerBreathe           = 10723,
        PlayerMissile           = 10724,
        PlayerRifle             = 10725,
        PlayerScatter           = 10726,
        PlayerHammerAttack      = 10800,
        PlayerItemChange        = 10900,
        PlayerHeartEat          = 10901,
        PlayerItemEat           = 10902,
        PlayerMouseClick        = 10100, // UNUSED
        
        // 20000 : [레거시] 보스(요루가미) 관련 이펙트
        BossNearAttack01            = 20000,
        BossNearAttack02            = 20001,
        BossNearAttack03            = 20002,
        BossNearAttack01Hit         = 20010,
        BossNearAttack02Hit         = 20011,
        BossNearAttack03Hit         = 20012,
        BossRangedAttack            = 20100,
        BossRangedAttackExplosion   = 20101,
        BossRangedAttackEffect01    = 20102,
        BossRangedAttackEffect02    = 20103,
        BossRangedAttackEffect03    = 20104,
        BossRangedAttackAura        = 20105,
        BossRushAttack              = 20200,
        BossRushAttackCharging      = 20201,
        BossWaveAttack              = 20300,
        BossSectorAttack            = 20400,
        BossDownAttack              = 20500,
        BossSpawnBomb               = 20600,
        BossSpawnBombExplosion      = 20601,
        BossDownAttackLeap          = 20602,
        BossFlashDisappear          = 20700,
        BossFlashAppear             = 20701,
        // 21000 : Aquus 이펙트
        AquusBullet01               = 21001,
        AquusBullet02               = 21002,
        AquusBullet03               = 21003,
        AquusBullet04               = 21004,
        AquusBullet05               = 21005,
        AquusBullet06               = 21006,
        AquusBullet07               = 21007,
        AquusBullet08               = 21008,
        AquusBullet09               = 21009,
        AquusBulletTrail01          = 21051,
        AquusBulletTrail02          = 21052,
        AquusBulletTrail03          = 21053,
        AquusBulletTrail04          = 21054,
        AquusBulletTrail05          = 21055,
        AquusBulletTrail06          = 21056,
        AquusBulletTrail07          = 21057,
        AquusBulletTrail08          = 21058,
        AquusBulletTrail09          = 21059,
        AquusBulletHit01            = 21070,
        AquusBulletSpawner          = 21090,
        AquusBulletSpawnerAppear    = 21091,
        AquusBulletSpawnerMuzzle    = 21092,
        
        AquusHarpStrike             = 21100,
        AquusDeadlyStrike           = 21101,
        AquusJellyfish              = 21200,
        AquusJellyfishExplosion     = 21201,
        AquusJellyfishDecal         = 21202,
        AquusBowAttack              = 21300,
        AquusBowAttackExplosion     = 21301,
        AquusBowAttackCharging      = 21302,
        AquusScream                 = 21400,
        AquusScreamStructure01      = 21401,
        AquusScreamStructure02      = 21402,
        AquusScreamStructure03      = 21403,
        AquusScreamStructure04      = 21404,
        AquusScreamStructure05      = 21405,
        AquusScreamStructureFall    = 21450,
        AquusShieldBulletHit        = 21500,
        
        
        // 30000 : 일반 잡몹 이펙트
        EnemyHitNormal                  = 30000,
        EnemyHitProjectile              = 30001, // 투사체 히트
        EnemyHitHammerToIce             = 30002, // 해머 히트: 언 상대에게
        EnemyHitHammerToNoneIce         = 30003, // 해머 히트: 안 언 상대에게
        EnemyIceCollide                 = 30004, // 얼어있는 적의 충돌
        EnemyStun                       = 30005,
        EnemyWaterSplash                = 30006,
        EnemyParabolaSpawnIndicator     = 30007,
        EnemyParabolaSpawnSplash        = 30008,
        TurretMonsterCannonExplosion    = 30100,
        EnemyFreezeClub                 = 30200,
        EnemyFreezeStingray             = 30201,
        EnemyFreezeSeahorse             = 30202,
        EnemyFreezeJellyfish            = 30203,
        EnemyFreezeBox                  = 30204,
        EnemyFreeze05                   = 30205,
        EnemyFreeze06                   = 30206,
        EnemyFreeze07                   = 30207,
        EnemyFreeze08                   = 30208,
        EnemyFreezeMeshBreak            = 30300,
        EnemySpawn                      = 30400,
        SeahorseCharging                = 30500,
        SeahorseProjectile              = 30501,
        SeahorseHit                     = 30502,
        SeahorseProjectileBurst         = 30503,
        SeahorseProjectileGuided        = 30504,
        JellyfishExplosion              = 30600,
        ClubMonsterAttackHit            = 30700,
        ClubMonsterAttack01             = 30701,
        ClubMonsterAttack02             = 30702,
    }

    public enum EnemyType
    {
        [InspectorName("없음 (None)")]
        None,
        [InspectorName("어인족 (ClubMonster)")]
        ClubMonster,
        [InspectorName("가오리 (StingrayMonster)")]
        StingrayMonster,
        [InspectorName("해마 (SeahorseMonster)")]
        SeahorseMonster,
        [InspectorName("해마 3점사 (SeahorseMonster02)")]
        SeahorseMonster02,
        [InspectorName("해마 유도탄 (SeahorseMonster03)")]
        SeahorseMonster03,
        [InspectorName("해파리 (JellyfishMonster)")]
        JellyfishMonster = 20,
        [InspectorName("보스: 아퀴스 (Aquus)")]
        Aquus = 9999,
    }

    public enum EnemyAttackType
    {
        None = 0,
        SwordMonster,
        Stingray,
        Seahorse,
        
        BossBullet,
        BossHarpStrike,
        BossJellyfish,
        BossBow,
        BossScream,
        
        _TypeCount,
    }
    
    public enum EQualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    public enum DamageMode
    {
        /// <summary>
        /// 일반 공격입니다. 즉시 피해가 들어갑니다.
        /// </summary>
        Normal,
        /// <summary>
        /// 피해를 축적합니다.
        /// </summary>
        Stack,
        /// <summary>
        /// 축적된 피해량과 이 피해량을 합쳐 한번에 가합니다.
        /// </summary>
        PopAll,
    }

    public enum DamageReaction
    {
        /// <summary>
        /// 약한 공격 - 일반 피격입니다. 애니메이션 출력이 없습니다.
        /// </summary>
        Normal,
        /// <summary>
        /// 일반 공격 - 경직입니다. OnDamage가 호출됩니다.
        /// </summary>
        Stun,
        /// <summary>
        /// 강한 공격 - 넉백입니다. OnKnockBack이 호출됩니다.
        /// </summary>
        KnockBack,
        /// <summary>
        /// 빙결 - OnFreeze가 호출됩니다.
        /// </summary>
        Freeze,
    }

    public enum ItemType
    {
        None    = 0,
        Heart   = 1,
        Rifle   = 100,
        Gatling = 101,
        Scatter = 102,
        Breathe = 103,
        Missile = 104,
        BulletBox = 199,
    }
}