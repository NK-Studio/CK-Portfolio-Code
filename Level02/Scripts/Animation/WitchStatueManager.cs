using System;
using AutoManager;
using Enemys.WolfBoss;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Zenject;

namespace Animation
{
    public class WitchStatueManager : MonoBehaviour
    {
        [Serializable]
        public struct TransformData
        {
            public Vector3 position;
            public Quaternion rotation;

            public TransformData(in Transform from)
            {
                position = from.position;
                rotation = from.rotation;
            }

            public void Apply(ref Transform to)
            {
                to.position = position;
                to.rotation = rotation;
            }
        }

        [Inject] private DiContainer _container;
        public WitchStatue StatuePrefab;

        public WitchStatue[] PlacedStatues;

        public WolfBoss WolfBoss;

        [Button("Auto Binding", ButtonSizes.Large)]
        private void AutoBinding()
        {
            PlacedStatues = FindObjectsOfType<WitchStatue>();
        }

        private void Start()
        {
            Manager.Get<AudioManager>().NoStopBGM = false;

            for (int i = 0; i < PlacedStatues.Length; i++)
            {
                if (!PlacedStatues[i]) continue;

                BindStatue(PlacedStatues[i], i);
            }
        }

        private void BindStatue(WitchStatue statue, int index)
        {
            statue.OnDestroyAsObservable()
                .Where(_ => !_isQuiting)
                .Subscribe(_ => CreateStatue(statue, index))
                .AddTo(this);
        }

        private bool _isQuiting;

        private void OnApplicationQuit()
        {
            _isQuiting = true;
        }

        private void OnDestroy()
        {
            _isQuiting = true;
        }

        private void CreateStatue(WitchStatue original, int index)
        {
            if (_isQuiting) return;

            if (WolfBoss.HP == 0)
                return;

            try
            {
                Transform t = original.transform;
                WitchStatue newStatue =
                    _container.InstantiatePrefabForComponent<WitchStatue>(StatuePrefab.gameObject, t.position,
                        t.rotation, null);
                PlacedStatues[index] = newStatue;
                BindStatue(newStatue, index);
            }
            catch (Exception e)
            {
                DebugX.LogWarning(e);
            }
        }
    }
}