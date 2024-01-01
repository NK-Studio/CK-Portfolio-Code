using System;
using System.Collections.Generic;
using Character.Model;
using Enemy;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SceneSystem
{
    /// <summary>
    /// 체크포인트 추가 저장 요소를 관리하는 클래스
    /// </summary>
    [Serializable]
    public class CheckPointStorage : ISerializationCallbackReceiver
    {
        public PlayerStatus Status = new();
        /// <summary>
        /// 클리어한 BattleArea Key 목록
        /// </summary>
        public HashSet<string> ClearedBattleAreaKeys { get; private set; } = new();

        // TODO 상호작용된 요소 (체크포인트에 저장되어야할) 추가
        // TODO 신당 추가

        public CheckPointStorage()
        {
        }

        public CheckPointStorage(CheckPointStorage from)
        {
            Copy(from);
        }

        public void Copy(CheckPointStorage from)
        {
            if (from == null) return;

            ClearedBattleAreaKeys.Clear();
            foreach (var key in from.ClearedBattleAreaKeys)
            {
                ClearedBattleAreaKeys.Add(key);
            }
        }

        public void AddBattleArea(string key)
        {
            ClearedBattleAreaKeys.Add(key);
#if UNITY_EDITOR
            OnAfterDeserialize();
#endif
        }

        public bool ContainsBattleArea(BattleArea area)
        {
            return ClearedBattleAreaKeys.Contains(area.Key);
        }

        public override string ToString()
        {
            return $"Health: {Status.Health}, Magazine: {Status.Magazine?.Settings.name ?? "None"}";
        }

        [SerializeField] private List<string> _clearedBattleAreaKeys = new List<string>();

        public void OnBeforeSerialize()
        {
            ClearedBattleAreaKeys.Clear();
            foreach (var key in _clearedBattleAreaKeys)
            {
                ClearedBattleAreaKeys.Add(key);
            }
        }

        public void OnAfterDeserialize()
        {
            _clearedBattleAreaKeys.Clear();
            foreach (var key in ClearedBattleAreaKeys)
            {
                _clearedBattleAreaKeys.Add(key);
            }
        }
    }


    /// <summary>
    /// 체크포인트 위치 정보
    /// </summary>
    [Serializable]
    public class CheckPointLocation
    {
        public SceneReference Scene;
        public bool UsePosition;
        [DisableIf("@UsePosition == false")] public Vector3 Position;

        public bool IsValid() => Scene != null && !string.IsNullOrEmpty(Scene.Name);
    }

    [Serializable]
    public class CheckPoint
    {
        public CheckPointLocation Location;
        public CheckPointStorage Storage;

        public CheckPoint(CheckPointLocation location, CheckPointStorage storage)
        {
            Location = location;
            Storage = new CheckPointStorage(storage);
        }

        public bool IsValid() => !string.IsNullOrEmpty(Location.Scene.Name);

        public override string ToString()
        {
            return $"{Location.Scene}, {Location.Position}, {Storage}";
        }

        public bool IsNull()
        {
            return Location.Scene.IsEmpty;
        }
    }
}