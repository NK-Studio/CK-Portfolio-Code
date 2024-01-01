using Enemy.Behavior;
using EnumData;
using Managers;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Spawner
{
    public class EnemySpawner : MonoBehaviour
    {
        public virtual Monster Spawn(EnemyType type, float delay)
        {
            GameObject monsterObject = EnemyPoolManager.Instance.Get(type);
            if (monsterObject.TryGetComponent(out NavMeshAgent agent))
            {
                agent.Warp(transform.position);
            }
            else
            {
                monsterObject.transform.position = transform.position;
            }

            return monsterObject.GetComponent<Monster>();
        }

        public virtual void DrawGizmosOnValid()
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}