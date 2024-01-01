using System;
using Character.Presenter;
using Enemy.Behavior;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial.Helper
{
    public class TutorialMonsterUIHelper : MonoBehaviour
    {
        public PlayerPresenter Player;
        public Monster TargetMonster;
        public Image TargetImage;

        public Sprite NormalSprite;
        public Sprite LockedSprite;
        public bool AlwaysLockedWhenFrozen = false;

        private void Update()
        {
            if (!Player)
            {
                Player = FindAnyObjectByType<PlayerPresenter>();
            }

            if (!TargetMonster || !TargetImage)
            {
                return;
            }

            if (!TargetMonster.isActiveAndEnabled || TargetMonster.IsFreezeSlipping || TargetMonster.IsFreezeFalling)
            {
                TargetImage.enabled = false;
                return;
            }
            TargetImage.enabled = true;
            
            var locked = Player.Model.HammerDashTarget;
            if (AlwaysLockedWhenFrozen && TargetMonster.IsFreeze || locked && locked.GetInstanceID() == TargetMonster.gameObject.GetInstanceID())
            {
                TargetImage.sprite = LockedSprite;
            }
            else
            {
                TargetImage.sprite = NormalSprite;
            }
        }
    }
}