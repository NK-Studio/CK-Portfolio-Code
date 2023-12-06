using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEditor;
using UnityEngine;


namespace Platform_Experimental_
{
    public class SavePoint : MonoBehaviour
    {
        private enum EnterShape
        {
            Box,
            Capsule
        }

        [field: SerializeField, InlineButton("SetName", "Name")]
        public int Index { get; private set; }

        [field: SerializeField, HideInInspector]
        private Transform spawnPoint;

        [field: SerializeField, HideInInspector]
        private Transform enterPoint;

        [field: SerializeField, HideInInspector]
        private Mesh playerMesh;

        [SerializeField, ReadOnly]
        private SpawnPointManager spawnPointManager;

        private const string SpawnPointName = "SpawnPoint";
        
        //런타임에서는 사용하지 않기 때문에 경고를 띄우지 않음
#pragma warning disable CS0414
        private const string MeshPath = "Assets/GameResources/Player/Meshes/MainPlayer.fbx";
#pragma warning restore CS0414
        
#if UNITY_EDITOR
        [Tooltip("플레이어 스폰 위치를 수정할 때 사용합니다.")] [SerializeField]
        private bool debugMode;

        [ShowIf("debugMode")] [SerializeField] private Color debugColor = Color.green;

        [SerializeField] [OnValueChanged("ChangeEnterArea")]
        private EnterShape enterShape;
#endif

        private void Reset()
        {
            Init();
        }

        private void Start()
        {
            enterPoint.OnTriggerEnterAsObservable()
                .Where(coll => coll.CompareTag("Player"))
                .Subscribe(coll =>
                {
#if UNITY_EDITOR
                    if (debugMode)
                        DebugX.Log(gameObject.name);
#endif
                    
                    spawnPointManager.SetIndex(Index);
                    //coll.transform.position = _enterPoint.position;
                })
                .AddTo(this);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void ChangeEnterArea()
        {
            if (enterShape == EnterShape.Capsule)
            {
                var capsule = enterPoint.gameObject.AddComponent<CapsuleCollider>();
                capsule.isTrigger = true;
                capsule.direction = 1;

                BoxCollider[] allBoxColliders = enterPoint.GetComponents<BoxCollider>();
                foreach (BoxCollider allBoxCollider in allBoxColliders)
                    DestroyImmediate(allBoxCollider);
            }
            else if (enterShape == EnterShape.Box)
            {
                enterPoint.gameObject.AddComponent<BoxCollider>().isTrigger = true;
                CapsuleCollider[] allCapsuleColliders = enterPoint.GetComponents<CapsuleCollider>();
                foreach (CapsuleCollider allCapsuleCollider in allCapsuleColliders)
                    DestroyImmediate(allCapsuleCollider);
            }
        }
#endif

        [UsedImplicitly]
        private void SetName()
        {
            gameObject.name = $"{SpawnPointName}-{Index}";
        }

        [Button("Init")]
        private void Init()
        {
            if (!spawnPoint)
                spawnPoint = transform.GetChild(0);

            if (!enterPoint)
                enterPoint = transform.GetChild(1);

            if (!spawnPointManager)
                spawnPointManager = GetComponentInParent<SpawnPointManager>();

            SavePoint[] worldSavePoints = FindObjectsOfType<SavePoint>();
            Index = worldSavePoints.Length;

            gameObject.name = $"{SpawnPointName}-{Index}";

#if UNITY_EDITOR
            if (!playerMesh)
                playerMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
#endif
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (debugMode)
            {
                Gizmos.color = debugColor;

                if (playerMesh)
                {
                    Gizmos.DrawMesh(playerMesh, spawnPoint.position,
                        spawnPoint.rotation * Quaternion.Euler(new Vector3(-90, 0, 0)));
                }
            }
        }
#endif
        
        /// <summary>
        /// 스폰 트랜스폼을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Transform GetSpawnPoint()
        {
            return spawnPoint;
        }
    }
}