using ManagerX;
using SceneSystem;
using UnityEngine;

namespace Managers
{
    [ManagerDefaultPrefab("CheckPointManager")]
    public class CheckpointManager : MonoBehaviour, AutoManager
    {
        public static CheckpointManager Instance => AutoManager.Get<CheckpointManager>();
        
        [SerializeField] private bool showDebug;
        
        [SerializeField] private CheckPoint checkPoint;
        
        public CheckPoint Default;
        
        public CheckPoint CheckPoint
        {
            get => checkPoint;
            set
            {
                checkPoint = value;
                if (showDebug)
                    DebugX.Log($"Saved at {value}");
            }
        }
        
        public void Reset()
        {
            CheckPoint = Default;
        }
    }
}