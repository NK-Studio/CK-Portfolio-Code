#if ENABLE_INPUT_SYSTEM
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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
                            EventSystem eventSystem = newGameObject.GetComponent<EventSystem>();
                            if (eventSystem)
                            {
                                // 기존에 있는 StandaloneInputModule는 제거
                                StandaloneInputModule standaloneInputModule =
                                    newGameObject.GetComponent<StandaloneInputModule>();

                                DestroyImmediate(standaloneInputModule);

                                // InputSystemUIInputModule 추가
                                newGameObject.AddComponent<InputSystemUIInputModule>();
                            }
                        }

                        return;
                }
            }
        }
    }
}
#endif