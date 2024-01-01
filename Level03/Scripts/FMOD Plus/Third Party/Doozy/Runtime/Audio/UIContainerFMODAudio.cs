using System.Collections.Generic;
using Doozy.Runtime.Common.Utils;
using Doozy.Runtime.Reactor.Ticker;
using Doozy.Runtime.UIManager.Animators;
using FMODPlus;
using FMODUnity;
using UnityEngine;

namespace Doozy.Runtime.UIManager.Audio
{
    /// <summary>
    /// Specialized audio component used to play a set EventReference by listening to a UIContainer (controller) show/hide commands.
    /// </summary>
    [AddComponentMenu("UI/Containers/Addons/UIContainer FMOD Audio")]
    public class UIContainerFMODAudio : BaseUIContainerAnimator
    {
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/UI/Containers/Addons/UIContainer FMOD Audio", false, 8)]
        private static void CreateComponent(UnityEditor.MenuCommand menuCommand)
        {
            GameObjectUtils.AddToScene<UIContainerFMODAudio>("UIContainer FMOD Audio", false, true);
        }
        #endif

        [SerializeField] private FMODAudioSource AudioSource;
        /// <summary> Reference to a target Audio source </summary>
        public FMODAudioSource audioSource => AudioSource;

        /// <summary> Check if a AudioSource is referenced or not </summary>
        public bool hasAudioSource => AudioSource != null;

        [SerializeField] private EventReference ShowAudioClip;
        /// <summary> Container Show AudioClip </summary>
        public EventReference showAudioClip => ShowAudioClip;

        [SerializeField] private EventReference HideAudioClip;
        /// <summary> Container Hide AudioClip </summary>
        public EventReference hideAudioClip => HideAudioClip;

        /// <summary> Stop the currently playing sound, if any. </summary>
        public override void StopAllReactions()
        {
            if (!hasAudioSource) return;
            audioSource.Stop();
        }

        public override void Show()
        {
            if (!hasAudioSource) return;
            if (showAudioClip.IsNull) return;
            audioSource.Stop();
            audioSource.clip = showAudioClip;
            audioSource.Play();
        }

        public override void ReverseShow() =>
            Hide();

        public override void Hide()
        {
            if (!hasAudioSource) return;
            if (hideAudioClip.IsNull) return;
            audioSource.Stop();
            audioSource.clip = hideAudioClip;
            audioSource.Play();
        }

        public override void ReverseHide() =>
            Show();

        /// <summary> Ignored </summary>
        public override void UpdateSettings() {}
        /// <summary> Ignored </summary>
        public override void InstantShow() {}
        /// <summary> Ignored </summary>
        public override void InstantHide() {}
        /// <summary> Ignored </summary>
        public override void ResetToStartValues(bool forced = false) {}
        /// <summary> Ignored </summary>
        public override List<Heartbeat> SetHeartbeat<T>() { return null; }
    }
}
