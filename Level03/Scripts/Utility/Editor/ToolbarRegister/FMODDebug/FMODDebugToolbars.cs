using System.Reflection;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace NKStudio
{
    public class FMODDebugToolbars
    {
        public static void OnToolbarGUI()
        {
            // 왼쪽 마진을 3정도 적용함.
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            EditorGUILayout.LabelField("FMOD Debug", GUILayout.Width(80));
            bool currentFMODDebugEnable = GetDebugOverlay() == TriStateBool.Enabled ? true : false;
            bool FMODDebugEnableToggle = EditorGUILayout.Toggle(currentFMODDebugEnable, GUILayout.Width(16));
            TriStateBool nextFMODDebugOverlay = FMODDebugEnableToggle ? TriStateBool.Enabled : TriStateBool.Disabled;
            SetDebugOverlay(nextFMODDebugOverlay);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Make sure to save the changes.
            if (GUI.changed)
            {
                EditorUtility.SetDirty(FMODUnity.Settings.Instance);
                AssetDatabase.SaveAssets();
            }
        }

        private static void SetDebugOverlay(TriStateBool value)
        {
            // Lastly, access the Overlay property.
            var overlayProperty = FMODUnity.Settings.Instance.PlayInEditorPlatform.GetType().GetField("Properties",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var overlayValue = overlayProperty.GetValue(FMODUnity.Settings.Instance.PlayInEditorPlatform);

            var OverlayProperty = overlayValue.GetType().GetField("Overlay",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var OverlayValue = OverlayProperty.GetValue(overlayValue);

            var ValueProperty = OverlayValue.GetType().GetField("Value",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            ValueProperty.SetValue(OverlayValue, value);
        }

        private static TriStateBool GetDebugOverlay()
        {
            return FMODUnity.Settings.Instance.PlayInEditorPlatform.Overlay;
        }
    }
}