using System;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Platform
{
    public class DestructibleObject : MonoBehaviour
    {
        [Title("매쉬")] public GameObject Origin;
        public GameObject Crack;

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void Start()
        {
            HaveNotChild()
                .Subscribe(_ => Destroy(gameObject))
                .AddTo(this);
        }

        private IObservable<Unit> HaveNotChild() => this.UpdateAsObservable()
            .Where(_ => Crack.transform.childCount == 0);

        public void Play()
        {
            if (_collider)
                _collider.enabled = false;

            if (Origin)
                Origin.SetActive(false);

            if (Crack)
                Crack.SetActive(true);
        }
    }
}