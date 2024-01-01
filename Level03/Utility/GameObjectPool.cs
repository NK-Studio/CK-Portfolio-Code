using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;

namespace Enemy.UI
{
    public interface IGameObjectPooled<T> where T : Component, IGameObjectPooled<T>
    {
        public GameObjectPool<T> Pool { get; set; }  
    }
    public abstract class GameObjectPool<T> : MonoBehaviour where T : Component, IGameObjectPooled<T>
    {
        protected ObjectPool<T> Pool { get; private set; }
        
        [LabelText("프리팹")]
        public T Target;
        [LabelText("예상 갯수")] 
        public int Capacity = 10;
        [LabelText("오브젝트 생성 부모"), Tooltip("지정되지 않으면 자신으로 설정합니다.")]
        public Transform Root;

        protected virtual void Awake()
        {
            if (!Target)
            {
                Debug.LogError($"GameObjectPool {name}: Target이 없습니다.", gameObject);
                return;
            }
            Pool = new ObjectPool<T>(
                OnObjectCreate, 
                null, 
                OnObjectRelease,
                OnObjectDestroy,
                maxSize: Capacity
            );
            
            // 초기 Capacity만큼 생성시키기
            var createdObjects = new List<T>();
            createdObjects.Capacity = Mathf.Max(createdObjects.Capacity, Capacity);
            for (int i = 0; i < Capacity; i++)
            {
                var obj = Pool.Get();
                createdObjects.Add(obj);
            }
            foreach (var obj in createdObjects)
            {
                Pool.Release(obj);
            }
        }

        public T Get()
        {
            var obj = Pool.Get();
            OnObjectGet(obj);
            return obj;
        }

        public void Release(T obj)
        {
            Pool.Release(obj);
        }

        private int _createCount = 0;
        protected virtual T OnObjectCreate()
        {
            var t = Instantiate(Target, transform);
            var obj = t.gameObject;
            obj.SetActive(false);
            obj.name = $"{Target.name}_{_createCount++:000}";
            t.Pool = this;
            return t;
        }

        protected virtual void OnObjectGet(T obj)
        {
            obj.gameObject.SetActive(true);
        }

        protected virtual void OnObjectRelease(T obj)
        {
            obj.gameObject.SetActive(false);
        }

        protected virtual void OnObjectDestroy(T obj)
        {
            Destroy(obj.gameObject);
        }
    }
}