#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Utility;

namespace Settings
{
    public class PlayerSpawner : Editor
    {
        [MenuItem("Tools/Setting Scene/Create Player")]
        [MenuItem("GameObject/3D Object/Create Player")]
        private static void SpawnPlayer()
        {
            GameObject package =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Misc/Prefabs/Package.prefab");

            GameObject packageObject = PrefabUtility.InstantiatePrefab(package, null) as GameObject;
            EditorUtility.SetDirty(packageObject);
            packageObject.GetComponent<UnPackage>().UnPack();
        }
    }
}
#endif