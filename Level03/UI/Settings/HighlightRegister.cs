using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Option
{
    public class HighlightRegister : MonoBehaviour
    {
        private PointerEventData _pointerEventData;
        private GraphicRaycaster _raycaster;
        private EventSystem _eventSystem;

        public List<RaycastResult> Results { get; private set; }

        private void Start()
        {
            Results = new List<RaycastResult>();
            _pointerEventData = new PointerEventData(_eventSystem);
            _raycaster = GetComponentInParent<GraphicRaycaster>();
            _eventSystem = FindAnyObjectByType<EventSystem>();
        }

        private void Update()
        {
            // 포인터 이벤트 위치를 마우스 위치로 설정합니다.
            _pointerEventData.position = Mouse.current.position.ReadValue();

            // Raycast 결과 목록 생성
            Results.Clear();

            // Graphics Raycaster 및 마우스 클릭 위치를 사용한 Raycast
            _raycaster.Raycast(_pointerEventData, Results);
        }
    }
}