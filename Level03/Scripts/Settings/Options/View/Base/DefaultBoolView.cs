using Doozy.Runtime.UIManager.Components;
using NKStudio.Option;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Option
{
    public class DefaultBoolView : MonoBehaviour
    {
        public LocalizeStringEvent Title;
        
        public LocalizedString On;
        public LocalizedString Off;
        
        public UIButton LButton;
        public UIButton RButton;

        /// <summary>
        /// 뷰를 갱신합니다.
        /// </summary>
        /// <param name="active">true시 활성화, false시 비활성화합니다.</param>
        public virtual void Refresh(bool active)
        {
            switch (active)
            {
                case true:
                    Title.StringReference = On;
                    break;
                case false:
                    Title.StringReference = Off;
                    break;
            }
        }
        
        /// <summary>
        /// Left 클릭 이벤트를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public UnityEvent OnLeftClickEvent()
        {
            return LButton.onClickEvent;
        }
        
        /// Right 클릭 이벤트를 반환합니다.
        public UnityEvent OnRightClickEvent()
        {
            return RButton.onClickEvent;
        }
    }
}
