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
using Doozy.Runtime.UIManager.Components;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Doozy.Editor.UIManager.Editors.Audio
{
    [CustomEditor(typeof(UIToggleFMODAudio), true)]
    [CanEditMultipleObjects]
    public class UIToggleFMODAudioEditor : BaseTargetComponentAnimatorEditor
    {
        public UIToggleFMODAudio castedTarget => (UIToggleFMODAudio)target;
        public IEnumerable<UIToggleFMODAudio> castedTargets => targets.Cast<UIToggleFMODAudio>();
        
        protected override Color accentColor =>new Color32(7, 159, 255, 255);
        protected override EditorSelectableColorInfo selectableAccentColor => EditorSelectableColors.Default.AudioComponent;
        
        private SerializedProperty propertyAudioSource { get; set; }
        private SerializedProperty propertyOnAudioClip { get; set; }
        private SerializedProperty propertyOffAudioClip { get; set; }
        
        private FluidField audioSourceFluidField { get; set; }
        private FluidField onAudioClipFluidField { get; set; }
        private FluidField offAudioClipFluidField { get; set; }
        
        private PropertyField audioSourceObjectField { get; set; }
        private PropertyField onAudioClipObjectField { get; set; }
        private PropertyField offAudioClipObjectField { get; set; }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            audioSourceFluidField?.Recycle();
            onAudioClipFluidField?.Recycle();
            offAudioClipFluidField?.Recycle();
        }
        
        protected override void FindProperties()
        {
            base.FindProperties();
            
            propertyAudioSource = serializedObject.FindProperty("AudioSource");
            propertyOnAudioClip = serializedObject.FindProperty("OnAudioClip");
            propertyOffAudioClip = serializedObject.FindProperty("OffAudioClip");
        }
        
        protected override void InitializeEditor()
        {
            base.InitializeEditor();

            componentHeader
                .SetComponentNameText(ObjectNames.NicifyVariableName(nameof(UIToggle)))
                .SetIcon(EditorSpriteSheets.EditorUI.Icons.Sound)
                .SetComponentTypeText("Audio")
                .AddManualButton()
                .AddApiButton()
                .AddYouTubeButton();


            audioSourceObjectField = DesignUtils.NewPropertyField(propertyAudioSource);
            audioSourceFluidField = FluidField.Get().SetLabelText("Audio Source").SetIcon(EditorSpriteSheets.EditorUI.Icons.Sound).AddFieldContent(audioSourceObjectField);

            onAudioClipObjectField = DesignUtils.NewPropertyField(propertyOnAudioClip);
            onAudioClipFluidField = FluidField.Get().SetLabelText(" On").SetElementSize(ElementSize.Tiny).AddFieldContent(onAudioClipObjectField);

            offAudioClipObjectField = DesignUtils.NewPropertyField(propertyOffAudioClip);
            offAudioClipFluidField = FluidField.Get().SetLabelText(" Off").SetElementSize(ElementSize.Tiny).AddFieldContent(offAudioClipObjectField);
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
                .AddChild(onAudioClipFluidField)
                .AddSpaceBlock()
                .AddChild(offAudioClipFluidField)
                .AddEndOfLineSpace()
                ;
        }
    }
}
