using AutoManager;
using GameplayIngredients;
using Managers;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Items
{
    public class Dalgona : MonoBehaviour
    {
        private void Start()
        {
        
            transform.GetChild(0)
                .OnTriggerEnterAsObservable()
                .Where(coll => coll.CompareTag("Player"))
                .Subscribe(OnGetItem)
                .AddTo(this);
        }

        private void OnGetItem(Collider _)
        {
            int dalgonaMax = Manager.Get<GameManager>().characterSettings.DalgonaMax;
            int nowDalgana = Manager.Get<GameManager>().Dalgana;
            
            if (nowDalgana >= dalgonaMax)
                return;
        
            Messager.Send("GetItem");
            Manager.Get<GameManager>().Dalgana += 1;
            Destroy(gameObject);
        }
    }
}