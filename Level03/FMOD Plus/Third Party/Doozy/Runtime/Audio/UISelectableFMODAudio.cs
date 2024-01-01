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
    /// Specialized audio component used to play a set EventReference by listening to a UISelectable (controller) selection state changes.
    /// </summary>
    [AddComponentMenu("UI/Components/Addons/UISelectable FMOD Audio")]
    public class UISelectableFMODAudio : BaseUISelectableAnimator
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/UI/Components/Addons/UISelectable FMOD Audio", false, 8)]
        private static void CreateComponent(UnityEditor.MenuCommand menuCommand)
        {
            GameObjectUtils.AddToScene<UISelectableFMODAudio>("UISelectable FMOD Audio", false, true);
        }
#endif

        [SerializeField] private FMODAudioSource AudioSource;

        /// <summary> Reference to a target Audio source </summary>
        public FMODAudioSource audioSource => AudioSource;

        /// <summary> Check if a AudioSource is referenced or not </summary>
        public bool hasAudioSource => AudioSource != null;

        [SerializeField] private EventReference NormalAudioClip;

        /// <summary> AudioClip for the Selectable Normal state </summary>
        public EventReference normalAudioClip => NormalAudioClip;

        [SerializeField] private EventReference HighlightedAudioClip;

        /// <summary> AudioClip for the Selectable Highlighted state </summary>
        public EventReference highlightedAudioClip => HighlightedAudioClip;

        [SerializeField] private EventReference PressedAudioClip;

        /// <summary> AudioClip for the Selectable Pressed state </summary>
        public EventReference pressedAudioClip => PressedAudioClip;

        [SerializeField] private EventReference SelectedAudioClip;

        /// <summary> AudioClip for the Selectable Selected state </summary>
        public EventReference selectedAudioClip => SelectedAudioClip;

        [SerializeField] private EventReference DisabledAudioClip;

        /// <summary> AudioClip for the Selectable Disabled state </summary>
        public EventReference disabledAudioClip => DisabledAudioClip;

        private bool initialized { get; set; }

        protected override void OnEnable()
        {
            initialized = false;
            base.OnEnable();
        }

        public override void StopAllReactions()
        {
            if (!hasAudioSource)
                return;

            audioSource.Stop();
        }

        public override bool IsStateEnabled(UISelectionState state) =>
            true;

        public override void Play(UISelectionState state)
        {
            if (!initialized)
            {
                initialized = true;
                if (state == UISelectionState.Normal)
                    return;
            }

            if (!hasAudioSource)
                return;


            switch (state)
            {
                case UISelectionState.Normal:
                    if (normalAudioClip.IsNull) return;
                    audioSource.PlayOneShot(normalAudioClip);
                    break;
                case UISelectionState.Highlighted:
                    if (highlightedAudioClip.IsNull) return;
                    audioSource.PlayOneShot(highlightedAudioClip);
                    break;
                case UISelectionState.Pressed:
                    if (pressedAudioClip.IsNull) return;
                    audioSource.PlayOneShot(pressedAudioClip);
                    break;
                case UISelectionState.Selected:
                    if (selectedAudioClip.IsNull) return;
                    audioSource.PlayOneShot(selectedAudioClip);
                    break;
                case UISelectionState.Disabled:
                    if (disabledAudioClip.IsNull) return;
                    audioSource.PlayOneShot(disabledAudioClip);
                    break;
                default:
                    return;
            }
        }

        public override void UpdateSettings()
        {
        } //ignored

        public override void ResetToStartValues(bool forced = false)
        {
        } //ignored

        public override List<Heartbeat> SetHeartbeat<T>()
        {
            return null;
        } //ignored
    }
}