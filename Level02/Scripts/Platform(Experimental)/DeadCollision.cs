using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Platform_Experimental_
{
    public class DeadCollision : MonoBehaviour
    {
        private enum EnterShape
        {
            Cube,
            Sphere,
            Capsule,
        }

        [InfoBox("플레이어가 이 충돌체에 닿으면 죽습니다.")] public UnityEvent killEvent;

        [OnValueChanged("OnChangeEnterShape")] [SerializeField]
        private EnterShape enterShape;

        [ValidateInput("@this.childCollider != null", "콜라이더가 비어있습니다.\nAuto 버튼을 눌러주세요."),
         InlineButton("OnChangeEnterShape", "Auto")]
        [SerializeField, DisableInPlayMode]
        private Collider childCollider;

        [Title("Debug Mode"),
         InfoBox("Capsule Collider는 지원하지 않습니다.", InfoMessageType.Warning, "@this.enterShape == EnterShape.Capsule")]
        [SerializeField]
        private bool debugMode;
        
        private void Start()
        {
            if (!Application.isPlaying) return;

            if (!childCollider) OnChangeEnterShape();

            childCollider
                .OnTriggerEnterAsObservable()
                .Where(coll => coll.CompareTag("Player"))
                .Subscribe(_ => killEvent?.Invoke())
                .AddTo(this);
        }

        [UsedImplicitly]
        private void OnChangeEnterShape()
        {
            Transform colliderChild = transform.GetChild(0);
            Assert.IsTrue(colliderChild, "자식 오브젝트를 찾을 수 없습니다.");

            Collider[] colliders = colliderChild.GetComponents<Collider>();

            foreach (Collider coll in colliders)
                DestroyImmediate(coll);

            Collider newCollider = null;

            switch (enterShape)
            {
                case EnterShape.Cube:
                    newCollider = colliderChild.gameObject.AddComponent<BoxCollider>();
                    break;
                case EnterShape.Sphere:
                    newCollider = colliderChild.gameObject.AddComponent<SphereCollider>();
                    break;
                case EnterShape.Capsule:
                    newCollider = colliderChild.gameObject.AddComponent<CapsuleCollider>();
                    break;
            }

            if (newCollider != null)
            {
                newCollider.isTrigger = true;
                childCollider = newCollider;
            }
        }

        [ContextMenu("콜라이더 바인딩", false, 0)]
        private void Init()
        {
            Transform colliderChild = transform.GetChild(0);
            Assert.IsTrue(colliderChild, "자식 오브젝트를 찾을 수 없습니다.");
            childCollider = colliderChild.GetComponent<Collider>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!debugMode) return;

            Transform colliderChild = transform.GetChild(0);
            Gizmos.color = Color.red;

            Type type = colliderChild.GetComponent<Collider>().GetType();

            switch (type)
            {
                case var _ when type == typeof(BoxCollider):
                    BoxCollider box = colliderChild.GetComponent<BoxCollider>();
                    Gizmos.matrix = Matrix4x4.TRS(colliderChild.TransformPoint(box.center), colliderChild.rotation, colliderChild.lossyScale);
                    Gizmos.DrawWireCube(Vector3.zero, box.size);
                    break;
                case var _ when type == typeof(SphereCollider):
                    SphereCollider sphere = colliderChild.GetComponent<SphereCollider>();
                    Gizmos.DrawWireSphere(colliderChild.position, sphere.radius);
                    break;
            }
        }
#endif
    }
}