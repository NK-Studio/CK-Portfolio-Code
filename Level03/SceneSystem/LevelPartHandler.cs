using System.Collections.Generic;
using Character.Presenter;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SceneSystem
{
    /// <summary>
    /// 현재 씬의 부분 레벨 관리자
    /// </summary>
    public class LevelPartHandler : MonoBehaviour
    {
        [field: SerializeField]
        public List<LevelPartArea> Areas { get; private set; } = new();

        private PlayerPresenter _player;
        private void Start()
        {
            _player = GameManager.Instance.Player;

            GameManager.Instance.CurrentLevelPartHandler = this;
            
            foreach (var area in Areas)
            {
                area.Initialize(_player);
            }
        }

        [Button("씬 내 LevelPartArea 바인드")]
        private void AutoBindAreas()
        {
            Areas.Clear();
            Areas.AddRange(FindObjectsOfType<LevelPartArea>());
        }

        public LevelPartArea GetNearestLevelPartArea(Vector3 position)
        {
            float Distance(LevelPartArea area) => Vector3.Distance(area.transform.position, position);

            var areas = Areas;

            if (areas.Count <= 0)
            {
                return null;
            }
            
            var nearest = areas[0];
            var nearestDistance = Distance(nearest);

            for (int i = 1; i < areas.Count; i++)
            {
                var distance = Distance(areas[i]);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = areas[i];
                }
            }
            
            return nearest;
        }
        
    }
}