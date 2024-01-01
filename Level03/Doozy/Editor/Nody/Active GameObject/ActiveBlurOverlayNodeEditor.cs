// Copyright (c) 2015 - 2023 Doozy Entertainment. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;
using Doozy.Editor.EditorUI;
using Doozy.Editor.EditorUI.Components;
using Doozy.Editor.EditorUI.Utils;
using Doozy.Editor.Nody.Nodes.Internal;
using Doozy.Runtime.Nody.Nodes;
using Doozy.Runtime.UIElements.Extensions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Doozy.Editor.Nody.Nodes
{
    [CustomEditor(typeof(ActiveBlurOverlayNode))]
    public class ActiveBlurOverlayNodeEditor : FlowNodeEditor
    {
        public override IEnumerable<Texture2D> nodeIconTextures => EditorSpriteSheets.Nody.Icons.CustomNode; //custom animated icon
        
        // SerializedProperty
        private SerializedProperty activeBlurOverlayProperty { get; set; }
        
        // PropertyField
        private PropertyField activeBlurOverlayPropertyField { get; set; }

        // FluidField
        private FluidField activeBlurOverlayField { get; set; }
        
        protected override void InitializeEditor()
        {
            base.InitializeEditor();

            componentHeader.SetComponentNameText(ObjectNames.NicifyVariableName(nameof(ActiveBlurOverlayNode))); //node name

            // --- SerializedProperty ---
            activeBlurOverlayProperty = serializedObject.FindProperty(nameof(ActiveBlurOverlayNode.ActiveBlurOverlay));
            
            // --- PropertyField ---
            activeBlurOverlayPropertyField = DesignUtils.NewPropertyField(activeBlurOverlayProperty).SetStyleMarginLeft(12);

            activeBlurOverlayField =
                FluidField.Get()
                    .SetStyleFlexGrow(1)
                    .SetLabelText("Active Blur Overlay")
                    .AddFieldContent(activeBlurOverlayPropertyField);
            
            // --- Icon ---
            // componentHeader.SetIcon(EditorSpriteSheets.Nody.Icons.Infinity); //custom animated icon
            // componentHeader.SetIcon(EditorTextures.Nody.Icons.Infinity);     //custom static icon

            // --- Secondary Icon ---
            // componentHeader.SetSecondaryIcon(EditorSpriteSheets.Nody.Icons.One); //custom secondary animated icon
            // componentHeader.SetSecondaryIcon(EditorTextures.Nody.Icons.One);     //custom secondary static icon

            // --- Accent Color ---
            // componentHeader.SetAccentColor(EditorColors.Nody.Color); //custom color

            // --- Extra Buttons ---
            // componentHeader.AddManualButton("https://docs.doozyui.com/");     //custom manual link
            // componentHeader.AddApiButton("https://api.doozyui.com/");         //custom manual link
            // componentHeader.AddYouTubeButton("https://youtube.doozyui.com/"); //custom video link

            // --- Usage Example ---
            // componentHeader
            //     .SetComponentNameText("Component Name")
            //     .SetComponentTypeText("Nody Node")
            //     .SetIcon(EditorSpriteSheets.Nody.Icons.Nody)
            //     .SetSecondaryIcon(EditorSpriteSheets.Nody.Icons.Infinity)
            //     .SetAccentColor(EditorColors.EditorUI.Amber);
        }

        protected override void Compose()
        {
            base.Compose();

            root
                .AddSpaceBlock(2)
                .AddChild(activeBlurOverlayField);
        }
    }
}
