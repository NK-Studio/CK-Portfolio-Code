// Copyright (c) 2015 - 2023 Doozy Entertainment. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;
using Doozy.Runtime.Common.Utils;
using Doozy.Runtime.Reactor.Ticker;
using Doozy.Runtime.UIManager.Animators;
using FMODPlus;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

namespace Doozy.Runtime.UIManager.Audio
{
    /// <summary>
    /// Specialized audio component used to play a set AudioClip by listening to a UIToggle (controller) isOn state changes
    /// </summary>
    [AddComponentMenu("UI/Components/Addons/UIToggle FMOD Audio")]
    public class UIToggleFMODAudio : BaseUIToggleAnimator
    {
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/UI/Components/Addons/UIToggle FMOD Audio", false, 8)]
        private static void CreateComponent(UnityEditor.MenuCommand menuCommand)
        {
            GameObjectUtils.AddToScene<UIToggleFMODAudio>("UIToggle FMOD Audio", false, true);
        }
        #endif

        [SerializeField] private FMODAudioSource AudioSource;
        /// <summary> Reference to a target Audio source </summary>
        public FMODAudioSource audioSource => AudioSource;

        /// <summary> Check if a AudioSource is referenced or not </summary>
        public bool hasAudioSource => AudioSource != null;

        [SerializeField] private EventReference OnAudioClip;
        /// <summary> Audio clip to play when the toggle transitions to the On state </summary>
        public EventReference onAudioClip => OnAudioClip;

        [SerializeField] private EventReference OffAudioClip;
        /// <summary> Audio clip to play when the toggle transitions to the Off state </summary>
        public EventReference offAudioClip => OffAudioClip;

        protected override bool onAnimationIsActive => hasController && hasAudioSource && controller.isOn && onAudioClip.IsNull && audioSource.isPlaying;
        protected override bool offAnimationIsActive => hasController && hasAudioSource && !controller.isOn && offAudioClip.IsNull && audioSource.isPlaying;
        protected override UnityAction playOnAnimation => () =>
        {
            if (!hasAudioSource) return;
            if (onAudioClip.IsNull) return;
            audioSource.Stop();
            audioSource.clip = onAudioClip;
            audioSource.Play();
        };

        protected override UnityAction playOffAnimation => () =>
        {
            if (!hasAudioSource) return;
            if (offAudioClip.IsNull) return;
            audioSource.Stop();
            audioSource.clip = offAudioClip;
            audioSource.Play();
        };
        protected override UnityAction reverseOnAnimation => () => playOffAnimation.Invoke();
        protected override UnityAction reverseOffAnimation => () => playOnAnimation.Invoke();
        /// <summary> Action disabled </summary>
        protected override UnityAction instantPlayOnAnimation => () => {}; //do nothing
        /// <summary> Action disabled </summary>
        protected override UnityAction instantPlayOffAnimation => () => {}; //do nothing
        protected override UnityAction stopOnAnimation => () =>
        {
            if (!hasAudioSource) return;
            audioSource.Stop();
        };
        protected override UnityAction stopOffAnimation => () =>
        {
            if (!hasAudioSource) return;
            audioSource.Stop();
        };
        
        /// <summary> Action disabled </summary>
        protected override UnityAction addResetToOnStateCallback => () => {}; //do nothing
        /// <summary> Action disabled </summary>
        protected override UnityAction removeResetToOnStateCallback => () => {}; //do nothing
        /// <summary> Action disabled </summary>
        protected override UnityAction addResetToOffStateCallback => () => {}; //do nothing
        /// <summary> Action disabled </summary>
        protected override UnityAction removeResetToOffStateCallback => () => {}; //do nothing

        public override void UpdateSettings()
        {
            if(!hasAudioSource) return;
            if (!hasController) return;
            audioSource.playOnAwake = false;
            audioSource.clip = controller.isOn ? onAudioClip : offAudioClip;
        }
        
        public override void StopAllReactions()
        {
            if (!hasAudioSource) return;
            audioSource.Stop();
        }
        
        public override void ResetToStartValues(bool forced = false) {}
        public override List<Heartbeat> SetHeartbeat<Theartbeat>() => null;
    }
}
