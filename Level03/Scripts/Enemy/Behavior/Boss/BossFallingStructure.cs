using Character.Presenter;
using Damage;
using Effect;
using EnumData;
using Managers;
using Settings.Boss;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    public class BossFallingStructure : MonoBehaviour
    {
        
        [BoxGroup("상태"), SerializeField, ReadOnly] private float _fallAfter;
        [BoxGroup("상태"), SerializeField, ReadOnly] private float _fallSpeed;
        [BoxGroup("상태"), SerializeField, ReadOnly] private float _time;
        [BoxGroup("상태"), SerializeField, ReadOnly] private float _simulatedFallTime;

        [field: BoxGroup("상태"), SerializeField, ReadOnly] 
        public bool Fallen { get; private set; } = false;

        [field: BoxGroup("상태"), SerializeField, ReadOnly]
        public bool IsValid { get; private set; } = true;
        
        // [field: BoxGroup("설정"), SerializeField]
        public BossAquusSettings.ScreamSettings Settings { get; private set; }
        [field: BoxGroup("설정"), SerializeField] 
        public DecalEffect FakeShadow { get; private set; }
        private Rigidbody _rigidbody;
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (!_rigidbody)
            {
                Debug.LogWarning("BossFallingStructure에 Rigidbody가 없음", gameObject);
                return;
            }
            if (!FakeShadow)
            {
                Debug.LogWarning("BossFallingStructure에 FakeShadow가 없음", gameObject);
                return;
            }
        }

        public void Initialize(BossAquusSettings.ScreamSettings settings, float delay, float speed)
        {
            Settings = settings;
            _time = 0f;
            _fallAfter = delay;
            _fallSpeed = speed;
            Fallen = false;
            FakeShadow.transform.forward = Vector3.down;
            FakeShadow.gameObject.SetActive(false);

            {
                var ray = new Ray(transform.position, Vector3.down);
                if (!Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity,
                        LayerMask.GetMask("Ground")
                    ))
                {
                    Debug.LogWarning($"BossFallingStructure {name}이 바닥 찾기에 실패함 !!!", gameObject);
                    _simulatedFallTime = float.NaN;
                    IsValid = false;
                    return;
                }

                var distance = hitInfo.distance;
                _simulatedFallTime = distance / _fallSpeed;
                IsValid = true;
            }
        }


        private void Update()
        {
            if (Fallen)
            {
                return;
            }

            _time += Time.deltaTime;

            if (_time >= Settings.InvalidationTime)
            {
                Fallen = true;
                gameObject.SetActive(false);
                return;
            }
            if (_time >= _fallAfter)
            {
                if (float.IsNaN(_simulatedFallTime))
                {
                    return;
                }

                FakeShadow.gameObject.SetActive(true);
                    
                float t = (_time - _fallAfter) / _simulatedFallTime;
                FakeShadow.Opacity = Settings.StructureDecalAlphaCurve.Evaluate(t);
                FakeShadow.Progress = Settings.StructureDecalOffsetCurve.Evaluate(t);
            }
        }

        private void FixedUpdate()
        {
            if (_time < _fallAfter || Fallen)
            {
                return;
            }
            
            _rigidbody.MovePosition(_rigidbody.position + Vector3.down * (_fallSpeed * Time.fixedDeltaTime));
        }

        private Collider[] _playerCollider = new Collider[1];
        private void OnCollisionEnter(Collision other)
        {
            Debug.Log($"{name} OnCollisionEnter with {other.gameObject.name}");
            Fallen = true;

            // 플레이어 충돌 시 피격
            if (Physics.OverlapSphereNonAlloc(_rigidbody.position, Settings.DamageRange, _playerCollider,
                    LayerMask.GetMask("Character")) > 0
            )
            {
                var player = _playerCollider[0].GetComponent<PlayerPresenter>();
                player.Damage(PlayerDamageInfo.Get(
                    Settings.Damage, gameObject, DamageMode.Normal, DamageReaction.KnockBack, 
                    enemyAttackType: EnemyAttackType.BossJellyfish
                ));
            }
            
            // 추락해서 박살나는 이펙트 출력
            EffectManager.Instance.Get(EffectType.AquusScreamStructureFall).transform.position = transform.position;
            
            // TODO rayfire
            
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            FakeShadow.gameObject.SetActive(false);
        }

        private void OnDrawGizmos()
        {
            if (ReferenceEquals(Settings, null))
            {
                return;
            }
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Settings.DamageRange);
        }
    }
}