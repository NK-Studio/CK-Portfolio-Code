using System;
using AutoManager;
using Character.USystem.Throw;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;
using Zenject;

namespace Enemys
{
    public enum PivotStyle
    {
        Center,
        CenterBottom,
    }

    public class StarCandyBomb : MonoBehaviour
    {
        [ValidateInput("@Settings != null", "세팅 파일이 비어 있습니다."), Title("세팅 데이터"), SerializeField]
        private StarCandySettings Settings;

        [ValidateInput("@explosionRange != null", "폭발 범위 콜라이더가 비어있습니다."), Title("폭발 범위"), SerializeField]
        private SphereCollider explosionRange;

        [Title("디버그 모드")] [SerializeField] private bool debugMode;
        [Inject] private DiContainer _container;

        private GameObject _effect;
        private Throwable _throwable;
        private Transform _gfx;

        private bool _touchExplosion;


        private void Awake()
        {
            _throwable = GetComponent<Throwable>();
            _gfx = transform.GetChild(0);
            _touchExplosion = false;
            explosionRange.radius = Settings.AttackRange;
            explosionRange.enabled = false;
        }

        [DisableInEditorMode, Button("폭발")]
        public void TestExplosion()
        {
            OnTriggerExplosion().Forget();
        }

        /// <summary>
        /// N초 후에 폭발을 트리거합니다.
        /// <param name="isAir">공중 또는 지상 폭발 이펙트 결정, 기본 false</param>
        /// <param name="time">폭발 딜레이, 음수일 시 기본 설정된 ExplodeDelay 사용</param>
        /// </summary>
        public async UniTaskVoid OnTriggerExplosion(bool isAir = false, float time = -1f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(time >= 0 ? time : Settings.ExplodeDelay),
                cancellationToken: this.GetCancellationTokenOnDestroy());

            Explode(isAir ? EffectType.CandyBombAir : EffectType.CandyBombGround).Forget();
        }

        /// <summary>
        /// 플레이어 및 주변 Enemy 폭발 피해 처리, 오브젝트 삭제
        /// </summary>
        private async UniTaskVoid Explode(EffectType effectType)
        {
            //폭발 이펙트 지정
            if (effectType == EffectType.CandyBombGround)
                _effect = _container.ResolveId<GameObject>(EffectType.CandyBombGround);
            else if (effectType == EffectType.CandyBombAir)
                _effect = _container.ResolveId<GameObject>(EffectType.CandyBombAir);
            else
            {
                DebugX.LogError("별 사탕 폭발 이펙트를 지정해야합니다.");
                Destroy(gameObject);
                return;
            }

            //짧은 순간 폭발 범위를 처리 합니다.
            explosionRange.enabled = true;

            //폭발 이펙트를 생성합니다.
            Instantiate(_effect, transform.position, Quaternion.identity);

            //100밀리 세컨드가 지나면 삭제하도록 합니다.
            //explosionRange를 통해 콜라이더에 닿으면 데미지를 주기 위함.
            await UniTask.Delay(TimeSpan.FromMilliseconds(100),
                cancellationToken: this.GetCancellationTokenOnDestroy());

            //삭제
            Destroy(gameObject);

            //  @별사탕 폭발 사운드
            Manager.Get<AudioManager>().PlayOneShot(Settings.SFXClips[1], transform.position);
        }

        /// <summary>
        /// 피벗의 위치를 변경합니다.
        /// </summary>
        /// <param name="pivotStyle"></param>
        public void ChangePivot(PivotStyle pivotStyle)
        {
            if (pivotStyle == PivotStyle.Center)
                _gfx.localPosition = new Vector3(0, -0.206f, 0);
            else
                _gfx.localPosition = new Vector3(0, 0, 0);
        }

        /// <summary>
        /// 뒤로 날려갑니다.
        /// </summary>
        public void KnockBack(Transform target, float knockBackPower, ForceMode mode)
        {
            Vector3 direction = (transform.position - target.position).normalized;
            _throwable.AddForce((new Vector3(0, 0.2f, 0) + direction) * knockBackPower, mode);

            //  @별사탕 피격 사운드
            Manager.Get<AudioManager>().PlayOneShot(Settings.SFXClips[2], transform.position);
        }

        /// <summary>
        /// 물리 기능을 억제합니다.
        /// </summary>
        public void OnTriggerNoPhysics()
        {
            _throwable.OnNoTriggerPhysics();
        }

        /// <summary>
        /// 물리 기능을 적용합니다.
        /// </summary>
        public void OnTriggerPhysics(bool useGravity = true, float mass = 10000000f, bool isTrigger = false)
        {
            _throwable.OnTriggerPhysics(useGravity, mass, isTrigger);
        }

        /// <summary>
        /// Throwable을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Throwable GetThrowable()
        {
            return _throwable;
        }

        /// <summary>
        /// 땅에 붙도록 처리합니다.
        /// </summary>
        public void FloorSnap()
        {
            Transform tr = transform;

            Vector3 nextPosition = tr.position;
            nextPosition.y -= 0.3f;
            tr.position = nextPosition;
        }

        /// <summary>
        /// 무언가에 닿으면 터지도록 트리거합니다.
        /// </summary>
        public void OnTriggerEnterExplosion()
        {
            _touchExplosion = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            try
            {
                //터치 폭발이 false이면 폭발하지 않습니다.
                if (!_touchExplosion) return;

                //Explode함수가 1번이라도 동작했으면 Enter되어도 폭발하지 않습니다.  
                if (explosionRange.enabled) return;

                // WolfBoss인가? 체크
                // 그러면 IsGroggy? 면 터뜨리고, 아니면 튕겨내고 몇 초 뒤에 공중폭발 (Throwable.Throw)
                {
                    var wolfBoss = other.GetComponent<WolfBoss.WolfBoss>();
                    if (wolfBoss != null)
                    {
                        // 폭발할 수 있는 상태가 아니면 폭발하지 않음
                        if (!wolfBoss.CanBombExplode(this))
                        {
                            return;
                        }
                    }
                }


                Explode(other.gameObject.layer == LayerMask.NameToLayer("Ground")
                    ? EffectType.CandyBombGround
                    : EffectType.CandyBombAir).Forget();
            }
            catch (Exception)
            {
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (debugMode)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, Settings.AttackRange);
            }
        }
#endif
    }
}