using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(fileName = "New CharacterSettings", menuName = "Settings/CharacterSettings", order = 2)]
    public class CharacterSettings : ScriptableObject
    {
        #region HP & 달고나

        public const int HpMax = 3;
        public readonly int DalgonaMax = 2;

        #endregion

        #region 이동 속도

        [field: SerializeField, FoldoutGroup("이동 속도")]
        public float MovementSpeed { get; private set; } = 7;

        #endregion

        #region 점프

        [field: SerializeField, FoldoutGroup("점프")]
        public float JumpSpeed { get; private set; } = 10;

        [field: SerializeField, FoldoutGroup("점프")]
        public float DoubleJumpSpeed { get; private set; } = 10;

        [field: SerializeField, FoldoutGroup("점프"), Tooltip("점프를 진행하는 시간")]
        public float JumpDuration { get; private set; } = 0.2f;

        //더블 점프 가능 횟수
        [field: SerializeField, FoldoutGroup("점프")]
        public int DoubleJumpCount { get; private set; } = 1;

        #endregion

        #region 공중 제어

        //플레이어가 땅에 닿은 경우 'GroundFriction'이 대신 사용됩니다.
        [field: SerializeField, FoldoutGroup("공중 제어"), Tooltip("컨트롤러가 공중에서 얼마나 빨리 운동량을 잃는지 결정합니다.")]
        public float AirFriction { get; private set; } = 0.5f;

        [field: SerializeField, FoldoutGroup("공중 제어")]
        public float GroundFriction { get; private set; } = 100f;

        //값이 높을수록 더 많은 공기 제어가 가능합니다.
        [field: SerializeField, FoldoutGroup("공중 제어"), Tooltip("컨트롤러가 공중에서 얼마나 빨리 방향을 바꿀 수 있는지 결정합니다.")]
        public float airControlRate { get; private set; } = 2f;

        #endregion

        #region 중력 및 경사면

        //하향 중력의 양;
        [field: SerializeField, FoldoutGroup("중력 및 경사면")]
        public float gravity { get; private set; } = 30f;

        [field: SerializeField, FoldoutGroup("중력 및 경사면"), Tooltip("캐릭터가 가파른 비탈을 얼마나 빨리 미끄러지는가?")]
        public float slideGravity { get; private set; } = 5f;

        [field: SerializeField, FoldoutGroup("중력 및 경사면"), Tooltip("허용 가능한 경사 각도 제한")]
        public float slopeLimit { get; private set; } = 80f;

        #endregion
        
        #region 훅
        [field: SerializeField, FoldoutGroup("훅")]
        public float HookDistanceMax { get; private set; } = 3;
        
        [FoldoutGroup("훅"), Tooltip("타겟팅을 하지 않았을 때 목표로 하는 거리입니다.")]
        public float nonTargetSeeLength = 20;

        [FoldoutGroup("훅"), Tooltip("로프 최대 길이")]
        public float hookLengthMax = 5;

        [FoldoutGroup("훅"), Tooltip("로프가 날아가는 속도")]
        public float hookForwardSpeed = 15;

        [FoldoutGroup("훅"), Tooltip("훅이 되돌아 올 때 속도")]
        public float hookBackendSpeed = 15;

        [FoldoutGroup("훅"), Tooltip("훅이 실패해서 되돌아 올 때 속도")]
        public float hookFailBackendSpeed = 10;

        [FoldoutGroup("훅"), Tooltip("로프를 던져서 타겟에게 날아가는 속도")]
        public float flyToTargetSpeed = 15;

        [FoldoutGroup("훅"), Tooltip("로프를 던져서 타겟에게 날아갈 때 멈추는 위치")]
        public float placePlayerByHookFly = 2f;

        [FoldoutGroup("훅"), Tooltip("훅을 던져서 타겟을 놓는 거리")]
        public float hookTargetPlaceDistance = 1f;

        #endregion

        #region 훅샷

        [FoldoutGroup("훅샷"), Tooltip("훅샷을 걸 수 있는 오브젝트 레이어")]
        public LayerMask moveToTargetLayerMask;

        [FoldoutGroup("훅샷"), Tooltip("훅샷을 사용했을 때 속도")]
        public float hookShotSpeed = 5f;

        [FoldoutGroup("훅샷"), Tooltip("도착지점에 도착하여 훅샷이 풀리는 거리")]
        public float finishHookShotDistanceFromArrive = 1;

        [FoldoutGroup("훅샷"), Tooltip("훅샷이 가능한 거리")]
        public float hookShotPossibleDistance = 4;

        [FoldoutGroup("훅샷"), Tooltip("훅샷이 가상의 센터 포인트으로부터 허용되는 거리" + "\n-1이면 무조건 허용됩니다.")]
        public float hookShotPossibleCenterDistance = -1;

        [FoldoutGroup("훅샷"), Tooltip("훅샷 포인트를 찾는 반경")]
        public float hookShotFindRadius = 3;

        [FoldoutGroup("훅샷"), Tooltip("훅샷을 하고 나서 앞으로 튀는 세기")]
        public float bounceOffPower  = 14;
        
        #endregion

        #region 당기기

        [FoldoutGroup("당기기"), Tooltip("당기기를 할 수 있는 오브젝트 레이어")]
        public LayerMask pullLayerMask;
        
        [FoldoutGroup("당기기"), Tooltip("당기기 오브젝트를 찾는 반경")]
        public float pullObjectFindRadius = 5;
        
        [FoldoutGroup("당기기"), Tooltip("당기기를 했을 때 가상의 센터 포인트으로부터 허용되는 거리")]
        public float pullPossibleCenterDistance = 1;
        
        #endregion
        
        #region 무적

        [FoldoutGroup("무적"), Tooltip("무적 상태를 되돌리는 시간")]
        public float invincibleRevertTime = 1f;

        #endregion

        #region 넉백

        [FoldoutGroup("넉백"), Tooltip("탄에 맞았을 때 넉백 파워")]
        public float bulletKnockBackPower = 4f;

        #endregion

        #region SFX Clips
        public EventReference[] SFXClips;
        #endregion

        [field: SerializeField, Tooltip("컨트롤러의 변환을 기준으로 운동량을 계산하고 적용할지 여부입니다.")]
        public bool useLocalMomentum { get; private set; }

        [Tooltip("최대 거리입니다.")] public float maxThrowAttackDistance = 300f;

        [field: SerializeField, Tooltip("공격모드가 풀리는 시간입니다. (초 단위)")]
        public int FightReleaseTime { get; private set; } = 5;
    }
}