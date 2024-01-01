
using System.Collections.Generic;
using System.Linq;
using Doozy.Editor.EditorUI;
using Doozy.Editor.EditorUI.Components;
using Doozy.Editor.EditorUI.ScriptableObjects.Colors;
using Doozy.Editor.EditorUI.Utils;
using Doozy.Editor.UIManager.Editors.Animators.Internal;
using Doozy.Editor.UIManager.Editors.Components;
using Doozy.Runtime.UIElements.Extensions;
using Doozy.Runtime.UIManager.Audio;
using Doozy.Runtime.UIManager.Components;
using FMODPlus;
using FMODUnity;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Doozy.Editor.UIManager.Editors.Audio
{
    [CustomEditor(typeof(UISelectableFMODAudio), true)]
    public class UISelectableFMODAudioEditor : BaseTargetComponentAnimatorEditor
    {
        public UISelectableFMODAudio castedTarget => (UISelectableFMODAudio)target;
        public IEnumerable<UISelectableFMODAudio> castedTargets => targets.Cast<UISelectableFMODAudio>();

        protected override Color accentColor => new Color32(7,159,255, 255);
        protected override EditorSelectableColorInfo selectableAccentColor => EditorSelectableColors.Default.AudioComponent;
        
        private SerializedProperty propertyAudioSource { get; set; }
        private SerializedProperty propertyNormalAudioClip { get; set; }
        private SerializedProperty propertyHighlightedAudioClip { get; set; }
        private SerializedProperty propertyPressedAudioClip { get; set; }
        private SerializedProperty propertySelectedAudioClip { get; set; }
        private SerializedProperty propertyDisabledAudioClip { get; set; }

        private FluidField audioSourceFluidField { get; set; }
        private FluidField normalAudioClipFluidField { get; set; }
        private FluidField highlightedAudioClipFluidField { get; set; }
        private FluidField pressedAudioClipFluidField { get; set; }
        private FluidField selectedAudioClipFluidField { get; set; }
        private FluidField disabledAudioClipFluidField { get; set; }

        private ObjectField audioSourceObjectField { get; set; }
        private PropertyField normalAudioClipObjectField { get; set; }
        private PropertyField highlightedAudioClipObjectField { get; set; }
        private PropertyField pressedAudioClipObjectField { get; set; }
        private PropertyField selectedAudioClipObjectField { get; set; }
        private PropertyField disabledAudioClipObjectField { get; set; }

        private SerializedProperty propertyToggleCommand { get; set; }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            audioSourceFluidField?.Recycle();
            normalAudioClipFluidField?.Recycle();
            highlightedAudioClipFluidField?.Recycle();
            pressedAudioClipFluidField?.Recycle();
            selectedAudioClipFluidField?.Recycle();
            disabledAudioClipFluidField?.Recycle();
        }

        protected override void FindProperties()
        {
            base.FindProperties();
            propertyAudioSource = serializedObject.FindProperty("AudioSource");
            propertyNormalAudioClip = serializedObject.FindProperty("NormalAudioClip");
            propertyHighlightedAudioClip = serializedObject.FindProperty("HighlightedAudioClip");
            propertyPressedAudioClip = serializedObject.FindProperty("PressedAudioClip");
            propertySelectedAudioClip = serializedObject.FindProperty("SelectedAudioClip");
            propertyDisabledAudioClip = serializedObject.FindProperty("DisabledAudioClip");

            propertyToggleCommand = serializedObject.FindProperty("ToggleCommand");
        }

        protected override void InitializeEditor()
        {
            base.InitializeEditor();

            componentHeader
                .SetComponentNameText(ObjectNames.NicifyVariableName(nameof(UISelectable)))
                .SetIcon(EditorSpriteSheets.EditorUI.Icons.Sound)
                .SetComponentTypeText("Audio");

            audioSourceObjectField =
                DesignUtils.NewObjectField(propertyAudioSource, typeof(FMODAudioSource))
                    .SetStyleFlexGrow(1)
                    .SetTooltip("Target AudioSource");
            audioSourceFluidField =
                FluidField.Get()
                    .SetLabelText("Audio Source")
                    .SetIcon(EditorSpriteSheets.EditorUI.Icons.Sound)
                    .AddFieldContent(audioSourceObjectField);

            normalAudioClipObjectField = DesignUtils.NewPropertyField(propertyNormalAudioClip);
            normalAudioClipFluidField = FluidField.Get().SetLabelText(" Normal").SetElementSize(ElementSize.Tiny).AddFieldContent(normalAudioClipObjectField);

            highlightedAudioClipObjectField = DesignUtils.NewPropertyField(propertyHighlightedAudioClip);
            highlightedAudioClipFluidField = FluidField.Get().SetLabelText(" Highlighted").SetElementSize(ElementSize.Tiny).AddFieldContent(highlightedAudioClipObjectField);

            pressedAudioClipObjectField = DesignUtils.NewPropertyField(propertyPressedAudioClip);
            pressedAudioClipFluidField = FluidField.Get().SetLabelText(" Pressed").SetElementSize(ElementSize.Tiny).AddFieldContent(pressedAudioClipObjectField);

            selectedAudioClipObjectField = DesignUtils.NewPropertyField(propertySelectedAudioClip);
            selectedAudioClipFluidField = FluidField.Get().SetLabelText(" Selected").SetElementSize(ElementSize.Tiny).AddFieldContent(selectedAudioClipObjectField);

            disabledAudioClipObjectField = DesignUtils.NewPropertyField(propertyDisabledAudioClip);
            disabledAudioClipFluidField = FluidField.Get().SetLabelText(" Disabled").SetElementSize(ElementSize.Tiny).AddFieldContent(disabledAudioClipObjectField);
        }

        protected override void Compose()
        {
            root
                .AddChild(componentHeader)
                .AddSpaceBlock()
                .AddChild(BaseUISelectableAnimatorEditor.GetController(propertyController, propertyToggleCommand))
                .AddSpaceBlock(2)
                .AddChild(audioSourceFluidField)
                .AddSpaceBlock(2)
                .AddChild(normalAudioClipFluidField)
                .AddSpaceBlock()
                .AddChild(highlightedAudioClipFluidField)
                .AddSpaceBlock()
                .AddChild(pressedAudioClipFluidField)
                .AddSpaceBlock()
                .AddChild(selectedAudioClipFluidField)
                .AddSpaceBlock()
                .AddChild(disabledAudioClipFluidField)
                .AddEndOfLineSpace()
                ;
        }

       
    }
}
