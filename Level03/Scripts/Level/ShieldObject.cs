using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Damage;
using EnumData;
using FMODPlus;
using Managers;
using UnityEngine.AI;
using UnityEngine.Events;
using Utility;

namespace Level
{
    public class ShieldObject : MonoBehaviour, IEntity
    {
        [field: SerializeField, BoxGroup("기본")]
        public float Health { get; set; } = 1f;

        [field: SerializeField, BoxGroup("기본")]
        public UnityEvent OnBreak;
        
        [field: SerializeField, BoxGroup("오브젝트")]
        public GameObject ShieldEffectRoot { get; private set; }
        
        [field: SerializeField, BoxGroup("오브젝트")]
        public Collider Collider { get; private set; }
        
        [field: SerializeField, BoxGroup("오브젝트")]
        public NavMeshObstacle Obstacle { get; private set; }
        
        [field: SerializeField, BoxGroup("오브젝트")]
        public GameObject ShieldBreakEffect { get; private set; }
        
        [field: SerializeField, BoxGroup("이펙트"), LabelText("MainShield")]
        public ParticleSystem ShieldEffectMainShield { get; private set; }
        private Material _shieldEffectMainShieldMaterial;

        [field: SerializeField, BoxGroup("이펙트"), LabelText("Shield_Crack_Particle")]
        public List<GameObject> ShieldEffectParticleOnDamageByHealth { get; private set; } = new();

        [field: SerializeField, BoxGroup("이펙트"), LabelText("_IsCracked02")]
        public List<int> ShieldEffectCrackLevelByHealth { get; private set; } = new()
        {
// left hp: 0, 1, 2, 3
            0, 2, 1, 0
        };

        [field: SerializeField, BoxGroup("이펙트"), LabelText("_Hit_ColorScaleCrackRemaped")]
        public List<AnimationCurve> ShieldEffectCrackCurveByHealth { get; private set; } = new()
        {
            null,
            AnimationCurve.EaseInOut(0f, 0.32f, 7f/60f, 0f),
            AnimationCurve.EaseInOut(0f, 0.109f, 7f/60f, 0f),
        };

        public bool IsFreeze => false;
        public float Height => 1f;
        
        private float _initialHealth;
        private void Awake()
        {
            _initialHealth = Health;
            InitializeMaterial();
        }

        private void InitializeMaterial()
        {
            var shieldEffectMainShieldRenderer = ShieldEffectMainShield.GetComponent<ParticleSystemRenderer>();
            if (shieldEffectMainShieldRenderer && !_shieldEffectMainShieldMaterial)
            {
                _shieldEffectMainShieldMaterial = shieldEffectMainShieldRenderer.material;
            }
        }

        public void Initialize()
        {
            gameObject.SetActive(true);
            ActiveShield(_initialHealth);
        }
        
        private void ActiveShield(float level)
        {
            InitializeMaterial();
            Health = level;
            if (Collider)
            {
                Collider.gameObject.SetActive(true);
                Collider.enabled = true;
            }
            if(Obstacle)
                Obstacle.enabled = true;
            ShieldEffectRoot.gameObject.SetActive(true);
            _shieldEffectMainShieldMaterial.SetFloat(IsCracked02, ShieldEffectCrackLevelByHealth[(int)level]);
        }
        
        protected bool IsBullet(GameObject obj) => obj.CompareTag("PlayerBullet");
        public EntityHitResult Damage(DamageInfo info)
        {
            if (IsBullet(info.Source))
            {
                PlaySoundOnce("HitShieldBullet");
                if (info is EnemyDamageInfo enemyDamageInfo)
                {
                    SpawnShieldHit(enemyDamageInfo);
                }
                return EntityHitResult.Defend;
            }

            // 적 또는 파괴 가능 오브젝트에 대한 공격(=빙결 미끄러짐 충돌)만 받음
            if (!info.Source.CompareTag("Enemy") && !info.Source.CompareTag("Destructible") 
                || !info.Source.TryGetComponent(out IFreezable freezable) || !freezable.IsFreeze)
            {
                Debug.Log($"BossAquus::Damage() - ignored by not freezable sleeping(source={info.Source}, tag={info.Source.tag}, freezable.IsSlipping={info.Source.GetComponent<IFreezable>()?.IsFreeze})", info.Source);
                return EntityHitResult.Invincible;
            }
                
            Debug.Log($"BossAquus::Damage() - shield damaged ({Health} => {Health - 1})");
            Health -= 1;
            OnShieldHit();
            return EntityHitResult.Invincible;
        }

        private void Update()
        {
            if (!ShieldEffectRoot || !ShieldEffectRoot.activeInHierarchy || !_shieldEffectMainShieldMaterial)
            {
                return;
            }
            
            var curve = _shieldEffectHitCurve;
            if (curve != null && curve.length > 0)
            {
                var value = curve.Evaluate(_shieldEffectTime);
                _shieldEffectMainShieldMaterial.SetFloat(HitColorScaleCrackRemaped, value);
                _shieldEffectTime += Time.deltaTime;

                if (_shieldEffectTime > curve.GetLength())
                {
                    _shieldEffectHitCurve = null;
                }
            }
        }

        private AnimationCurve _shieldEffectHitCurve;
        private float _shieldEffectTime;
        private static readonly int IsCracked02 = Shader.PropertyToID("_IsCracked02");
        private static readonly int HitColorScaleCrackRemaped = Shader.PropertyToID("_Hit_ColorScaleCrackRemaped");
        
        private void OnShieldHit()
        {
            if (Health <= 0)
            {
                if(Collider)
                    Collider.enabled = false;
                if(Obstacle)
                    Obstacle.enabled = false;
                ShieldEffectRoot.SetActive(false);
                ShieldBreakEffect.SetActive(true);
                OnBreak?.Invoke();
            }else if (Health >= 1)
            {
                int shieldLevel = (int)Health;
                ShieldEffectParticleOnDamageByHealth[shieldLevel]?.SetActive(true);
                _shieldEffectHitCurve = ShieldEffectCrackCurveByHealth[shieldLevel];
                if (_shieldEffectHitCurve != null)
                {
                    _shieldEffectTime = 0f;
                }
                _shieldEffectMainShieldMaterial.SetFloat(IsCracked02, ShieldEffectCrackLevelByHealth[shieldLevel]);
                PlaySoundOnce("HitShieldMonster");
            }
        }
        
        private void SpawnShieldHit(EnemyDamageInfo info)
        {
            if (info.ColliderInfo is SphereCollider sc)
            {
                var effect = EffectManager.Instance.Get(EffectType.AquusShieldBulletHit);
                var bulletPosition = info.Source.transform.position;
                var scTransform = sc.transform;
                var scOrigin = scTransform.TransformPoint(sc.center);
                // DrawUtility.DrawWireSphere(scOrigin, sc.radius * scTransform.lossyScale.x, 16, DrawUtility.DebugDrawer(Color.green, 5f, false));
                var surfacePosition = sc.ClosestPoint(bulletPosition);
                // DrawUtility.DrawWireSphere(surfacePosition, 0.5f, 16, DrawUtility.DebugDrawer(Color.yellow, 5f, false));
                var normal = (surfacePosition - scOrigin).Copy(y: 0f).normalized;
                // Debug.DrawLine(surfacePosition, surfacePosition + normal * 5f, Color.cyan, 5f, false);
                effect.transform.SetPositionAndRotation(
                    surfacePosition + effect.transform.position, 
                    Quaternion.LookRotation(normal)
                );
            }
            else
            if (info.ColliderInfo is MeshCollider mc)
            {
                var position = info.Source.transform.position;
                // DrawUtility.DrawWireSphere(position, 0.5f, 16, DrawUtility.DebugDrawer(Color.yellow, 5f, false));

                // var closest = position;
                var closest = mc.ClosestPoint(position);
                // Debug.Log($"PlayerBullet({info.Source.name}) collided MeshCollider {mc.name} at {position}(closest={closest},closestB={closestBound})");
                // DrawUtility.DrawWireSphere(closest, 0.5f, 16, DrawUtility.DebugDrawer(Color.green, 5f, false));
                
                // closest - position
                var bulletToClosest = (closest - position);
                var rayLength = bulletToClosest.magnitude;
                var ray = new Ray(position, bulletToClosest * (1f / rayLength));
                
                Vector3 normal;
                // 1. 총알(원형보다 바깥에 있음) -> 가장 가까운 점 방향으로 ray를 쏴서 normal을 구하기
                if (Physics.Raycast(ray, out var rayHit, rayLength * 2f, 1 << gameObject.layer))
                {
                    normal = rayHit.normal;
                }
                // 2. 최악의 상황이지만, 구하지 못한 경우 Mesh를 하나하나 긁어서 충돌 지점에서 가장 가까운 삼각형의 normal 구하기
                else
                {
                    var mesh = mc.sharedMesh;
                    var triangles = mesh.triangles;
                    var triangleLength = triangles.Length / 3;
                    var vertices = mesh.vertices;
                    var mct = mc.transform;
                    var localToWorld = mct.localToWorldMatrix;
                    var worldToLocal = mct.worldToLocalMatrix;

                    Vector3 closestOS = worldToLocal * closest.ToVector4(1f);
                    Vector3Int nearestTriangle = new Vector3Int(0, 1, 2);
                    float nearestDistanceSquared = closestOS.DistanceSquared((vertices[0] + vertices[1] + vertices[2]) * (1f/3f));
                    for (int i = 1; i < triangleLength; i++)
                    {
                        var t0 = triangles[i * 3 + 0];
                        var t1 = triangles[i * 3 + 1];
                        var t2 = triangles[i * 3 + 2];
                        
                        var v0 = vertices[t0];
                        var v1 = vertices[t1];
                        var v2 = vertices[t2];

                        // var v0ws = localToWorld * v0.ToVector4(1f);
                        // var v1ws = localToWorld * v1.ToVector4(1f);
                        // var v2ws = localToWorld * v2.ToVector4(1f);
                        var centerOS = (v0 + v1 + v2) * (1f/3f);
                        var distanceSquared = closestOS.DistanceSquared(centerOS);
                        if (distanceSquared < nearestDistanceSquared)
                        {
                            nearestDistanceSquared = distanceSquared;
                            nearestTriangle = new Vector3Int(t0, t1, t2);
                        }
                    }

                    var nv0 = localToWorld * vertices[nearestTriangle[0]].ToVector4(1f);
                    var nv1 = localToWorld * vertices[nearestTriangle[1]].ToVector4(1f);
                    var nv2 = localToWorld * vertices[nearestTriangle[2]].ToVector4(1f);
                    // var nearestCenterWS = (nv0 + nv1 + nv2) * (1f/3f);
                    normal = Vector3.Cross(
                        nv1 - nv0,
                        nv2 - nv1
                    ).normalized;
                    /*
                    Debug.DrawLine(nv0, nv1, Color.blue, 3f, false);
                    Debug.DrawLine(nv1, nv2, Color.blue, 3f, false);
                    Debug.DrawLine(nv0, nv2, Color.blue, 3f, false);
                    Debug.DrawLine(nearestCenter, nv0, Color.blue, 3f, false);
                    Debug.DrawLine(nearestCenter, nv1, Color.blue, 3f, false);
                    Debug.DrawLine(nearestCenter, nv2, Color.blue, 3f, false);
                    Debug.DrawLine(nearestCenter, nearestCenter + normal, Color.cyan, 3f, false);
                    */
                }
                var effect = EffectManager.Instance.Get(EffectType.AquusShieldBulletHit);
                effect.transform.SetPositionAndRotation(
                    closest + effect.transform.position, 
                    Quaternion.LookRotation(normal)
                );
            }
        }
        
        
        [field: SerializeField, BoxGroup("사운드")]
        public LocalKeyList Sounds { get; private set; }
        public void PlaySoundOnce(string key) => PlaySoundOnce(key, transform.position);
        public void PlaySoundOnce(string key, Vector3 position)
        {
            if (!Sounds.TryFindClip(key, out var clip))
            {
                Debug.Log($"{key}에 해당하는 클립 없음 !!!", Sounds);
                return;
            }
            
            AudioManager.Instance.PlayOneShot(clip, position);
        }

        private void OnDestroy()
        {
            if(_shieldEffectMainShieldMaterial) {
                Destroy(_shieldEffectMainShieldMaterial);
                _shieldEffectMainShieldMaterial = null;
            }
        }
    }
}