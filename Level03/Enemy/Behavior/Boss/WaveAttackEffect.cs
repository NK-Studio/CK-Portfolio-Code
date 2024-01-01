using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    [RequireComponent(typeof(ParticleSystem))]
    public class WaveAttackEffect : MonoBehaviour
    {
        private ParticleSystem _self;
        [SerializeField]
        private List<ParticleSystem> _particle = new();
        [SerializeField]
        private float _sizeFactor = 7f;

        private void Awake()
        {
            _self = GetComponent<ParticleSystem>();
        }

        [Button("Set Effect Width")]
        public void SetWidth(float width)
        {
            var sizeX = width / _sizeFactor;
            var sizeXCurve = new ParticleSystem.MinMaxCurve(sizeX);
            foreach (var ps in _particle)
            {
                var main = ps.main;
                main.startSizeX = sizeXCurve;
            }

            if (!_self) _self = GetComponent<ParticleSystem>();
            if(_self.isPlaying)
                _self.Stop();
            _self.Play(true);
            
        }

        [SerializeField]
        private GameObject _follow;
        public void SetFollowTarget(GameObject target)
        {
            _follow = target;
        }

        private void LateUpdate()
        {
            // 타겟 cube가 디지면 나도 디짐 
            if (!_follow || !_follow.activeSelf)
            {
                _follow = null;
                gameObject.SetActive(false);
                return;
            }
            transform.position = _follow.transform.position;
        }
    }
}