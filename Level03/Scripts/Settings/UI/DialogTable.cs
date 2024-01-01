using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;

#if UNITY_EDITOR
using UnityEditor.Localization;
#endif

namespace Settings.UI
{
    [CreateAssetMenu(fileName = "DialogTable", menuName = "Scriptable Object/Dialog Table", order = 0)]
    public class DialogTable : SerializedScriptableObject
    {
        public enum DialogEventPriority : byte { High, Low, }

        public enum DialogSoundType : byte
        {
            [InspectorName("스")]
            Su = 0,
            [InspectorName("스?")]
            SuWithQuestionMark,
            [InspectorName("스스")]
            SuSu,
            [InspectorName("스!")]
            SuWithExclamationMark,
            [InspectorName("스~")]
            SuWave,
            [InspectorName("스..")]
            Suuu,
            [InspectorName("스슥")]
            SuSuk,
            [InspectorName("스슷")]
            SuSut,
            
            Default = 0xFF
        }
        [Serializable]
        public class DialogEvent
        {
#if UNITY_EDITOR
            [TableColumnWidth(300)]
            public string KeyDisplay;
#endif
            [HideInInspector]
            public LocalizedString Key;
            
            [HideInInspector]
            public string Condition;
            [HideInInspector]
            public string Context;
            [TableColumnWidth(20)]
            public int Count;
            [TableColumnWidth(30)]
            public float Chance;
            [TableColumnWidth(30)]
            public DialogEventPriority Priority;
            [TableColumnWidth(30)]
            public DialogSoundType SoundType;

            private bool ParseAndApply(string rawProperty)
            {
                var split = rawProperty.Split('=');
                if (split.Length < 2)
                {
                    Error($"올바르지 않은 형태(split < 2): {rawProperty}");
                    return false;
                }

                var property = split[0];
                var valueRaw = split[1];
                switch (property)
                {
                    case "Type":
                        return valueRaw == "Dialog";
                    case nameof(Context):
                        Context = valueRaw;
                        return true;
                    case nameof(Condition):
                        Condition = valueRaw;
                        return true;
                    case nameof(Count):
                        if (!int.TryParse(valueRaw, out Count))
                        {
                            Error($"Count가 정수의 형태가 아님: {valueRaw}");
                            return false;
                        }
                        return true;
                    case nameof(Chance):
                        // None 값은 별도로 처리
                        if (valueRaw == "None")
                        {
                            Chance = float.NaN;
                            return true;
                        }
                        if (!float.TryParse(valueRaw, out Chance))
                        {
                            Error($"Chance가 실수의 형태가 아님: {valueRaw}");
                            return false;
                        }
                        return true;
                    case nameof(Priority):
                    {
                        switch (valueRaw)
                        {
                            case "높음":
                                Priority = DialogEventPriority.High;
                                return true;
                            case "낮음":
                                Priority = DialogEventPriority.Low;
                                return true;
                        }
                        Error($"Priority가 유효하지 않음: {valueRaw}");
                        return false;
                    }
                    case nameof(SoundType):
                    {
                        if (valueRaw == "NULL")
                        {
                            SoundType = DialogSoundType.Default;
                            return true;
                        }
                        if (!int.TryParse(valueRaw, out int value))
                        {
                            Error($"SoundType이 정수의 형태가 아님: {valueRaw}");
                            return false;
                        }
                        SoundType = (DialogSoundType)value;
                        return true;
                    }
                }

                Error($"읽을 수 없는 Property 종류: {property}");
                return false;
                
                static void Error(string message)
                {
                    Debug.LogWarning($"DialogEvent::ParseAndApply - {message}");
                }
            }
            
            public static DialogEvent ParseOrNull(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    Error($"{raw} 변환 실패, 스킵");
                    return null;
                }

                var result = new DialogEvent();

                var properties = raw.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var property in properties)
                {
                    if (!result.ParseAndApply(property))
                    {
                        Error($"{raw} 변환 실패, 스킵");
                        return null;
                    }
                }
                return result;

                static void Error(string message)
                {
                    Debug.LogWarning($"DialogEvent::ParseOrNull - {message}");
                }
            }
        }
        
        [Serializable, InlineProperty]
        public class DialogEventList
        {
            public enum Type
            {
                Single,
                Random,
                Sequential,
            }

            [field: SerializeField] 
            public Type EventExecutionType;
            [field: SerializeField] 
            public int EventExecutionTriggerCount;
            [field: TableList(AlwaysExpanded = true)]
            public List<DialogEvent> List = new();
            
            public void PostProcessEventList()
            {
                DetermineEventType();
                EventExecutionTriggerCount = List[0].Count; // TODO Count가 다른 종류로 있는 경우는 상정하지 않음
            }

            private void DetermineEventType()
            {
                // 단일 이벤트
                if (List.Count <= 1)
                {
                    EventExecutionType = Type.Single;
                    return;
                }
                // Chance가 None(NaN): 이벤트 순차 실행 (순회 X)
                if (List.Any(it => float.IsNaN(it.Chance)))
                {
                    EventExecutionType = Type.Sequential;
                    return;
                }
                // 일반적인 경우: 확률에 따른 임의 실행
                EventExecutionType = Type.Random;
            }
            
            public DialogEvent SelectOrNull(ref int count)
            {
                switch (EventExecutionType)
                {
                    case Type.Single:
                        var single = List[0];
                        return EventExecutionTriggerCount <= 1 || count % EventExecutionTriggerCount == 0 ? single : null;
                    case Type.Random:
                        // 아직은 트리거될 차례가 아님
                        if (EventExecutionTriggerCount > 0 && count % EventExecutionTriggerCount != 0)
                        {
                            return null;
                        }
                        return SelectRandomOrNull();
                    case Type.Sequential:
                        // 트리거 횟수가 1 이하인 경우 매번 발동
                        if (EventExecutionTriggerCount <= 1)
                        {
                            return count < List.Count ? List[count] : null; // 리스트 횟수 이내로
                        }
                        // 아직은 트리거될 차례가 아님
                        if (count % EventExecutionTriggerCount != 0)
                        {
                            return null;
                        }
                        // index: count 기준으로 리스트 순서대로 받아내기
                        int index = count / EventExecutionTriggerCount - 1;
                        return index < List.Count ? List[index] : null; // 리스트 횟수 이내로 설정
                }

                return null;
            }

            // Chance의 합이 1이 아닐 수도 있으므로 Null 반환 가능성 있음
            private DialogEvent SelectRandomOrNull()
            {
                float select = UnityEngine.Random.value;
                float currentWeight = 0f;
                foreach (var dialogEvent in List)
                {
                    if (currentWeight <= select && select <= currentWeight + dialogEvent.Chance)
                    {
#if UNITY_EDITOR
                        Debug.Log($"DialogEventList::SelectOrNull - {currentWeight} <= {select} <= {currentWeight + dialogEvent.Chance} :: {dialogEvent.KeyDisplay} SELECTED ({dialogEvent.Chance})");
#endif
                        return dialogEvent;
                    }
                    currentWeight += dialogEvent.Chance;
                }
                Debug.Log($"DialogEventList::SelectOrNull - {select} FAILED");
                return null;
            }
        }

        [field: SerializeField, DictionaryDrawerSettings]
        public Dictionary<string, DialogEventList> EventRawTable { get; private set; } = new();
       
#if UNITY_EDITOR
        [FoldoutGroup("불러오기", true)]
        public StringTableCollection TargetTable;
#endif

#if UNITY_EDITOR
        [FoldoutGroup("불러오기", true)]
        [Button]
        private void ImportFromTable()
        {
            EventRawTable.Clear();
            foreach (var row in TargetTable.GetRowEnumerator())
            {
                Debug.Log($"Importing {row.KeyEntry.Key}({row.KeyEntry.Id})");
                // 메타데이터 얻어오기
                var metadata = row.KeyEntry.Metadata.GetMetadata<Comment>();
                if (metadata == null)
                {
                    continue;
                }

                var raw = metadata.CommentText;
                var parsed = DialogEvent.ParseOrNull(raw);
                if (parsed == null)
                {
                    continue;
                }

                var tableRef = TargetTable.TableCollectionNameReference;
                parsed.Key = new LocalizedString(tableRef, row.KeyEntry.Id);
                parsed.KeyDisplay = row.KeyEntry.Key;

                // var key = parsed.Context+"."+parsed.Condition;
                var key = parsed.Context+"."+parsed.Condition;
                if (!EventRawTable.TryGetValue(key, out var list))
                {
                    EventRawTable.Add(key, list = new DialogEventList());
                }
                
                list.List.Add(parsed);
            }

            foreach (var (key, eventList) in EventRawTable)
            {
                eventList.PostProcessEventList();
            }
        }
#endif
    }
}