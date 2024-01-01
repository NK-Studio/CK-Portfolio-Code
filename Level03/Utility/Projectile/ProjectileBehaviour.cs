using System;
using UnityEngine;

namespace Utility.Projectile
{
    public abstract class ProjectileBehaviour : MonoBehaviour
    {
        [SerializeField] protected Transform explosionFX;

        protected bool exploded;

        public Action<Collision> OnExplode;

        public Transform Target { get; set; }
        public Vector3 TargetPoint { get; set; }

        public virtual Transform ExplosionFX
        {
            get => explosionFX;
            set => explosionFX = value;
        }
        
        protected abstract void OnLaunch();

        protected virtual void Fly()
        {
            if (exploded)
                return;

            if (Target != null)
                TargetPoint = Target.position;
        }

        protected virtual void Explosion(Collision collision)
        {
            OnExplode?.Invoke(collision);

            if (explosionFX != null)
                Instantiate(explosionFX, transform.position, Quaternion.identity);
        }

        public void Launch(Transform target)
        {
            exploded = false;
            Target = target;
            TargetPoint = target.position;
            OnLaunch();
        }

        public void Launch(Vector3 targetPoint)
        {
            exploded = false;
            Target = null;
            TargetPoint = targetPoint;
            OnLaunch();
        }
        
        private void Update()
        {
            Fly();
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (exploded)
                return;

            exploded = true;
            Explosion(collision);
          
            OnExplode = null;
            Destroy(gameObject);
        }
    }
}