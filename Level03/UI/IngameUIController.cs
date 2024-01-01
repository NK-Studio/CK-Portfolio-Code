using System;
using System.Collections.Generic;
using Doozy.Runtime.UIManager.Containers;
using Managers;
using UnityEngine;

namespace UI
{
    public class IngameUIController : MonoBehaviour
    {
        public bool IsPlayingTimeline
        {
            get => GameManager.Instance.IsPlayingTimeline;
            set => GameManager.Instance.IsPlayingTimeline = value;
        }
        public List<UIContainer> Targets;

        private void Awake()
        {
            // 매 씬마다 초기화되는 Property
            IsPlayingTimeline = false; 
        }

        public void TimelineStart(bool instantHide = false)
        {
            if (instantHide)
            {
                InstantHide();
            }
            else
            {
                Hide();
            }

            IsPlayingTimeline = true;
        }

        public void TimelineEnd()
        {
            Show();
            IsPlayingTimeline = false;
        }
        
        public void Show()
        {
            foreach (var ui in Targets)
                ui.Show();
        }

        public void Hide()
        {
            foreach (var ui in Targets)
                ui.Hide();
        }
        
        public void InstantShow()
        {
            foreach (var ui in Targets)
                ui.InstantShow();
        }
        
        public void InstantHide()
        {
            foreach (var ui in Targets)
                ui.InstantHide();
        }
    }
}