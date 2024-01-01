using Character.View;
using EnumData;
using Managers;
using UnityEngine;

namespace Effect
{
    public class DestinationVisualizer : MonoBehaviour
    {
        // [SerializeField] private ParticleSystem _marker;
        [SerializeField] private EffectType _effect;
        public float Duration = 1f;

        private Transform _player;
        private float _showTime;

        private void Awake()
        {
            _player = FindObjectOfType<PlayerView>().transform;
        }

        private void Update()
        {
            if (_showTime > 0)
            {
                _showTime -= Time.deltaTime;
                // _marker.gameObject.SetActive(true);
            }
            else
            {
                _showTime = 0;
                // _marker.gameObject.SetActive(false);
            }

            if (Vector3.Distance(_player.transform.position, transform.position) < 0.5f)
                _showTime = 0;
        }

        /// <summary>
        /// 도착 비주얼라이저를 보이게합니다.
        /// </summary>
        public void Show(Vector3 position)
        {
            if(!isActiveAndEnabled) return;
            transform.position = position;
            _showTime = Duration;

            var effect = EffectManager.Instance.Get(_effect);
            effect.transform.position = position;
            // if (_marker.gameObject.activeInHierarchy)
            // {
            // _marker.Play();
            // }
        }
    }
}