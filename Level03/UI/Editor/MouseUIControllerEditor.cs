using System;
using System.Collections.Generic;
using NKStudio;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(MouseUIController))]
public class MouseUIControllerEditor : Editor
{
    private SerializedProperty _mouseUISetting;
    private SerializedProperty _state;
    private SerializedProperty _normalMouseUI;
    private SerializedProperty _attackMouseUI;
    private SerializedProperty _pointerUI;
    private SerializedProperty _pointerArrowUI;
    // private SerializedProperty _rotateOffset;

    private StyleSheet _styleSheet;
    
    private VisualElement _root;

    private void Awake()
    {
        _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Plugins/CustomTemplate/Template/NKUSSTemplate.uss");
    }

    private void FindProperty()
    {
        _mouseUISetting = serializedObject.FindProperty("MouseUISetting");
        _state = serializedObject.FindProperty("state");
        _normalMouseUI = serializedObject.FindProperty("NormalMouseUI");
        _attackMouseUI = serializedObject.FindProperty("AttackMouseUI");
        _pointerUI = serializedObject.FindProperty("PointerUI");
        _pointerArrowUI = serializedObject.FindProperty("PointerArrowUI");
        // _rotateOffset = serializedObject.FindProperty("RotateOffset");
    }

    private void Initialize()
    {
        _root = new VisualElement();
        _root.styleSheets.Add(_styleSheet);

        Label mouseUITitle = new("Mouse UI Controller");
        mouseUITitle.AddToClassList("TitleStyle");
        
        var mouseUISetting = new PropertyField(_mouseUISetting);
        var state = new PropertyField(_state);
        var normalMouseUI = new PropertyField(_normalMouseUI);
        var attackMouseUI = new PropertyField(_attackMouseUI);
        var pointerUI = new PropertyField(_pointerUI);
        var pointerArrowUI = new PropertyField(_pointerArrowUI);
        // var rotateOffset = new PropertyField(_rotateOffset);
        var line01 = NKEditorUtility.Line(new Color(0f, 0f, 0f, 0.5f), 2, 6);
        var line02 = NKEditorUtility.Line(new Color(0f, 0f, 0f, 0.5f), 2, 6);
        
        _root.Add(mouseUITitle);
        _root.Add(state);
        _root.Add(normalMouseUI);
        _root.Add(line01);
        _root.Add(mouseUISetting);
        _root.Add(attackMouseUI);
        _root.Add(pointerUI);
        _root.Add(pointerArrowUI);
        _root.Add(line02);
        // _root.Add(rotateOffset);

        Dictionary<string,VisualElement> elements = new()
        {
            {"state", state},
            {"normalMouseUI", normalMouseUI},
            {"mouseUISetting", mouseUISetting},
            {"attackMouseUI", attackMouseUI},
            {"pointerUI", pointerUI},
            {"pointerArrowUI", pointerArrowUI},
            // {"rotateOffset", rotateOffset},
            {"line01", line01},
            {"line02", line02}
        };
        
        Control(elements);
        
        state.RegisterValueChangeCallback(evt => {
            Control(elements);
        });
    }

    private void Control(Dictionary<string,VisualElement> elements)
    {
        var attackMouseUI = elements["attackMouseUI"];
        var pointerUI = elements["pointerUI"];
        var mouseUISetting = elements["mouseUISetting"];
        var pointerArrowUI = elements["pointerArrowUI"];
        // var rotateOffset = elements["rotateOffset"];
        var line01 = elements["line01"];
        var line02 = elements["line02"];

        if (_state.enumValueIndex == 0)
        {
            mouseUISetting.SetActive(false);
            attackMouseUI.SetActive(false);
            pointerArrowUI.SetActive(false);
            pointerUI.SetActive(false);
            // rotateOffset.SetActive(false);
            line01.SetActive(false);
            line02.SetActive(false);
        }
        else if (_state.enumValueIndex == 1)
        {
            mouseUISetting.SetActive(true);
            attackMouseUI.SetActive(true);
            pointerArrowUI.SetActive(true);
            pointerUI.SetActive(true);
            // rotateOffset.SetActive(true);
            line01.SetActive(true);
            line02.SetActive(true);
        }
    }

    public override VisualElement CreateInspectorGUI()
    {
        FindProperty();
        Initialize();
        
        var iterator = serializedObject.GetIterator();
        for (int i = 0; i < 6; i++)
            iterator.NextVisible(true);

        while (iterator.NextVisible(true))
        {
            // UnityEngine.Debug.Log($"path={iterator.propertyPath} (displayName={iterator.displayName})");
            if (iterator.type.Equals("ArraySize") || iterator.propertyPath.Contains("Array.data"))
                continue;
            _root.Add(new PropertyField(iterator));
        }

        return _root;
    }

}
