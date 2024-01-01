using UnityEditor;
using UnityToolbarExtender;

namespace NKStudio
{
    [InitializeOnLoad]
    public class ToolbarRegister
    {
        static ToolbarRegister()
        {
            // LeftToolbarGUI는 왼쪽부터 렌더링됩니다.
            ToolbarExtender.LeftToolbarGUI.Add(EnterPlayModeOptionToolbars.OnToolbarGUI);
            ToolbarExtender.LeftToolbarGUI.Add(FMODDebugToolbars.OnToolbarGUI);
            ToolbarExtender.LeftToolbarGUI.Add(SceneSwitchLeftButton.OnToolbarGUI);
            
            // RightToolbarGUI는 오른쪽부터 렌더링됩니다.
            ToolbarExtender.RightToolbarGUI.Add(SceneSwitchRightButton.OnToolbarGUI);
        }
    }
}