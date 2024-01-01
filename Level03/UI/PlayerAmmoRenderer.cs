using Character.Core.Weapon;
using Character.Presenter;
using Dummy.Scripts;
using EnumData;
using Managers;
using Settings;
using Sirenix.OdinInspector;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utility;

namespace Enemy.UI
{
    public class PlayerAmmoRenderer : MonoBehaviour
    {
        // TODO 무기 아이콘 변화 필요 예정
        
        private PlayerPresenter _player;
        private CharacterSettings _settings;

        [BoxGroup("탄환 종류 아이콘")]
        public Image BulletTypeIcon;
        [BoxGroup("잔탄수")]
        public TMP_Text AmmoCountText;
        [BoxGroup("잔탄수")]
        public Animator AmmoCountTextAnimator;
        private static readonly int Shoot = Animator.StringToHash("Shoot");
        [BoxGroup("잔탄수/바")]
        public Image AmmoProgressBar;
        [BoxGroup("잔탄수/바")] 
        public Gradient AmmoProgressBarGradientByPercentage;
        [BoxGroup("잔탄수/바")]
        public Image AmmoProgressBarFollowing;
        [BoxGroup("잔탄수/바"), MinMaxSlider(0f, 200f, true)] 
        public Vector2 AmmoProgressBarWidth = new(0f, 140f);
        [BoxGroup("잔탄수/바")]
        public float AmmoProgressBarFollowingSpeed = 10f;
        [BoxGroup("재장전")]
        public Image ReloadProgressBar;
        [BoxGroup("재장전")]
        public Image ReloadProgressBarBackground;
        [BoxGroup("재장전/키")] 
        public GameObject ReloadNotification;

        private bool _initialized = false;
        private void Start()
        {
            _initialized = false;
        }

        private void Update()
        {
            if (!_initialized)
            {
                InitUI();
            }
            RenderAmmo();
        }

        private float _reloadTime;
        private void InitUI()
        {
            _player = GameManager.Instance.Player;
            _settings = ManagerX.AutoManager.Get<GameManager>().Settings;

            _player.Model.MagazineObservable.Subscribe(UpdateBulletTypeIcon).AddTo(this);
        }

        private void UpdateBulletTypeIcon(PlayerBulletMagazine magazine)
        {
            if (magazine == null) magazine = _player.Model.DefaultMagazine;
            BulletTypeIcon.sprite = magazine.Settings.TypeIconSprite;
        }

        private int _lastAmmo = 0;
        private float _followingAmmoNormalizedAmount;

        private void RenderAmmo()
        {
            // AmmoCountText.text = $"<size=64>{_player.Model.Magazine.Ammo} /</size>{_player.Model.Magazine.MaxAmmo}";
            AmmoCountText.text = $"{_player.Model.Magazine.Ammo}";
            if (AmmoProgressBar)
            {
                int currentAmmo = _player.Model.Magazine.Ammo;
                int maxAmmo = _player.Model.Magazine.MaxAmmo;
                float normalizedAmount = (float)currentAmmo / maxAmmo;
                float width = AmmoProgressBarWidth.Lerp(normalizedAmount); 
                AmmoProgressBar.rectTransform.sizeDelta = AmmoProgressBar.rectTransform.sizeDelta.Copy(x: width);
                if (AmmoProgressBarFollowing)
                {
                    if (currentAmmo != _lastAmmo)
                    {
                        _followingAmmoNormalizedAmount = AmmoProgressBarWidth.Lerp((float)(currentAmmo + 1) / maxAmmo);
                        AmmoProgressBarFollowing.color = AmmoProgressBarFollowing.color.Copy(a: 1f);
                        AmmoCountTextAnimator.SetTrigger(Shoot);
                    }
                    float followingWidth = _followingAmmoNormalizedAmount;
                    AmmoProgressBarFollowing.rectTransform.sizeDelta = AmmoProgressBarFollowing.rectTransform.sizeDelta.Copy(
                        x: followingWidth
                    );
                    float followingAlpha = Mathf.Lerp(AmmoProgressBarFollowing.color.a, 0f, AmmoProgressBarFollowingSpeed * Time.deltaTime);
                    AmmoProgressBarFollowing.color = AmmoProgressBarFollowing.color.Copy(a: followingAlpha);
                    _lastAmmo = currentAmmo;
                }

                AmmoProgressBar.color = AmmoProgressBarGradientByPercentage.Evaluate(normalizedAmount);
                // AmmoProgressBar.fillAmount = normalizedAmount;
            }

            var model = _player.Model;
            var state = model.OtherState;
            var isReloading = state == PlayerState.PlayerBulletReload;
            var isOutOfAmmo = model.Magazine.Ammo <= 0;
            ReloadProgressBar.fillAmount = isReloading 
                ? (model.Magazine.ReloadTime / model.Magazine.ReloadTimeDuration) 
                : isOutOfAmmo ? 1f : 0f;
            if (ReloadProgressBarBackground)
            {
                ReloadProgressBarBackground.gameObject.SetActive(isReloading || isOutOfAmmo);
            }

            if (ReloadNotification)
            {
                bool canReload = state is PlayerState.Idle or PlayerState.Shoot 
                                 && model.Magazine == model.DefaultMagazine 
                                 && model.Magazine.Ammo < model.Magazine.MaxAmmo
                                 && model.Magazine.Ammo > 0;
                ReloadNotification.SetActive(canReload);
            }
        }
        
    }
}