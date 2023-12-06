using System.Threading;
using AutoManager;
using Character.Model;
using Managers;
using SaveLoadSystem;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class FindCutSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject FindCutScene;

    private bool _isNotPlay;

    private PlayerModel _playerModel;

    private void Awake()
    {
        _playerModel = FindObjectOfType<PlayerModel>();
    }

    private void Start()
    {
        Vector3 candyInfo = Manager.Get<DataManager>().Load("Stage2Data", Vector3.zero);

        if (candyInfo != Vector3.zero)
            _isNotPlay = true;

        //컷씬이 실행되면 캐릭터를 멈추고, 공격을 받지 않도록 한다.
        this.UpdateAsObservable()
            .Where(_ => FindCutScene.activeSelf)
            .Subscribe(_ =>
            {
                //플레이를 시작하면 다음에는 시작하지 못하도록 트리거를 겁니다.
                _isNotPlay = true;
                
                _playerModel.IsStop = true;
                Manager.Get<GameManager>().IsNotAttack = true;
            })
            .AddTo(this);

        //Find컷씬의 액티브 상태를 트래킹해서, 비활성화될 때 한번 실행한다.
        this.UpdateAsObservable()
            .ObserveEveryValueChanged(_ => FindCutScene.activeSelf)
            .Where(active => !active)
            .Subscribe(_ =>
            {
                _playerModel.IsStop = false;
                Manager.Get<GameManager>().IsNotAttack = false;
            })
            .AddTo(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isNotPlay) return;

        if (other.CompareTag("Player"))
            FindCutScene.SetActive(true);
    }
}