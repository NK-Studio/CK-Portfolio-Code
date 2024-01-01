using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class QuestObject : MonoBehaviour
{
    [SerializeField] [Header("출력시킬 퀘스트 Index")]
    private int _questIndex = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            TriggerQuest();
    }

    /// <summary>
    /// 퀘스트를 트리거합니다.
    /// </summary>
    private void TriggerQuest()
    {
        QuestRenderer.Instance.UpdateLeftUI(_questIndex);
        Destroy(gameObject);
    }
}