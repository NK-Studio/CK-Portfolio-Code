using Doozy.Editor.EditorUI;
using Doozy.Editor.EditorUI.Components;
using Doozy.Editor.EditorUI.Components.Internal;
using Doozy.Editor.EditorUI.ScriptableObjects.Colors;
using Doozy.Editor.EditorUI.Utils;
using Doozy.Editor.Mody;
using Doozy.Editor.Mody.Components;
using Doozy.Editor.UIElements;
using Doozy.Runtime.UIElements.Extensions;
using Doozy.Runtime.UIManager.Modules;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Doozy.Editor.UIManager.Editors.Modules
{
    [CustomEditor(typeof(FMODAudioSourceModule), true)]
    public sealed class FMODAudioSourceModuleEditor : ModyModuleEditor<FMODAudioSourceModule>
    {
        private SerializedProperty propertySource { get; set; }
        protected override Color accentColor => new Color32(7, 159, 255, 255);

        private  EditorThemeColor _fmodColor = new EditorThemeColor()
        {
            ColorOnDark = new Color32(0, 156, 255, 255),
            ColorOnLight = new Color32(6, 135, 217, 255)
        };
        
        
        protected override void FindProperties()
        {
            base.FindProperties();

            propertySource = serializedObject.FindProperty(nameof(FMODAudioSourceModule.Source));
        }

        protected override void InitializeEditor()
        {
            base.InitializeEditor();

            componentHeader
                .SetComponentNameText(FMODAudioSourceModule.k_DefaultModuleName)
                .SetSecondaryIcon(EditorSpriteSheets.EditorUI.Icons.Sound);
        }

        protected override void InitializeSettings()
        {
            base.InitializeSettings();
            EditorSelectableColorInfo FMODColorInfo = EditorSelectableColors.Default.AudioComponent;
            FMODColorInfo.Normal = _fmodColor;
            settingsTab.ButtonSetAccentColor(FMODColorInfo);
            
            settingsAnimatedContainer.SetOnShowCallback(() =>
            {
                var actionsDrawer =
                    new ModyActionsDrawer();
                
                actionsDrawer.schedule.Execute(() => actionsDrawer.Update());

                VisualElement actionsContainer =
                    new VisualElement().SetName("Actions Container");

                void AddActionToDrawer(ModyActionsDrawerItem item)
                {
                    actionsDrawer.AddItem(item);
                    actionsContainer.AddChild(item.animatedContainer);
                }

                //MODULE ACTIONS
                AddActionToDrawer(
                    new ModyActionsDrawerItem(serializedObject.FindProperty(nameof(FMODAudioSourceModule.Play))));
                AddActionToDrawer(
                    new ModyActionsDrawerItem(serializedObject.FindProperty(nameof(FMODAudioSourceModule.Stop))));
                AddActionToDrawer(
                    new ModyActionsDrawerItem(serializedObject.FindProperty(nameof(FMODAudioSourceModule.Mute))));
                AddActionToDrawer(
                    new ModyActionsDrawerItem(serializedObject.FindProperty(nameof(FMODAudioSourceModule.Unmute))));
                AddActionToDrawer(
                    new ModyActionsDrawerItem(serializedObject.FindProperty(nameof(FMODAudioSourceModule.Pause))));
                AddActionToDrawer(
                    new ModyActionsDrawerItem(serializedObject.FindProperty(nameof(FMODAudioSourceModule.Unpause))));

                settingsAnimatedContainer
                    .AddContent
                    (
                        FluidField.Get()
                            .SetLabelText("AudioSource Reference")
                            .AddFieldContent(DesignUtils.NewPropertyField(propertySource).TryToHideLabel())
                    )
                    .AddContent(DesignUtils.spaceBlock)
                    .AddContent(actionsDrawer)
                    .AddContent(DesignUtils.spaceBlock)
                    .AddContent(actionsContainer)
                    .Bind(serializedObject);
            });
        }
    }
}