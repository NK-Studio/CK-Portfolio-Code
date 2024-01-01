using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.Universal;

namespace Effect
{
    public class DecalEffect : MonoBehaviour
    {
        [field: SerializeField]
        public DecalProjector Projector { get; private set; }
        
        // Progress 변경 시 사용할 프로퍼티 및 캐시 ID
        public string ProgressProperty = "_Inner_Circle_Offset";
        private int _progressPropertyID;
        
        // Decal은 Material Instancing을 지원하지 않아 직접 구현 ㅋㅋ 
        // private static readonly Dictionary<int, ObjectPool<Material>> MaterialPoolById = new();
        // 현재 Material의 인스턴스 풀
        // private ObjectPool<Material> _pool;
        // Projector에 박힌 Material
        private Material _sharedMaterial;
        // 풀링으로 생성된 Material
        private Material _instantiatedMaterial;
        
        private void Awake()
        {
            if(!Projector)
                Projector = GetComponent<DecalProjector>();

            // 최초 등록된 Material의 InstanceID 기반으로 ObjectPool 구성
            if (!_sharedMaterial)
            {
                _sharedMaterial = Projector.material;
            }
            
            /*
            var originalMaterial = _sharedMaterial;
            var id = originalMaterial.GetInstanceID();
            if (!MaterialPoolById.TryGetValue(id, out _pool))
            {
                MaterialPoolById.Add(id, _pool = new ObjectPool<Material>(
                    createFunc: () => new Material(originalMaterial),
                    actionOnDestroy: Destroy,
                    collectionCheck: false
                ));
                const int poolSize = 10;
                var preInitialize = new Material[poolSize];
                for (int i = 0; i < poolSize; i++)
                    preInitialize[i] = _pool.Get();
                for (int i = 0; i < poolSize; i++)
                    _pool.Release(preInitialize[i]);
            }
            */
            _instantiatedMaterial = new Material(_sharedMaterial);
            Projector.material = _instantiatedMaterial;
            _progressPropertyID = Shader.PropertyToID(ProgressProperty);
        }

        private void OnEnable()
        {
            // _instantiatedMaterial = _pool.Get();
            // Projector.material = _instantiatedMaterial;
            Progress = _progress;
        }

        public float Radius
        {
            get => Projector.size.x * 2f;
            set => Projector.size = new Vector3(value * 2f, value * 2f, Projector.size.z);
        }

        private float _progress;
        public float Progress
        {
            get => _progress;
            set
            {
                if (_instantiatedMaterial)
                {
                    _instantiatedMaterial.SetFloat(_progressPropertyID, value);
                }
                _progress = value;
            }
        }

        public float Opacity
        {
            get => Projector.fadeFactor;
            set => Projector.fadeFactor = Mathf.Clamp01(value);
        }

        /*
        private void OnDisable()
        {
            if (_instantiatedMaterial)
            {
                _pool.Release(_instantiatedMaterial);
                _instantiatedMaterial = null;
            }
        }
        */

        private void OnDestroy()
        {
            if (_instantiatedMaterial)
            {
                Destroy(_instantiatedMaterial);
                _instantiatedMaterial = null;
            }
        }
    }
}