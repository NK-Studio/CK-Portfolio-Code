using UnityEditor;
using NKStudio;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(MakeChainModule))]
public class MakeChainModuleEditor : Editor
{
    public VisualTreeAsset TreeAsset;
    private MakeChainModule _makeChainModule;

    public override VisualElement CreateInspectorGUI()
    {
        if (!TreeAsset)
            return base.CreateInspectorGUI();

        _makeChainModule = (MakeChainModule)target;

        VisualElement root = new VisualElement();
        TreeAsset.CloneTree(root);

        // Add your UI content here
        FloatField spacingField = root.Q<FloatField>("floatfield_spacing");
        spacingField.label = Application.systemLanguage == SystemLanguage.Korean ? "간격" : "Spacing";
        Button bakeButton = root.Q<Button>("button_bake");

        if (Application.isEditor)
        {
            bakeButton.SetEnabled(true);
            spacingField.SetEnabled(true);

            string bakeTooltip = Application.systemLanguage == SystemLanguage.Korean
                ? "Chain을 Bake합니다."
                : "Bake the Chain.";

            bakeButton.tooltip = bakeTooltip;
            
            string spacingTooltip = Application.systemLanguage == SystemLanguage.Korean
                ? "간격을 조정합니다."
                : "Adjust the spacing.";
            
            spacingField.tooltip = spacingTooltip;
        }

        if (Application.isPlaying)
        {
            bakeButton.SetEnabled(false);
            spacingField.SetEnabled(false);
            
            string bakeTooltip = Application.systemLanguage == SystemLanguage.Korean
                ? "Play모드에서는 사용 불가능합니다."
                : "It is not available in Play mode.";

            bakeButton.tooltip = bakeTooltip;
            
            string spacingTooltip = Application.systemLanguage == SystemLanguage.Korean
                ? "Play모드에서는 사용 불가능합니다."
                : "It is not available in Play mode.";
            
            spacingField.tooltip = spacingTooltip;
        }

        bakeButton.clickable.clicked += () => _makeChainModule.BakeChain();

        return root;
    }
}