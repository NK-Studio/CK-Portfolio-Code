using UnityEditor;
using UnityEngine.UIElements;

[CustomEditor(typeof(DitherEffect))]
public class DitherEffectEditor : Editor
{
    public VisualTreeAsset TreeAsset;
    
    public override VisualElement CreateInspectorGUI()
    {
        if (!TreeAsset)
            return base.CreateInspectorGUI();

        VisualElement root = new VisualElement();
        TreeAsset.CloneTree(root);
        
        return root;
    }
}