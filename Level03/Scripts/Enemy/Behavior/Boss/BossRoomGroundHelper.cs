using System.Collections.Generic;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    public class BossRoomGroundHelper : MonoBehaviour
    {
        [Button]
        private void SortChildrenHierarchyByAxis(Axis axis, bool inverted)
        {
            // 자식 오브젝트를 가져옵니다.
            int childCount = transform.childCount;
            List<Transform> children = new List<Transform>(childCount);

            for (int i = 0; i < childCount; i++)
            {
                children.Add(transform.GetChild(i));
            }

            // 정렬 기준을 선택합니다.
            switch (axis)
            {
                case Axis.X:
                    children.Sort((a, b) => a.position.x.CompareTo(b.position.x));
                    break;
                case Axis.Y:
                    children.Sort((a, b) => a.position.y.CompareTo(b.position.y));
                    break;
                case Axis.Z:
                    children.Sort((a, b) => a.position.z.CompareTo(b.position.z));
                    break;
            }

            // inverted 플래그에 따라 역순으로 정렬합니다.
            if (inverted)
            {
                children.Reverse();
            }

            // Hierarchy 상의 순서를 변경합니다.
            for (int i = 0; i < childCount; i++)
            {
                children[i].SetSiblingIndex(i);
            }
        }
    }
}