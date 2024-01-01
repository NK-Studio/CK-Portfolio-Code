using System;
using Character.Presenter;
using Dummy.Scripts;
using Enemy.UI;
using EnumData;
using FMODUnity;
using Managers;
using Settings.Item;
using Settings.Player;
using UnityEngine;

namespace Level
{
    public class HeartItem : GeneralItem
    {
        public EventReference SoundOnInteraction;
        public float Amount = 1f;

        protected override bool CanBeSelected(PlayerPresenter player) 
            => player.Model.Health < player.View.Settings.MaximumHealth 
               && (player.transform.position - transform.position).magnitude <= player.View.Settings.HealthItemRange;

        protected override bool InteractImmediatelyIfCanBeSelected(PlayerPresenter player) => true;

        public override void Interact(PlayerPresenter player)
        {
            PlayInteractionEffect();
            player.Model.Health = Mathf.Min(player.View.Settings.MaximumHealth, player.Model.Health + Amount);
            gameObject.SetActive(false);
            AudioManager.Instance.PlayOneShot(SoundOnInteraction, transform.position);
        }

        public void PlayInteractionEffect()
        {
            EffectManager.Instance.Get(EffectType.PlayerHeartEat).transform.position = transform.position;
        }

        public override void OnStartNearestItem() { }
        public override void OnEndNearestItem() { }
        
    }
}