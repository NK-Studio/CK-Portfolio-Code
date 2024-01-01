using Character.Model;
using UniRx;
using UnityEngine;

public class SoulPresenter : MonoBehaviour
{
    private PlayerModel _playerModel;
    private SoulModel _model;
    private SoulView _view;

    private void Awake()
    {
        _view = GetComponent<SoulView>();
        _model = GetComponent<SoulModel>();
        _playerModel = GameObject.FindWithTag("Player").GetComponent<PlayerModel>();
    }

    private void Start()
    {
        _playerModel.SoulObservable
            .Subscribe(value => _view.ChangeThreshold(value))
            .AddTo(this);
    }
}