using Character.Presenter;
using Managers;
using UnityEngine;
using UnityEngine.AI;
using Logger = NKStudio.Logger;

namespace Level
{
    public interface IItem
    {
        public string name { get; }
        public Transform transform { get; }
        public GameObject gameObject { get; }
        public bool isActiveAndEnabled { get; }
        public int GetInstanceID();

        public void Initialize(Vector3 spawnPosition);
        public bool CanBeNearestItem(PlayerPresenter player);
        public void OnStartNearestItem();
        public void OnEndNearestItem();
        /// <summary>
        /// 플레이어가 상호작용 시 호출됩니다.
        /// </summary>
        /// <param name="player">호출한 플레이어 객체입니다.</param>
        public void Interact(PlayerPresenter player);
        
        
        public static Vector3 GetPlayerAvailablePosition(Vector3 position)
        {
            
            var player = GameManager.Instance.Player;
            if (!NavMesh.SamplePosition(position, out var hit, (player.transform.position - position).magnitude, player.Behaviour.NavMeshAreaMask)) {
                Logger.LogWarning($"{position}에서 아이템 생성 실패! 플레이어 위치로 설정");
                return player.transform.position;
            }

            return hit.position;
        }
    }
}