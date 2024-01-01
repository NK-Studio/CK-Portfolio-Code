using System;
using System.Linq;
using Doozy.Runtime.UIManager.Components;
using Doozy.Runtime.UIManager.Modules;
using Managers;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utility;

namespace Option
{
    public class Highlight : MonoBehaviour
    {
        [SerializeField, Header("Background")]
        private AnimatorModule BackgroundAnimator;
        [SerializeField]
        private Selectable Selectable;

        [SerializeField, Header("Buttons")]
        public AnimatorModule ButtonAnimator;
        
        [Header("Highlight Register")]
        public HighlightRegister HighlightRegister;
        
        [Header("Auto Scroll")]
        public RectTransform SelfRectTransform;
        public RectTransform ScrollViewport;
        public RectTransform ScrollContentPanel;

        [SerializeField, Header("Slider")] 
        public UISlider SliderPressReceiver;
        public float SliderPressFactorMultiplier = 10f;
        
        [SerializeField, Header("Events")] 
        public UnityEvent OnLeftPressed;
        public UnityEvent OnRightPressed;
        
        
        private BoolReactiveProperty _isPointerOver;

        private InputAction AxisInput => InputManager.Instance.Controller.System.Axis;
        private void Start()
        {
            if (!HighlightRegister)
            {
                HighlightRegister = FindAnyObjectByType<HighlightRegister>();
            }
            Selectable ??= GetComponent<Selectable>();
            if(!SelfRectTransform)
                SelfRectTransform = GetComponent<RectTransform>();
            if (!ScrollViewport || !ScrollContentPanel)
            {
                var helper = FindAnyObjectByType<AutoScrollHelper>();
                ScrollViewport = helper.Viewport;
                ScrollContentPanel = helper.ContentPanel;
            }
            _isPointerOver = new BoolReactiveProperty(false);

            _isPointerOver.ObserveEveryValueChanged(isPointerOver => isPointerOver.Value)
                .Subscribe(isPointerOver => {
                    if (isPointerOver)
                        Show();
                    else
                        Hide();
                }).AddTo(this);

            AxisInput.performed += OnAxisMove;
        }

        private void OnAxisMove(InputAction.CallbackContext ctx)
        {
            if (!_isPointerOver.Value)
            {
                return;
            }
            var axis = ctx.ReadValue<Vector2>();
            if(SliderPressReceiver)
            {
                // Debug.Log($"{name}::OnAxisMove - Slider += {axis.x * SliderPressFactorMultiplier}");
                SliderPressReceiver.value += axis.x * SliderPressFactorMultiplier;
            }else if (axis.x < 0f)
            {
                // Debug.Log($"{name}::OnAxisMove - LeftPressed");
                OnLeftPressed.Invoke();
            }else if (axis.x > 0f)
            {
                // Debug.Log($"{name}::OnAxisMove - RightPressed");
                OnRightPressed.Invoke();
            }
        }

        private void OnDestroy()
        {
            AxisInput.performed -= OnAxisMove;
        }

        private void Update()
        {
            // 마우스가 가리킬 때
            if (HighlightRegister.Results.Any(result => result.gameObject == transform.GetChild(0).gameObject)
                // 또는 Selected일 때  
                || Selectable && EventSystem.current.currentSelectedGameObject == Selectable.gameObject)
            {
                // 선택됐다고 판정
                _isPointerOver.Value = true;
                return; 
            }

            _isPointerOver.Value = false;
        }

        public void Show()
        {
            if (BackgroundAnimator)
                BackgroundAnimator.Animators[0].Play();
#if UNITY_EDITOR
            else
                Debug.Log("No UIAnimator found on " + gameObject.name);
#endif

            if (ButtonAnimator)
                ButtonAnimator.Animators[0].Play();

            if (!ScrollViewport || !ScrollContentPanel)
            {
                return;
            }

            var self = SelfRectTransform;
            var selfPosition = self.anchoredPosition;
            Rect selfRect = self.rect;
            float topPositionY = Mathf.Abs(selfPosition.y) - selfRect.height * 0.5f;
            float bottomPositionY = ScrollViewport.rect.y - (selfPosition.y - selfRect.height * 0.5f);

            bool inView = RectTransformUtility.RectangleContainsScreenPoint(ScrollViewport, self.position);
            if (!inView)
            {
                var contentPanelPosition = ScrollContentPanel.anchoredPosition;
                // Debug.Log($"OUT OF VIEW from {name} - aP: {selfPosition}, V.rect: {ScrollViewport.rect}, Content.pos: {contentPanelPosition}, top: {topPositionY}");
                if (topPositionY < contentPanelPosition.y)
                {
                    // Debug.Log($"top({topPositionY}) < content({contentPanelPosition.y}) => {topPositionY}");
                    ScrollContentPanel.anchoredPosition = contentPanelPosition.Copy(y: topPositionY);
                }
                else
                {
                    // Debug.Log($"top({topPositionY}) > content({contentPanelPosition.y}) => {bottomPositionY}");
                    ScrollContentPanel.anchoredPosition = contentPanelPosition.Copy(y: bottomPositionY);
                }
            }
            // else
            {
                // var contentPanelPosition = ScrollContentPanel.anchoredPosition;
                // Debug.Log($"{name} - aP: {selfPosition}, V.rect: {ScrollViewport.rect}, Content.pos: {contentPanelPosition}, top: {topPositionY}");
            }
        }

        public void Hide()
        {
            if (BackgroundAnimator)
                BackgroundAnimator.Animators[1].Play();
#if UNITY_EDITOR
            else
                Debug.Log("No UIAnimator found on " + gameObject.name);
#endif

            if (ButtonAnimator)
                ButtonAnimator.Animators[1].Play();
        }
    }
}
