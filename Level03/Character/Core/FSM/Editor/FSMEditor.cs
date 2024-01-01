using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Character.Core.FSM
{
    [CustomEditor(typeof(FSM<,>), true)]
    public class FSMEditor : UnityEditor.Editor
    {
        private Type _targetType = null;

        private int _statePopupIndex = 0;
        private string _currentStateName = "";


        protected void OnEnable()
        {
            SceneView.duringSceneGui += this.OnSceneGUI;

            _targetType = target.GetType();
            while (_targetType.BaseType != typeof(MonoBehaviour))
            {
                _targetType = _targetType.BaseType;
            }
        }

        protected void OnDisable()
        {
            SceneView.duringSceneGui -= this.OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            DebugX.LogWarning("OnInspectorGUI() on FSMEditor");
            ShowStateInfo();

            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            var transform = ((MonoBehaviour)target).transform;
            Handles.Label(transform.position + Vector3.up, _currentStateName);
        }


        private void ShowStateInfo()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _currentStateName = GetTypeName(GetPropertyValue("CurrentState"));
            var previousStateName = GetTypeName(GetPropertyValue("CurrentState"));
            var globalStateName = GetTypeName(GetPropertyValue("GlobalState"));

            var ids = new List<int>();
            var names = new List<string>();
            SetStateList((dynamic)GetValue("_states"), ids, names, _currentStateName);


            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(55));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"GLOBAL : ", EditorStyles.whiteLargeLabel, GUILayout.Width(100));
            GUILayout.Label($"{globalStateName}", EditorStyles.largeLabel, GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("PreviousState : ", EditorStyles.whiteLargeLabel, GUILayout.Width(100));
            GUILayout.Label($"{previousStateName}", EditorStyles.largeLabel, GUILayout.ExpandWidth(false));


            GUILayout.Label("➜", EditorStyles.whiteLargeLabel, GUILayout.Width(20));

            GUILayout.Label("CurrentState : ", EditorStyles.whiteLargeLabel, GUILayout.Width(100));
            int newValue = EditorGUILayout.Popup(_statePopupIndex, names.ToArray());

            if (newValue != _statePopupIndex)
            {
                _statePopupIndex = newValue;
                _targetType.GetMethod("ChangeState").Invoke(target, new object[1] { ids[_statePopupIndex] });
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private object GetPropertyValue(string propertyName)
            => GetValue(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
        private object GetValue(string fieldName, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default)
        {
            return _targetType.GetField(fieldName, flags)?.GetValue(target);
        }

        private string GetTypeName(object type)
        {
            return type?.ToString()?.Split('.')[^1] ?? "None";
        }

        private void SetStateList<T>(Dictionary<int, T> states, List<int> ids, List<string> names, string currentStateName)
        {
            var keys = states.Keys.ToList();
            var values = states.Values.ToList();
            for (int i = 0; i < states.Count; i++)
            {
                var name = GetTypeName(values[i]);
                names.Add(name);
                ids.Add(keys[i]);

                if (name == currentStateName)
                {
                    _statePopupIndex = i;
                }
            }
        }
    }
}