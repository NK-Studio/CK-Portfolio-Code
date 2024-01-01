using Character.Model;
using Managers;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MoveAxisAdjuster : MonoBehaviour
{
    public Quaternion Rotation = Quaternion.identity;
    
    [ReadOnly] public Vector3 Forward;
    [ReadOnly] public Vector3 Right;

    private PlayerModel _playerModel;
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
    }

    private void Start()
    {
        _playerModel = GameManager.Instance.Player.Model;
        
        this.OnTriggerEnterAsObservable()
            .Where(it => it.CompareTag("Player"))
            .Subscribe(_ =>
            {
                _playerModel.MoveAxisAdjuster = this;
            }).AddTo(this);

        this.OnTriggerExitAsObservable()
            .Where(it => it.CompareTag("Player"))
            .Subscribe(_ =>
            {
                if(_playerModel.MoveAxisAdjuster == this)
                    _playerModel.MoveAxisAdjuster = null;
            }).AddTo(this);
    }

    private void OnValidate()
    {
        Forward = Rotation * Vector3.forward;
        Right = Rotation * Vector3.right;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var origin = transform.position;
        Handles.color = Color.blue;
        Handles.DrawLine(origin, origin + Forward, 5f);
        Handles.color = Color.red;
        Handles.DrawLine(origin, origin + Right, 5f);
    }
#endif
}
