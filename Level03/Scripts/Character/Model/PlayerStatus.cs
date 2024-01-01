using System;
using Character.Core.Weapon;
using Dummy.Scripts;
using Managers;

namespace Character.Model
{
    public class PlayerStatus
    {
        public float Health = float.NaN;
        public PlayerBulletMagazine Magazine = null;
        public void Save(PlayerModel player)
        {
            Health = player.Health;
            Magazine = player.RawMagazine;
        }
        public void Apply(PlayerModel player)
        {
            player.Health = float.IsNaN(Health) ? GameManager.Instance.Settings.MaximumHealth : Health;
            player.Magazine = Magazine;
        }

        public void Reset()
        {
            Health = float.NaN;
            Magazine = null;
        }
    }
}