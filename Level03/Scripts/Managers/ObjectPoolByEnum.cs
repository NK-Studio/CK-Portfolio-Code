using System;
using System.Collections.Generic;
using ManagerX;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Pool;

namespace Managers
{
    public abstract class ObjectPoolByEnum<TEnum> : MonoBehaviour, AutoManager
    {
        [Serializable]
        public struct PrefabSettings
        {
            [LabelWidth(60f)]
            public TEnum Type;
            [LabelWidth(60f)]
            public GameObject Prefab;
            [HorizontalGroup]
            public int Capacity;
            [HorizontalGroup]
            public int Max;

            public PrefabSettings(TEnum type, GameObject prefab, int capacity = 10, int max = 10000)
            {
                Type = type;
                Prefab = prefab;
                Capacity = capacity;
                Max = max;
            }
        }

        [Serializable]
        public struct ParentSettings
        {
            public TEnum Type;
            public Transform Parent;
        }

        [SerializeField, ListDrawerSettings(NumberOfItemsPerPage = 999), Searchable]
        private List<PrefabSettings> _prefabSettings = new();
        [SerializeField]
        private List<ParentSettings> _parentSettings = new();

        public Dictionary<TEnum, PrefabSettings> PrefabSettingsMap { get; } = new();

        public virtual GameObject Get(TEnum type)
        {
            var settings = PrefabSettingsMap[type];
            return Get(type, settings.Prefab.transform.position, settings.Prefab.transform.rotation);
        }
        public virtual GameObject Get(TEnum type, Vector3 position, Quaternion rotation, bool active = true)
        {
            var obj = _objectPoolByEnum[type].Get();
            var id = obj.GetInstanceID();
            UsedObjects.TryAdd(id, new PoppedGameObject { Type = type, Value = obj }); // Remove 이벤트는 Supplier에서
            // Prefab 최초 position & rotation으로 초기화
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(active);
            return obj;
        }

        private Dictionary<TEnum, Transform> _parentByEnum = new();
        private Transform GetOrCreateParent(TEnum type)
        {
            if (_parentByEnum.TryGetValue(type, out var parent))
            {
                return parent;
            }

            var obj = new GameObject($"{type}_Pool");
            parent = obj.transform;
            parent.SetParent(transform);
            _parentSettings.Add(new ParentSettings { Type = type, Parent = parent });
            _parentByEnum.Add(type, parent);

            return parent;
        }

        [Button]
        private void ResetParents()
        {
            _parentByEnum.Clear();
            foreach (var setting in _parentSettings)
            {
                DestroyImmediate(setting.Parent.gameObject);
            }
            _parentSettings.Clear();
            var t = transform;
            foreach (var ct in t.GetComponentsInChildren<Transform>())
            {
                if(ct == t) continue;
                DestroyImmediate(ct.gameObject);
            }

            foreach (var setting in _prefabSettings)
            {
                GetOrCreateParent(setting.Type);
            }
        }
        
        private Dictionary<TEnum, ObjectPool<GameObject>> _objectPoolByEnum = new();

        private Dictionary<TEnum, int> _objectCounts = new();
        protected virtual void Start()
        {
            PrefabSettingsMap.Clear();
            foreach (var setting in _prefabSettings)
            {
                PrefabSettingsMap.Add(setting.Type, setting);
            }
            _parentByEnum.Clear();
            foreach (var setting in _parentSettings)
            {
                _parentByEnum.Add(setting.Type, setting.Parent);
            }
            
            var createdObjects = new List<GameObject>();
            foreach (var (type, settings) in PrefabSettingsMap)
            {
                // Hierarchy 관리용 Parent 
                var parent = GetOrCreateParent(type);
                
                // 풀 생성
                var pool = new ObjectPool<GameObject>(
                    () => Supplier(type, parent, settings.Prefab),
                    it => {},
                    it => it.SetActive(false),
                    Destroy,
                    false,
                    settings.Capacity,
                    settings.Max
                );
                
                // 풀 등록
                _objectPoolByEnum.Add(type, pool);

                // 초기 Capacity만큼 생성시키기
                createdObjects.Clear();
                createdObjects.Capacity = Mathf.Max(createdObjects.Capacity, settings.Capacity);
                for (int i = 0; i < settings.Capacity; i++)
                {
                    var obj = pool.Get();
                    createdObjects.Add(obj);
                }
                foreach (var obj in createdObjects)
                {
                    pool.Release(obj);
                    obj.SetActive(false);
                }
                createdObjects.Clear();
            }
        }

        protected virtual GameObject Supplier(TEnum type, Transform parent, GameObject prefab)
        {
            // 오브젝트 생성
            GameObject obj;
#if UNITY_EDITOR
            // 에디터의 경우 예외처리
            try
            {
                obj = Instantiate(prefab, parent);
            }
            catch (Exception)
            {
                Debug.LogWarning($"ObjectPool {name}: Failed to instantiate {prefab}({type.ToString()})", prefab);
                return null;
            }
#else
            // 단순 생성
            obj = Instantiate(prefab, parent);
#endif
            
            obj.SetActive(false);
            
            // 오브젝트 Type별 갯수 카운트 - 이름에 반영
            if (!_objectCounts.TryGetValue(type, out var count))
            {
                _objectCounts.Add(type, 1);
                count = 1;
            }
            else
            {
                _objectCounts[type] = ++count;
            }
            obj.name = prefab.name + $"_{count:000}";
            
            // 풀 등록
            var pool = _objectPoolByEnum[type];
            
            // Disable 되면 Release
            obj.OnDisableAsObservable()
                .Subscribe(_ => pool.Release(obj))
                .AddTo(obj);
            return obj;
        }
        
        
        
        protected struct PoppedGameObject
        {
            public TEnum Type;
            public GameObject Value;
        }
        protected Dictionary<int, PoppedGameObject> UsedObjects = new();

        /// <summary>
        /// 기존 사용했던 오브젝트들을 정리합니다. 씬 로딩 시 주로 사용됩니다.
        /// </summary>
        public virtual void ReleaseUsedObjects()
        {
            foreach (var (_, obj) in UsedObjects)
            {
                if (!obj.Value || !obj.Value.gameObject.activeSelf)
                {
                    continue;
                }
                obj.Value.gameObject.SetActive(false);
            }
            UsedObjects.Clear();
        }
        
    }
}