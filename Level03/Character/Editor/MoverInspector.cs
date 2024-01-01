using Character.Core;
using UnityEditor;
using UnityEngine;

namespace Character.Editor
{
    //이 편집기 스크립트는 현재 레이캐스트 배열의 미리보기와 같은 무버 인스펙터에 몇 가지 추가 정보를 표시합니다.
    [CustomEditor(typeof(Mover))]
    public class MoverInspector : UnityEditor.Editor {

        private Mover _mover;

        private void Reset() => Setup();

        private void OnEnable() => Setup();

        //무버 구성 요소에 대한 참조 가져오기
        private void Setup() => _mover = (Mover)target;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawRaycastArrayPreview();
        }

        //인스펙터에서 레이캐스트 배열 미리보기 그리기;
        private void DrawRaycastArrayPreview()
        {
            if(_mover.SensorType == Sensor.ECastType.RaycastArray)
            {
                GUILayout.Space(5);

                Rect space = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(100));

                Rect background = new(space.x + (space.width - space.height)/2f, space.y, space.height, space.height);
                EditorGUI.DrawRect(background, Color.grey);

                const float pointSize = 3f;

                Vector3[] previewPositions = _mover.RaycastArrayPreviewPositions;

                Vector2 center = new(background.x + background.width/2f, background.y + background.height/2f);

                if(previewPositions != null && previewPositions.Length != 0)
                {
                    for(int i = 0; i < previewPositions.Length; i++)
                    {
                        Vector2 position = center + new Vector2(previewPositions[i].x, previewPositions[i].z) * background.width/2f * 0.9f;

                        EditorGUI.DrawRect(new Rect(position.x - pointSize/2f, position.y - pointSize/2f, pointSize, pointSize), Color.white);
                    }
                }

                if(previewPositions != null && previewPositions.Length != 0)
                    GUILayout.Label("광선의 수 = " + previewPositions.Length, EditorStyles.centeredGreyMiniLabel );
            }
        }

		
    }
}