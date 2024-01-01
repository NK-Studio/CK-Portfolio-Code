using Managers;
using UnityEngine;
using UnityEngine.Pool;

namespace Enemy.UI
{
    public class EnemyHUDPoolManager : MonoBehaviour
    {
        public ObjectPool<EnemyHUD> HUDPool { get; private set; }
        public GameObject EnemyHUD;

        [Tooltip("예상되는 EnemyHUD 개수")] public int Capacity = 10;

        private void Awake()
        {
            HUDPool = new ObjectPool<EnemyHUD>(CreateHUD, OnGetHUD, OnReleaseHUD, OnDestroyHUD, maxSize: Capacity);
            GameManager.Instance.CurrentHUDPoolManager = this;
        }

        private void Start()
        {
        }

        private EnemyHUD CreateHUD()
        {
            EnemyHUD enemyHUD = Instantiate(EnemyHUD).GetComponent<EnemyHUD>();
            enemyHUD.transform.SetParent(transform);
            enemyHUD.SetPool(HUDPool);
            return enemyHUD;
        }

        private void OnGetHUD(EnemyHUD obj)
        {
            obj.gameObject.SetActive(true);
        }

        private void OnReleaseHUD(EnemyHUD obj)
        {
            obj.gameObject.SetActive(false);
        }

        private void OnDestroyHUD(EnemyHUD obj)
        {
            Destroy(obj.gameObject);
        }
    }
}