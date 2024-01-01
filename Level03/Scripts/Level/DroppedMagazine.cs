using System;
using Character.Core.Weapon;
using Character.Presenter;
using Dummy.Scripts;
using Enemy.UI;
using Settings.Player;
using UnityEngine;

namespace Level
{
    public class DroppedMagazine : GeneralItem
    {
        [field: SerializeField]
        public PlayerBulletSettings DefaultBulletSettings { get; private set; }

        public PlayerBulletMagazine Magazine = null;

        private PlayerPresenter _player;
        private void OnEnable()
        {
            Magazine = DefaultBulletSettings.CreateMagazine();
            _player = FindAnyObjectByType<PlayerPresenter>();
        }

        public override void Interact(PlayerPresenter player)
        {
            // 무기를 새 무기로 설정합니다.
            player.Model.Magazine = Magazine;
            
            // TODO 아이템 획득 시 이펙트, 사운드
            
            gameObject.SetActive(false);
        }
    }
}