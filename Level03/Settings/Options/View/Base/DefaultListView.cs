using Doozy.Runtime.UIManager.Components;
using NKStudio.Option;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace Option
{
    public class DefaultListView : MonoBehaviour
    {
        public LocalizeStringEvent Title;

        [SerializeField]
        private Transform circleGroup;

        public UIButton LButton;
        public UIButton RButton;

        private Circle[] _circles;

        /// <summary>
        /// 초기 원을 생성합니다.
        /// </summary>
        /// <param name="circlePrefab">Circle 프리팹</param>
        /// <param name="count">생성시킬 개수</param>
        /// <param name="index">활성화할 인덱스</param>
        public void InitCircle(Circle circlePrefab, int count, int index)
        {
            // 지원해야하는 해상도가 1개 뿐이라면 1개만 출력하고 아니라면 count만큼 출력합니다.
            _circles = new Circle[count];

            for (int i = 0; i < count; i++)
            {
                Circle circleObject = Instantiate(circlePrefab, circleGroup);
                circleObject.IsFill = false;

                _circles[i] = circleObject;
            }
            
            _circles[index].IsFill = true;
        }

        /// <summary>
        /// Index에 해당하는 원을 활성화합니다.
        /// </summary>
        /// <param name="index"></param>
        protected void SetActiveCircle(int index)
        {
            foreach (Circle circle in _circles)
                circle.IsFill = false;

            _circles[index].IsFill = true;
        }

        /// <summary>
        /// 뷰를 갱신합니다.
        /// </summary>
        /// <param name="antiAliasingIndex">활성화할 인덱스</param>
        public virtual void Refresh(int antiAliasingIndex)
        {
            SetActiveCircle(antiAliasingIndex);
        }

        /// <summary>
        /// 뷰를 갱신합니다.
        /// </summary>
        /// <param name="text">새로고침할 텍스트</param>
        /// <param name="activeIndex">활성화할 인덱스</param>
        public virtual void Refresh(LocalizedString text, int activeIndex)
        {
            Title.StringReference = text;
            SetActiveCircle(activeIndex);
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
