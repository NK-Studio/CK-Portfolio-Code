// Copyright (c) 2015 - 2023 Doozy Entertainment. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using System.Collections.Generic;
using Doozy.Editor.EditorUI;
using Doozy.Editor.EditorUI.ScriptableObjects.Colors;
using Doozy.Runtime.Nody;
using Doozy.Runtime.Nody.Nodes;
using UnityEngine;

namespace Doozy.Editor.Nody.Nodes
{
    public class ActiveBlurOverlayNodeView : FlowNodeView
    {
        public override Type nodeType => typeof(ActiveBlurOverlayNode);
        public override Texture2D nodeIconTexture => EditorTextures.Nody.Icons.CustomNode;                        // custom static icon
        public override IEnumerable<Texture2D> nodeIconTextures => EditorSpriteSheets.Nody.Icons.CustomNode;      // custom animated icon
        public override Color nodeAccentColor => EditorColors.Nody.Color;                                         // custom accent color
        public override EditorSelectableColorInfo nodeSelectableAccentColor => EditorSelectableColors.Nody.Color; // custom selectable accent color

        public ActiveBlurOverlayNodeView(FlowGraphView graphView, FlowNode node) : base(graphView, node)
        {
            // InjectAddOutputButton(); // add '+' button to add new output ports
        }
    }
}
