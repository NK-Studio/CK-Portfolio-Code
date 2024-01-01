#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Utility
{
    public class UnPackage : MonoBehaviour
    {
        [Button]
        public void UnPack()
        {
            //자신의 자식들을 가져와서 리스트에 담습니다
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in transform) 
                children.Add(child.gameObject);
            
            //자신을 프리팹을 언팩합니다.
            PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

            foreach (GameObject child in children) 
                child.transform.SetParent(null);
            
            DestroyImmediate(gameObject);
        }
    }
}
#endif