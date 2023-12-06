
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace SlidePuzzle
{
    public class PuzzleTrigger : MonoBehaviour
    {
        private enum PuzzleDoorType
        {
            None,
            Entrance,
            Exit
        }
        
        [SerializeField, ValidateInput("@AISpawner != null", "AI Spawner가 비어있습니다")]
        private GameObject AISpawner;

        [SerializeField, Tooltip("퍼즐의 입구/출구의 역할을 지정합니다.")]
        private PuzzleDoorType DoorType;

        [Inject]
        private SlidePuzzleSystem _puzzleSystem;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Player")) return;
            
            if (DoorType == PuzzleDoorType.Entrance)
                if (_puzzleSystem.PlayState == SlidePuzzleSystem.PuzzlePlayState.None)
                    _puzzleSystem.OnTriggerPuzzleMode();
        }
        
        [Button("Auto Binding", ButtonSizes.Large), PropertySpace(20)]
        private void AutoBinding()
        {
            AISpawner = GameObject.Find("AI Spawner");
            _puzzleSystem = FindObjectOfType<SlidePuzzleSystem>();
        }
    }
}