using System;
using NKStudio;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NKStudio
{
    [CustomEditor(typeof(OffScreenUI))]
    public class OffScreenUIEditor : Editor
    {
        private SerializedProperty _screenModeProperty;
        private SerializedProperty _defaultCanvasProperty;
        private SerializedProperty _targetCanvasProperty;
        private SerializedProperty _selfTargetProperty;
        private SerializedProperty _targetProperty;
        private SerializedProperty _autoTargetUIProperty;
        private SerializedProperty _pointerProperty;
        private SerializedProperty _pointerPrefabProperty;
        private SerializedProperty _offSpriteProperty;
        private SerializedProperty _onSpriteProperty;
        private SerializedProperty _paddingProperty;
        private SerializedProperty _offsetProperty;
        private SerializedProperty _offSetRotationProperty;
        private SerializedProperty _useRotationProperty;

        private VisualElement _root;
        private PropertyField _screenMode;
        private PropertyField _defaultCanvas;
        private PropertyField _targetCanvas;
        private PropertyField _selfTarget;
        private PropertyField _target;
        private PropertyField _autoTargetUI;
        private PropertyField _pointer;
        private PropertyField _pointerPrefab;
        private PropertyField _offSprite;
        private PropertyField _onSprite;
        private PropertyField _padding;
        private PropertyField _offset;
        private PropertyField _offSetRotation;
        private PropertyField _useRotation;

        private StyleSheet _styleSheet;

        private void Awake()
        {
            _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Plugins/CustomTemplate/Template/NKUSSTemplate.uss");
        }

        private void FindProperty()
        {
            _screenModeProperty = serializedObject.FindProperty("screenMode");
            _defaultCanvasProperty = serializedObject.FindProperty("defaultCanvas");
            _targetCanvasProperty = serializedObject.FindProperty("targetCanvas");
            _selfTargetProperty = serializedObject.FindProperty("selfTarget");
            _targetProperty = serializedObject.FindProperty("target");
            _autoTargetUIProperty = serializedObject.FindProperty("autoTargetUI");
            _pointerProperty = serializedObject.FindProperty("pointer");
            _pointerPrefabProperty = serializedObject.FindProperty("pointerPrefab");
            _offSpriteProperty = serializedObject.FindProperty("offSprite");
            _onSpriteProperty = serializedObject.FindProperty("onSprite");
            _paddingProperty = serializedObject.FindProperty("padding");
            _offsetProperty = serializedObject.FindProperty("offset");
            _offSetRotationProperty = serializedObject.FindProperty("offSetRotation");
            _useRotationProperty = serializedObject.FindProperty("useRotation");
        }

        private void InitializeRoot()
        {
            _root = new VisualElement();
            _root.styleSheets.Add(_styleSheet);

            _screenMode = new PropertyField(_screenModeProperty);
            _defaultCanvas = new PropertyField(_defaultCanvasProperty);
            _defaultCanvas.tooltip = "true시 씬에서 'OFFScreenCanvas'를 찾아서 자동으로 적용해줍니다.";
            _targetCanvas = new PropertyField(_targetCanvasProperty);
            _selfTarget = new PropertyField(_selfTargetProperty);
            _selfTarget.tooltip = "true시 자기 자신을 타겟팅합니다.";
            _target = new PropertyField(_targetProperty);
            _autoTargetUI = new PropertyField(_autoTargetUIProperty);
            _autoTargetUI.tooltip = "true시 자동으로 프리팹을 생성하여 적용해줍니다.";
            _pointer = new PropertyField(_pointerProperty);
            _pointer.tooltip = "Scene에 Image 컴포넌트를 가지고 있는 오브젝트를 연결합니다.";
            _pointerPrefab = new PropertyField(_pointerPrefabProperty);
            _pointerPrefab.tooltip = "Project에 Image 컴포넌트를 가지고 있는 프리팹을 연결합니다.";
            _offSprite = new PropertyField(_offSpriteProperty);
            _onSprite = new PropertyField(_onSpriteProperty);
            _onSprite.label = "On Sprite (Optional)";
            _padding = new PropertyField(_paddingProperty);
            _offset = new PropertyField(_offsetProperty);
            _offSetRotation = new PropertyField(_offSetRotationProperty);
            _offSetRotation.label = "Offset Rotation";
            _useRotation = new PropertyField(_useRotationProperty);
            _useRotation.tooltip = "true시 지속적으로 타겟을 바라봅니다.";
            Label OffScreenTitle = new("Off Screen UI");
            OffScreenTitle.AddToClassList("TitleStyle");
            _root.Add(OffScreenTitle);

            _root.Add(_screenMode);

            // 캔버스 그룹
            GroupBox canvasGroup = new();
            canvasGroup.AddToClassList("GroupBoxStyle");
            canvasGroup.text = "Canvas";
            canvasGroup.contentContainer.Add(_defaultCanvas);
            canvasGroup.contentContainer.Add(_targetCanvas);

            GroupBox targetGroup = new();
            targetGroup.AddToClassList("GroupBoxStyle");
            targetGroup.text = "타겟";
            targetGroup.contentContainer.Add(_selfTarget);
            targetGroup.contentContainer.Add(_target);

            GroupBox uiTargetGroup = new();
            uiTargetGroup.AddToClassList("GroupBoxStyle");
            uiTargetGroup.text = "UI 지정";
            uiTargetGroup.contentContainer.Add(_autoTargetUI);
            uiTargetGroup.contentContainer.Add(_pointer);
            uiTargetGroup.contentContainer.Add(_pointerPrefab);

            GroupBox spriteGroup = new();
            spriteGroup.AddToClassList("GroupBoxStyle");
            spriteGroup.text = "스프라이트";
            spriteGroup.contentContainer.Add(_offSprite);
            spriteGroup.contentContainer.Add(_onSprite);

            GroupBox optionGroup = new();
            optionGroup.AddToClassList("GroupBoxStyle");
            optionGroup.text = "옵션";
            optionGroup.contentContainer.Add(_padding);
            optionGroup.contentContainer.Add(_offset);
            optionGroup.contentContainer.Add(_useRotation);
            optionGroup.contentContainer.Add(_offSetRotation);

            _root.Add(canvasGroup);
            _root.Add(targetGroup);
            _root.Add(uiTargetGroup);
            _root.Add(spriteGroup);
            _root.Add(optionGroup);

            Init();
            Controller();
        }

        private void Init()
        {
            if (_defaultCanvasProperty.boolValue)
            {
                _targetCanvas.SetActive(false);
            }
            else
            {
                _targetCanvas.SetActive(true);
            }

            if (_selfTargetProperty.boolValue)
            {
                // 직접 자기 자신을 지정
                _target.SetActive(false);
            }
            else
            {
                // 타겟을 설정을 해야함
                _target.SetActive(true);
            }

            if (_autoTargetUIProperty.boolValue)
            {
                // 자동으로 프리팹을 생성하여 적용까지 해줌
                _pointer.SetActive(false);
                _pointerPrefab.SetActive(true);
            }
            else
            {
                // 내가 직접 타겟팅을 연결해야함.
                _pointer.SetActive(true);
                _pointerPrefab.SetActive(false);
            }

            OffScreenUI.EScreenMode screenMode = (OffScreenUI.EScreenMode)_screenModeProperty.enumValueIndex;

            switch (screenMode)
            {

                case OffScreenUI.EScreenMode.OffScreen:
                    _offSprite.SetActive(true);
                    _onSprite.SetActive(false);
                    break;
                case OffScreenUI.EScreenMode.OnScreen:
                    _offSprite.SetActive(false);
                    _onSprite.SetActive(true);
                    break;
                case OffScreenUI.EScreenMode.OnOffScreen:
                    _offSprite.SetActive(true);
                    _onSprite.SetActive(true);
                    break;
            }

            if (_useRotationProperty.boolValue)
                _offSetRotation.SetActive(true);
            else
                _offSetRotation.SetActive(false);

        }
        private void Controller()
        {
            _defaultCanvas.RegisterCallback<ChangeEvent<bool>>(evt => {
                if (evt.newValue)
                {
                    _targetCanvas.SetActive(false);
                }
                else
                {
                    _targetCanvas.SetActive(true);
                }
            });

            _selfTarget.RegisterCallback<ChangeEvent<bool>>(evt => {
                if (evt.newValue)
                {
                    _target.SetActive(false);
                }
                else
                {
                    _target.SetActive(true);
                }
            });

            _autoTargetUI.RegisterCallback<ChangeEvent<bool>>(evt => {
                if (evt.newValue)
                {
                    _pointer.SetActive(false);
                    _pointerPrefab.SetActive(true);
                }
                else
                {
                    _pointer.SetActive(true);
                    _pointerPrefab.SetActive(false);
                }
            });

            _screenMode.RegisterValueChangeCallback(evt => {
                OffScreenUI.EScreenMode screenMode = (OffScreenUI.EScreenMode)evt.changedProperty.enumValueIndex;

                switch (screenMode)
                {
                    case OffScreenUI.EScreenMode.OffScreen:
                        _offSprite.SetActive(true);
                        _onSprite.SetActive(false);
                        break;
                    case OffScreenUI.EScreenMode.OnScreen:
                        _offSprite.SetActive(false);
                        _onSprite.SetActive(true);
                        break;
                    case OffScreenUI.EScreenMode.OnOffScreen:
                        _offSprite.SetActive(true);
                        _onSprite.SetActive(true);
                        break;
                }
            });

            _useRotation.RegisterCallback<ChangeEvent<bool>>(evt => {
                if (evt.newValue)
                    _offSetRotation.SetActive(true);
                else
                    _offSetRotation.SetActive(false);
            });
        }

        public override VisualElement CreateInspectorGUI()
        {
            FindProperty();
            InitializeRoot();
            return _root;
        }


    }
}
