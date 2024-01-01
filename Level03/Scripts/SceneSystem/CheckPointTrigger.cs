using Managers;
using ManagerX;
using UnityEditor;
using UnityEngine;

namespace SceneSystem
{
    public class CheckPointTrigger : MonoBehaviour
    {
        [field: SerializeField] public CheckPointLocation Location { get; set; }

        public void Save()
        {
            AutoManager.Get<CheckpointManager>().CheckPoint
                = new CheckPoint(Location, GameManager.Instance.CurrentCheckPointStorage);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Location.Position = Handles.PositionHandle(Location.Position, Quaternion.identity);
        }
#endif
    }
}