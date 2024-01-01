using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NKStudio
{
    public class SetPositionAsChildrenCenter
    {
        [MenuItem("GameObject/Set Position As Children Center")]
        private static void Execute()
        {
            var obj = Selection.activeGameObject;
            var transform = obj.transform;
            var childCount = transform.childCount;
            if (childCount <= 0)
            {
                Debug.Log($"SetPositionAsChildrenCenter - No Children on {obj}", obj);
                return;
            }

            var children = new List<Transform>(childCount);
            var mean = Vector3.zero;
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                mean += child.transform.position;
                children.Add(child);
            }

            mean *= (1f / childCount);
            
            var rollBackVector = mean - transform.position;
            transform.position = mean;
            foreach (var child in children)
            {
                child.transform.position -= rollBackVector;
            }
            
            // EditorUtility.SetDirty(obj);

        }
    }
}