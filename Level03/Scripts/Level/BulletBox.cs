using System;
using Character.Presenter;
using Dummy.Scripts;
using Enemy.UI;
using EnumData;
using Managers;
using Settings.Item;
using Settings.Player;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Level
{
    public class BulletBox : GeneralItem
    {
        [field: SerializeField, BoxGroup("탄환")]
        public ItemDropTable WeaponDropTable { get; private set; }
        
        [field: SerializeField, BoxGroup("탄환")]
        public BulletSettingsByItemTypeTable BulletSettingsByItemTypeTable { get; private set; }

        [field: SerializeField, BoxGroup("연출")]
        private Animator _animator;
        
        [field: SerializeField, BoxGroup("연출")]
        private EffectType _effectOnInteract = EffectType.PlayerItemEat;


        public enum State
        {
            Protected,
            Spawned,
            Opening,
            Awaiting,
        }

        [field: SerializeField, BoxGroup("연출"), ReadOnly]
        private State _state;

        [field: SerializeField, BoxGroup("실드")]
        private ShieldObject _shieldObject;

        [field: SerializeField, BoxGroup("이벤트")]
        public UnityEvent OnInteract;
        
        
        private static readonly int Open = Animator.StringToHash("Open");

        private void OnEnable()
        {
            if (_shieldObject)
            {
                _shieldObject.Initialize();
                _shieldObject.OnBreak.AddListener(OnShieldBreak);
                _state = State.Protected;
            }
            else
            {
                _state = State.Spawned;
            }
        }

        private void OnShieldBreak()
        {
            _state = State.Spawned;
            _shieldObject.OnBreak.RemoveListener(OnShieldBreak);
        }

        protected override bool CanBeSelected(PlayerPresenter player)
        {
            return _state == State.Spawned;
        }

        public override void Interact(PlayerPresenter player)
        {
            // 랜덤 아이템 선택
            var itemType = WeaponDropTable.Get();
            if (itemType == ItemType.None)
            {
                return;
            }
            if(!BulletSettingsByItemTypeTable.Table.TryGetValue(itemType, out var settings))
            {
                Debug.Log($"{name} failed to get bullet from {itemType}");
                return;
            }
            
            // 무기 변경
            player.Model.Magazine = settings.CreateMagazine();

            if (_animator)
            {
                // 애니메이션 호출
                _animator.SetTrigger(Open);

                _state = State.Opening;
            
                OnInteract?.Invoke();
            }
            else
            {
                gameObject.SetActive(false);
            }
            
            PlayInteractionEffect();
        }

        // 애니메이션에서 호출
        public void OnOpeningEnd()
        {
            _state = State.Awaiting;
        }

        // 애니메이션에서 호출
        public void OnWaitEnd()
        {
            gameObject.SetActive(false);
        }

        public void PlayInteractionEffect()
        {
            if (_effectOnInteract == EffectType.None)
            {
                return;
            }

            var effect = EffectManager.Instance.Get(_effectOnInteract);
            effect.transform.position = transform.TransformPoint(effect.transform.position);
        }
    }
}