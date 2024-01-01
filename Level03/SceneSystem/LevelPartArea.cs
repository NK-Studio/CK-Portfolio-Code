using Character.Presenter;
using ManagerX;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace SceneSystem
{
    public class LevelPartArea : MonoBehaviour
    {
        [field: SerializeField] public bool ShouldLoad { get; private set; } = false;
        [field: SerializeField, ReadOnly] public bool IsLoaded { get; private set; } = false;

        public UnityEvent OnLoaded;
        public UnityEvent OnUnloaded;

        [SerializeField]
        private bool showDebug;

        private bool _isInitialized;
        private PlayerPresenter _player;

        public void Initialize(PlayerPresenter player)
        {
            _player = player;

            // 현재 로드된 씬 있으면 IsLoaded = true
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.name == TargetScene.Name)
                {
                    if (showDebug)
                        DebugX.Log($"{TargetScene.Name} Already Scene Loaded, IsLoaded = true");
                    ShouldLoad = true;
                    IsLoaded = true;
                    OnLoaded?.Invoke();
                    break;
                }
            }

            _isInitialized = true;
        }

        //private static bool IsLevelLoading => AutoManager.Get<SceneController>().IsLoading;

        private void Update()
        {
            if (!_isInitialized) return;
            //if (IsLevelLoading) return;

            if (ShouldLoad)
            {
                LoadScene();
            }
            else
            {
                UnloadScene();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // if (!IsLevelLoading && other.CompareTag("Player"))
            // {
            //     if(showDebug)
            //         DebugX.Log($"Player Entered to {TargetScene.Name}");
            //     ShouldLoad = true;
            // }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if(showDebug)
                    DebugX.Log($"Player Exited to {TargetScene.Name}");
                ShouldLoad = false;
            }
        }

        private void LoadScene()
        {
            if (IsLoaded) return;

            if(showDebug)
                DebugX.Log($"{TargetScene.Name} LoadScene() called by trigger");
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(TargetScene.Name, LoadSceneMode.Additive);
            IsLoaded = true;
            OnLoaded?.Invoke();
        }

        private void UnloadScene()
        {
            if (!IsLoaded) return;

            if (showDebug)
                DebugX.Log($"{TargetScene.Name} UnloadScene() called by trigger");
            var operation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(TargetScene.Name);
            
            if(showDebug)
                operation.completed += _ =>
                {
                    DebugX.Log($"{TargetScene.Name} Unload Completed");
                };

            IsLoaded = false;
            OnUnloaded?.Invoke();
        }

        [field: SerializeField] public SceneReference TargetScene { get; private set; }
#if UNITY_EDITOR
        [Button, DisableInPlayMode]
        private void OpenTargetScene()
        {
            var path = TargetScene.Path;

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.name == TargetScene.Name)
                {
                    if (showDebug)
                        DebugX.LogWarning($"{path}(은)는 이미 열려있는 씬입니다.");
                    return;
                }
            }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        [Button, DisableInPlayMode]
        private void CloseTargetScene()
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.name == TargetScene.Name)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    return;
                }
            }

            if (showDebug)
                DebugX.LogWarning($"{TargetScene.Name}(은)는 열려있지 않은 씬입니다.");
        }
#endif
    }
}