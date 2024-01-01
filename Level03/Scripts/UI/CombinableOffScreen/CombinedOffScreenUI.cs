using TMPro;
using UnityEngine;

namespace UI
{
    public class CombinedOffScreenUI : CombinableOffScreenUI
    {
        public TMP_Text CountText;
        public Vector3 MeanPosition;
        
        public override void UpdatePosition()
        {
            if (!TargetUI)
            {
                return;
            }
            TargetImage.gameObject.SetActive(true);
            TargetUI.gameObject.SetActive(true);
            
            // 화면 밖에 있는 경우
            Vector3 computedPositionSS = ClampPosition(MeanPosition, Padding);

            TargetUI.position = computedPositionSS;
            if (UseRotation)
            {
                RotationTargetUI.rotation = GetRotation(computedPositionSS);
            }
        }

        public void SetEnemyCount(int count)
        {
            if (CountText)
            {
                CountText.text = count.ToString();
            }
        }
        
    }
}