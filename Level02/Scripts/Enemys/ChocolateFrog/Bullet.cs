using UnityEngine;

namespace Enemys
{
    public class Bullet : MonoBehaviour
    {
        [HideInInspector] public float speed = 1;
        [HideInInspector] public float lifeTime = 3f;

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }
        
        private void Update()
        {
            transform.Translate(Vector3.forward * (speed * Time.deltaTime));
        }
    }
}