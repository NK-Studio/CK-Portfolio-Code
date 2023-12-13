#if ENABLE_INPUT_SYSTEM
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NKStudio
{
    public class EventSystemPatch : Editor
    {
        [InitializeOnLoadMethod]
        private static void Trigger()
        {
            ObjectChangeEvents.changesPublished += ChangesPublished;
        }

        static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                ObjectChangeKind type = stream.GetEventType(i);
                switch (type)
                {
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                        stream.GetCreateGameObjectHierarchyEvent(i,
                            out CreateGameObjectHierarchyEventArgs createGameObjectHierarchyEvent);

                        GameObject newGameObject =
                            EditorUtility.InstanceIDToObject(createGameObjectHierarchyEvent.instanceId) as GameObject;

                        if (newGameObject != null)
                        {
                            if (newGameObject.TryGetComponent(out Canvas _))
                                ConvertInputSystemUIModule();
                            else if (newGameObject.TryGetComponent(out Image _))
                                ConvertInputSystemUIModule();
                            else if (newGameObject.TryGetComponent(out RawImage _))
                                ConvertInputSystemUIModule();
                            else if (newGameObject.TryGetComponent(out TMP_Text _))
                                ConvertInputSystemUIModule();
                            else if (newGameObject.TryGetComponent(out Button _))
                                ConvertInputSystemUIModule();
                            else if (newGameObject.TryGetComponent(out Toggle _))
                                ConvertInputSystemUIModule();
                            else if (newGameObject.TryGetComponent(out Slider _))
                                ConvertInputSystemUIModule();
                            else if (newGameObject.TryGetComponent(out Dropdown _))
                                ConvertInputSystemUIModule();
                            else if (newGameObject.TryGetComponent(out InputField _))
                                ConvertInputSystemUIModule();
                            else if (newGameObject.TryGetComponent(out Text _))
                                ConvertInputSystemUIModule();
                            else if (newGameObject.TryGetComponent(out EventSystem _))
                                ConvertInputSystemUIModule();
                        }

                        return;
                }
            }
        }

        private static void ConvertInputSystemUIModule()
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem)
            {
                if (eventSystem.TryGetComponent(out StandaloneInputModule standaloneInputModule))
                {
                    DestroyImmediate(standaloneInputModule);

                    // InputSystemUIInputModule 추가
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
        }
    }
}
#endif