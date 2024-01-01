using EnumData;
using Settings;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace Damage
{
    public enum EntityHitResult
    {
        /// <summary>
        /// 피해가 정상적으로 들어갔습니다.
        /// </summary>
        Success,
        /// <summary>
        /// 피해가 막혔습니다. 이 경우는 피격자에서 이펙트를 호출합니다.
        /// </summary>
        Defend,
        /// <summary>
        /// 무적 상태라 피해가 무시됐습니다. 이펙트는 정상적으로 호출됩니다.
        /// </summary>
        Invincible,
        /// <summary>
        /// 피해가 완전히 무시됐습니다. 투사체의 경우는 관통합니다.
        /// </summary>
        Ignored,
    }

    /// <summary>
    /// 피격받을 수 있는 모든 개체가 구현합니다.
    /// </summary>
    public interface IEntity
    {
        public string name { get; }
        public Transform transform { get; }
        public GameObject gameObject { get; }
        public bool isActiveAndEnabled { get; }
        public int GetInstanceID();
        
        public bool IsFreeze { get; }
        
        public float Health { get; set; }
        public float Height { get; }
        public EntityHitResult Damage(DamageInfo info);
    }
    
    /// <summary>
    /// 얼고, 녹고, 미끄러지고, 깨질 수 있는 모든 개체가 구현합니다.
    /// </summary>
    public interface IFreezable : IEntity
    {
        public bool SlipGuided { get; set; }
        public bool IsFreezeSlipping { get; }
        public bool IsFreezeFalling { get; }
        public bool CanBeDashTarget { get; }
    }
    
    /// <summary>
    /// 플레이어와 적대적인 개체가 구현합니다.
    /// </summary>
    public interface IHostile : IEntity { }


public class DamageInfo {
    public float Amount; // damage amount
    public GameObject Source; // damager
    public DamageMode Mode;
    public DamageReaction Reaction;
    public KnockBackInfo KnockBack; // knockback data
    
    
    protected virtual void Reset()
    {
        Source = null;
    }

    public virtual void Release()
    {
    }
} 

public readonly struct KnockBackInfo {
    public readonly Vector3 Direction;
    public readonly float Amount;
    public readonly float Time; // for NavMeshAgent
    public readonly ForceMode ForceMode; // for NavMeshAgent; if Force or Impulse, multiply EnemySettings::KnockBackPower, else not.
    public readonly float FreezeSlippingFactor;
    public readonly float FreezeSlippingAmount => Amount * FreezeSlippingFactor;

    public KnockBackInfo(
        Vector3 direction = default, 
        float amount = 0f, 
        float time = 0f, 
        ForceMode mode = ForceMode.Force,
        float freezeSlippingFactor = 0.1f
    ) {
        Direction = direction.normalized;
        Amount = amount;
        Time = time;
        ForceMode = mode;
        FreezeSlippingFactor = freezeSlippingFactor;
    }
    public KnockBackInfo(
        Transform from, 
        Transform target,
        float amount = 0f, 
        float time = 0f, 
        ForceMode mode = ForceMode.Force,
        float freezeSlippingFactor = 0.1f
    ) : this(target.position - from.position, amount, time, mode, freezeSlippingFactor) {
    }
    public KnockBackInfo(
        CharacterSettings.KnockBackSettings settings,
        Vector3 direction = default, 
        ForceMode mode = ForceMode.Force
    ) : this(direction, settings.Amount, settings.Time, mode, settings.FreezeFactor) {
    }
    public KnockBackInfo(
        CharacterSettings.KnockBackSettings settings,
        Transform from, 
        Transform target,
        ForceMode mode = ForceMode.Force
    ) : this(target.position - from.position, settings.Amount, settings.Time, mode, settings.FreezeFactor) {
    }

    public bool IsValid() => Amount > 0f;
    
    public static readonly KnockBackInfo None = default;
    
}

public class EnemyDamageInfo : DamageInfo {
    public PlayerState PlayerAttackType; // can be None if not caused by player
    public float FreezeFactor = float.NaN;
    public Collider ColliderInfo;
    
    private void Fill(
        float amount, 
        GameObject source = null, 
        DamageMode mode = DamageMode.Normal, 
        DamageReaction reaction = DamageReaction.Stun, 
        in KnockBackInfo knockBack = default, 
        PlayerState playerAttackType = PlayerState.None,
        float freezeFactor = 1f,
        Collider collider = null
    ) {
        Amount = amount;
        Source = source;
        Mode = mode;
        Reaction = reaction;
        KnockBack = knockBack;
        PlayerAttackType = playerAttackType;
        FreezeFactor = freezeFactor;
        ColliderInfo = collider;
    }

    public static EnemyDamageInfo Get(
        float amount,
        GameObject source = null,
        DamageMode mode = DamageMode.Normal,
        DamageReaction reaction = DamageReaction.Stun,
        in KnockBackInfo knockBack = default,
        PlayerState playerAttackType = PlayerState.None,
        float freezeFactor = 1f,
        Collider collider = null
    ) {
        var info = Get();
        info.Fill(amount, source, mode, reaction, knockBack, playerAttackType, freezeFactor, collider);
        return info;
    }


    public static EnemyDamageInfo Get() => Pool.Get();

    protected static EnemyDamageInfo Instantiate() => new();
    private static readonly ObjectPool<EnemyDamageInfo> Pool = new(
        Instantiate,
        _ => { },
        it => { it.Reset(); },
        it => { it.Reset(); },
        false,
        100
    );
    
    public override void Release()
    {
        Reset();
        Pool.Release(this);
    }
}

public class PlayerDamageInfo : DamageInfo {
    public EnemyAttackType EnemyAttackType; // can be None if not caused by enemy

    private void Fill(
        float amount, 
        GameObject source = null, 
        DamageMode mode = DamageMode.Normal, 
        DamageReaction reaction = DamageReaction.Stun, 
        in KnockBackInfo knockBack = default, 
        EnemyAttackType enemyAttackType = EnemyAttackType.None
    )
    {
        Amount = amount;
        Source = source;
        Mode = mode;
        Reaction = reaction;
        KnockBack = knockBack;
        EnemyAttackType = enemyAttackType;
    }

    public static PlayerDamageInfo Get(
        float amount,
        GameObject source = null,
        DamageMode mode = DamageMode.Normal,
        DamageReaction reaction = DamageReaction.Stun,
        in KnockBackInfo knockBack = default,
        EnemyAttackType enemyAttackType = EnemyAttackType.None
    )
    {
        var info = Get();
        info.Fill(amount, source, mode, reaction, knockBack, enemyAttackType);
        return info;
    }
    public static PlayerDamageInfo Get() => Pool.Get();

    protected static PlayerDamageInfo Instantiate() => new();
    private static readonly ObjectPool<PlayerDamageInfo> Pool = new(
        Instantiate,
        _ => { },
        it => { it.Reset(); },
        it => { it.Reset(); },
        false,
        100
    );
    
    public override void Release()
    {
        Reset();
        Pool.Release(this);
    }
} 

}