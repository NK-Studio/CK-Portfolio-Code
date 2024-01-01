// Copyright (c) 2015 - 2023 Doozy Entertainment. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;
using System.Linq;
using Doozy.Editor.EditorUI;
using Doozy.Editor.EditorUI.Components;
using Doozy.Editor.EditorUI.ScriptableObjects.Colors;
using Doozy.Editor.EditorUI.Utils;
using Doozy.Editor.UIManager.Editors.Animators.Internal;
using Doozy.Runtime.UIElements.Extensions;
using Doozy.Runtime.UIManager.Audio;
using Doozy.Runtime.UIManager.Containers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Doozy.Editor.UIManager.Editors.Audio
{
    [CustomEditor(typeof(UIContainerFMODAudio), true)]
    [CanEditMultipleObjects]
    public class UIContainerFMODAudioEditor : BaseTargetComponentAnimatorEditor
    {
        public UIContainerFMODAudio castedTarget => (UIContainerFMODAudio)target;
        public IEnumerable<UIContainerFMODAudio> castedTargets => targets.Cast<UIContainerFMODAudio>();

        protected override Color accentColor => new Color32(7,159,255, 255);
        protected override EditorSelectableColorInfo selectableAccentColor => EditorSelectableColors.Default.AudioComponent;

        private SerializedProperty propertyAudioSource { get; set; }
        private SerializedProperty propertyShowAudioClip { get; set; }
        private SerializedProperty propertyHideAudioClip { get; set; }

        private FluidField audioSourceFluidField { get; set; }
        private FluidField showAudioClipFluidField { get; set; }
        private FluidField hideAudioClipFluidField { get; set; }

        private PropertyField audioSourceObjectField { get; set; }
        private PropertyField showAudioClipObjectField { get; set; }
        private PropertyField hideAudioClipObjectField { get; set; }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            audioSourceFluidField?.Recycle();
            showAudioClipFluidField?.Recycle();
            hideAudioClipFluidField?.Recycle();
        }
      
        protected override void FindProperties()
        {
            base.FindProperties();

            propertyAudioSource = serializedObject.FindProperty("AudioSource");
            propertyShowAudioClip = serializedObject.FindProperty("ShowAudioClip");
            propertyHideAudioClip = serializedObject.FindProperty("HideAudioClip");
        }

        protected override void InitializeEditor()
        {
            base.InitializeEditor();

            componentHeader
                .SetComponentNameText(ObjectNames.NicifyVariableName(nameof(UIContainer)))
                .SetIcon(EditorSpriteSheets.EditorUI.Icons.Sound)
                .SetComponentTypeText("Audio");
            
            audioSourceObjectField = DesignUtils.NewPropertyField(propertyAudioSource);
            audioSourceFluidField = FluidField.Get().SetLabelText("Audio Source").SetIcon(EditorSpriteSheets.EditorUI.Icons.Sound).AddFieldContent(audioSourceObjectField);

            showAudioClipObjectField = DesignUtils.NewPropertyField(propertyShowAudioClip);
            showAudioClipFluidField = FluidField.Get().SetLabelText(" Show").SetElementSize(ElementSize.Tiny).AddFieldContent(showAudioClipObjectField);

            hideAudioClipObjectField = DesignUtils.NewPropertyField(propertyHideAudioClip);
            hideAudioClipFluidField = FluidField.Get().SetLabelText(" Hide").SetElementSize(ElementSize.Tiny).AddFieldContent(hideAudioClipObjectField);
        }

        protected override void Compose()
        {
            root
                .AddChild(componentHeader)
                .AddSpaceBlock()
                .AddChild(BaseUIContainerAnimatorEditor.GetController(propertyController))
                .AddSpaceBlock(2)
                .AddChild(audioSourceFluidField)
                .AddSpaceBlock(2)
                .AddChild(showAudioClipFluidField)
                .AddSpaceBlock()
                .AddChild(hideAudioClipFluidField)
                .AddEndOfLineSpace()
                ;
        }

     
    }
}
