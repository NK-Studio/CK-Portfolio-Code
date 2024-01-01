using System.Collections.Generic;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityAnimator;
using FMODPlus;
using ManagerX;
using Settings.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using Logger = NKStudio.Logger;

namespace Managers
{
    [ManagerDefaultPrefab("DialogManager")]
    public class DialogManager : SerializedMonoBehaviour, AutoManager
    {
        
        public static DialogManager Instance => AutoManager.Get<DialogManager>();

        [field: SerializeField, FoldoutGroup("상태", true)]
        public TMP_Text Target { get; set; }
        
        [field: SerializeField, FoldoutGroup("상태", true)]
        public FMODAudioSource AudioSource { get; set; }

        [field: SerializeReference, FoldoutGroup("상태", true)] 
        public DialogTable.DialogEvent CurrentEvent { get; private set; }

        [field: SerializeField, FoldoutGroup("상태", true)] 
        public float EventLeftTime { get; private set; }
        
        [field: SerializeField, FoldoutGroup("상태", true)]
        public Dictionary<string, int> EventCallCount { get; private set; } = new();

        [field: SerializeField, FoldoutGroup("설정", true)]
        public bool Disabled { get; set; } = false;
        
        
        [field: SerializeField, FoldoutGroup("설정", true)]
        public DialogTable Table { get; private set; }
        
        [field: SerializeField, FoldoutGroup("설정", true)] 
        public UIObjectType UIObjectType { get; private set; } = UIObjectType.RightSnowRabbitDialog;
        
        [field: SerializeField, FoldoutGroup("설정", true)]
        public float EventDuration { get; private set; } = 3f;

        [field: SerializeField, FoldoutGroup("설정", true)]
        public string SoundTypeParameterName = "VoiceType";

        [field: SerializeField, FoldoutGroup("설정", true)]
        public DialogTable.DialogSoundType SoundTypeDefault = DialogTable.DialogSoundType.Su;


        private void Awake()
        {
            ResetEvents();
        }

        public void ResetEvents()
        {
            EventCallCount.Clear();
            foreach (var (key, list) in Table.EventRawTable)
            {
                EventCallCount.Add(key, 0);
            }
        }

        
        public void CallEvent(string eventType)
        {
            if (Disabled)
            {
                return;
            }
            if (!EventCallCount.TryGetValue(eventType, out int oldCount))
            {
                Logger.LogWarning($"DialogManager::CallEvent - {eventType}는 존재하지 않는 이벤트 종류입니다.");
                return;
            }

            int newCount = oldCount + 1;
            Logger.Log($"DialogManager::CallEvent({eventType}) called {newCount} times");
            var eventList = Table.EventRawTable[eventType];
            Execute(eventList, ref newCount);
            EventCallCount[eventType] = newCount;
        }
        
        public void ResetEvent(string eventType) {
            Logger.Log($"DialogManager::CallEvent({eventType}) reset");
            EventCallCount[eventType] = 0;
        }    

        
        private void Execute(DialogTable.DialogEventList eventList, ref int count)
        {
            var selection = eventList.SelectOrNull(ref count);
            if (selection == null)
            {
                return;
            }

            switch (selection.Priority)
            {
                // High: 무조건 내가 먼저
                case DialogTable.DialogEventPriority.High:
                    break;
                // Low: 실행중인 이벤트가 있으면 나를 실행하지 않음
                case DialogTable.DialogEventPriority.Low:
                    if (CurrentEvent != null)
                    {
#if UNITY_EDITOR
                        Debug.Log($"DialogManager::Execute selected {selection.KeyDisplay} but ignored by Priority");
#endif
                        return;
                    }
                    break;
            }
#if UNITY_EDITOR
            Debug.Log($"DialogManager::Execute selected {selection.KeyDisplay}({selection.Key})");
#endif

            CurrentEvent = selection;
            EventLeftTime = EventDuration;
            Target.text = selection.Key.GetLocalizedString();
            PlayDialogSound(selection.SoundType);
        }

        public void PlayDialogSound(DialogTable.DialogSoundType soundType)
        {
            float soundTypeParameter = soundType == DialogTable.DialogSoundType.Default
                ? (float)SoundTypeDefault
                : (float)soundType;
            // AudioSource.SetParameter(SoundTypeParameterName, soundTypeParameter);
            AudioSource.PlayOneShot(AudioSource.clip, SoundTypeParameterName, soundTypeParameter);
            UIRenderer.Instance.ShowUI((int)UIObjectType);
        }

        private void Update()
        {
            if (CurrentEvent == null)
            {
                return;
            }
            if (!Target || EventLeftTime <= 0f)
            {
                CurrentEvent = null;
                EventLeftTime = 0f;
                UIRenderer.Instance.HideUI((int)UIObjectType);
                return;
            }

            EventLeftTime -= Time.deltaTime;
        }


        // TODO 코드 자동생성 .. 할 수 있을까?
        public static class BossPhase01
        {
            private const string Prefix = "BossPhase.01.";
            public const string BreakShield = Prefix+"BreakShield";
            public const string Phase1Start = Prefix+"Phase1Start";
            public const string PhaseTransition = Prefix+"PhaseTransition";
            public const string ShootShield = Prefix+"ShootShield";
        }   
        public static class BossPhase02
        {
            private const string Prefix = "BossPhase.02.";
            public const string BreakShield = Prefix+"BreakShield";
            public const string ShootShield = Prefix+"ShootShield";
        }   
        public static class Place
        {
            private const string Prefix = "Place.";
            public const string Start01 = Prefix+"01.Start";
            public const string Start02 = Prefix+"02.Start";
            public const string Start03 = Prefix+"03.Start";
            public const string Start04 = Prefix+"04.Start";
            public const string End01 = Prefix+"01.End";
            public const string End02 = Prefix+"02.End";
            public const string End03 = Prefix+"03.End";
            public const string End04 = Prefix+"04.End";
        }
        public static class SnowRabbit
        {
            private const string Prefix = "SnowRabbit.";
            public const string Dash = Prefix+"Dash";
            public const string Freezing = Prefix+"Freezing";
            public const string IceHammer = Prefix+"IceHammer";
            public const string Timer = Prefix+"Timer";
        }
    }
}