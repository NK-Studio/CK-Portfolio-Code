namespace Utility
{
    public enum EOtherState
    {
        Nothing, //아무것도 아닌 상태
        Aiming, //조준 중 상태
        Attack, //공격 중 상태
        HookShotRotate, //훅샷을 사용하여 공중에 있는 상태
        HookShotFlying, //훅샷을 사용하여 공중에 있는 상태
        ReadyRope, //로프를 던지기 전 준비 단계
        ThrowRope, //로프를 던진 상태
        Death, //죽음 상태
        Catch // 캐치 상태
    }

    public enum ERopeState
    {
        Pull, //당기기
        MoveToTarget //타겟으로 이동
    }

    public enum EHookState
    {
        Hide, //숨김 상태
        Impossible, //불가능 상태
        Occluded, //가려짐
        DistanceImpossible, //거리로 인한 불가능
        Possible, //훅을 던질 수 있음
    }

    public enum ControllerState
    {
        Grounded, //땅에 있는 상태
        Sliding, //슬라이딩 상태
        Falling, //낙하 상태
        Rising, //올라가는 상태
        Jumping //점프 상태
    }

    public enum InvincibleState
    {
        Original, //일반 상태
        Invincible //무적 상태
    }

    public enum PlayerAnimation
    {
        OnJump, //점프
        OnDJump, //더블 점프
        OnDeath, //죽음
        OnCatch, //잡기
        OnHit, //피격
        OnAttack,
        OnReAttackInputCheck,
        OnReAttack
    }

    public enum EffectType
    {
        GetItem,
        HpRecovery,
        LandDust,
        ChocolateBomb,
        RopeRush,
        Hit,
        CandyBombGround,
        Attack01Red,
        Attack01Blue,
        Attack02Red,
        Attack02Blue,
        CandyBombAir,
        RopePang,
        WolfBossDeath,
        WolfBossGroggy,
        WolfBossRush,
        WolfBossRushPrepare,
        WolfBossScratch,
        WolfBossJumpAttackStart,
        WolfBossJumpAttackSlash,
        WolfBossJumpAttackGround,
        MonsterHit,
        WaterSplash,
        WolfBossDeath02
    }
    
    public enum EScreenType
    {
        FullScreen,
        S1920,
        S1280,
    }
}