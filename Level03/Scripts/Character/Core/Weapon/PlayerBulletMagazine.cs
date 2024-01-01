using System;
using EnumData;
using Level;
using Managers;
using Settings.Player;
using UniRx;
using UnityEngine;
using Object = System.Object;

namespace Character.Core.Weapon
{
    /// <summary>
    /// 플레이어에 Model에 부착되는 탄창 개념
    /// </summary>
    public class PlayerBulletMagazine
    {
        public PlayerBulletMagazine(PlayerBulletSettings settings)
        {
            Settings = settings;
            Reset();
        }
        
        /// <summary>
        /// 무기 설정 종류입니다.
        /// </summary>
        public PlayerBulletSettings Settings { get; private set; }

        /// <summary>
        /// 무기 설정의 최대 장탄수입니다.
        /// </summary>
        public int MaxAmmo => Settings.MaxAmmo;

        /// <summary>
        /// 무기 설정의 발사 간격입니다.
        /// </summary>
        public float CoolTimeDuration => Settings.CoolTime + Settings.AnimationDuration;

        private IntReactiveProperty _ammo = new();
        /// <summary>
        /// 현재 남은 장탄수입니다.
        /// </summary>
        public int Ammo { get => _ammo.Value; set => _ammo.Value = value; }
        public IObservable<int> AmmoObservable => _ammo.AsObservable();
        
        private FloatReactiveProperty _coolTime = new();
        /// <summary>
        /// 현재 남은 쿨타임입니다.
        /// </summary>
        public float CoolTime { get => _coolTime.Value; set => _coolTime.Value = value; }
        public IObservable<float> CoolTimeObservable => _coolTime.AsObservable();

        /// <summary>
        /// 재장전 가능 여부입니다.
        /// </summary>
        public bool CanReload => Settings.ReloadTime > 0f;
        
        /// <summary>
        /// 무기 설정의 재장전 시간입니다.
        /// </summary>
        public float ReloadTimeDuration => Settings.ReloadTime;
        
        /// <summary>
        /// 현재 재장전 시간입니다.
        /// </summary>
        public float ReloadTime { get; set; }

        public void Reset(bool fullAmmo = true)
        {
            Ammo = fullAmmo ? Settings.MaxAmmo : 0;
            CoolTime = 0f;
            ReloadTime = 0f;
        }

        // TODO 풀링
        public DroppedMagazine DropMagazine(Vector3 position)
        {
            if (Settings.Item != ItemType.None)
            {
                var itemObj = ItemManager.Instance.Get(Settings.Item, position, Quaternion.identity);
                var item = itemObj.GetComponent<DroppedMagazine>();
                item.Magazine = this;
                
                if (itemObj.TryGetComponent(out Renderer r))
                {
                    r.material.color = Settings.ItemMaterialTint;
                }

                return item;
            }

            {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.position = position;
                obj.layer = LayerMask.NameToLayer("Item");
                var item = obj.AddComponent<DroppedMagazine>();
                item.Magazine = this;
                if (obj.TryGetComponent(out Renderer r))
                {
                    r.material.color = Settings.ItemMaterialTint;
                }
                return item;
            }
        }
        
    }
}