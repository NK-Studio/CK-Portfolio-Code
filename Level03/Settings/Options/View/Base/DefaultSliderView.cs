using Doozy.Runtime.Common.Events;
using Doozy.Runtime.UIManager.Components;
using NKStudio.Option;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Option
{
    public class DefaultSliderView : MonoBehaviour
    {
        public UISlider Slider;
        public TMP_Text Number;
        
        public UIButton LButton;
        public UIButton RButton;
        
        private void Start()
        {
            Slider.OnValueChanged.AddListener(Refresh);
        }
        
        /// <summary>
        /// 뷰를 갱신합니다.
        /// </summary>
        /// <param name="value">Slider의 value를 설정합니다.</param>
        public virtual void Refresh(float value)
        {
            Slider.value = value;
            Number.text = (value * 100).ToString("N0");
        }
        
        /// <summary>
        /// Left 클릭 이벤트를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public UnityEvent OnLeftClickEvent()
        {
            return LButton.onClickEvent;
        }
        
        /// <summary>
        /// Right 클릭 이벤트를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public UnityEvent OnRightClickEvent()
        {
            return RButton.onClickEvent;
        }

        /// <summary>
        /// 핸들에 클릭을 놓았을 때 동작합니다.
        /// </summary>
        /// <returns></returns>
        public UnityEvent OnHandleExit()
        {
            return Slider.onPointerUpEvent;
        }

        /// <summary>
        /// 값이 변화하면 동작합니다.
        /// </summary>
        /// <returns></returns>
        public FloatEvent OnValueChanged()
        {
            return Slider.OnValueChanged;
        }
    }
}
