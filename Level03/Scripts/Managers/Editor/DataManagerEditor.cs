using System.Collections.Generic;
using System.IO;
// using MessagePack;
using UnityEditor;
using UnityEngine;


public class DataManagerEditor : Editor
{
    private const string GameName = "Akari";
    
    [MenuItem("Tools/DataManager/ShowAllKey")]
    private static void ShowAllKeyConsole()
    {
        // string path = $"{Application.persistentDataPath}/{GameName}.bin";
        // if (!File.Exists(path)) return;

        // byte[] bin = File.ReadAllBytes(path);
            
        // var gameData = MessagePackSerializer.Deserialize<Dictionary<string, byte[]>>(bin);
            
        // foreach (KeyValuePair<string, byte[]> data in gameData)
            // Debug.Log(data.Key);
    }
    
    [MenuItem("Tools/DataManager/OpenSaveDataPath")]
    private static void OpenSaveDataPath()
    {
        string path = $"{Application.persistentDataPath}";

        Application.OpenURL(path);
    }
}
