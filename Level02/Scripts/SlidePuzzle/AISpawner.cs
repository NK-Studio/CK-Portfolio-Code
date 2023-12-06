using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

// 클릭할때마다 네비게이션 AI 출격
namespace SlidePuzzle
{
    public class AISpawner : MonoBehaviour
    {
        [Title("프리팹"), SerializeField, ValidateInput("@AIPrefab != null", "AI Prefab이 비어있습니다.")]
        private GameObject AIPrefab;

        [Title("생성 주기")] public float SpawnDelayTime = 1f; // 스폰 지연 시간
        private float _spawnDelayTime;

        [Title("부모 계층")] public Transform SpawnParent;

        [Inject] private SlidePuzzleSystem _slidePuzzleSystem;

        [Inject]
        private DiContainer _container;
        
        private void Update()
        {
            if (_slidePuzzleSystem.PlayState != SlidePuzzleSystem.PuzzlePlayState.Play) return;

            _spawnDelayTime += Time.unscaledDeltaTime;

            if (_spawnDelayTime > SpawnDelayTime)
            {
                CreateAI();
                _spawnDelayTime = 0;
            }
        }


        [Button("생성")]
        public void CreateAI()
        {
            _container.InstantiatePrefab(AIPrefab, transform.position, Quaternion.identity, SpawnParent);
        }
    }
}