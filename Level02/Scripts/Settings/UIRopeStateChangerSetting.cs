using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(fileName = "UIRopeStateChangerSetting", menuName = "Settings/UIRopeStateChangerSetting")]
    public class UIRopeStateChangerSetting : ScriptableObject
    {
        [field: SerializeField]
        [ValidateInput("@speed == 0.25f", "이 값을 수정하려면 AD와 김한용 플머에게 문의하세요.")]
        public float speed { get; private set; } = 0.25f;

        [field: SerializeField]
        [ValidateInput("@ease == Ease.InCubic", "이 값을 수정하려면 AD와 김한용 플머에게 문의하세요.")]
        public Ease ease { get; private set; } = Ease.InCubic;
    }
}