using AutoManager;
using DG.Tweening;
using GameplayIngredients;
using Managers;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HP
{
    public class HPUI : MonoBehaviour
    {
        private enum HpSpriteType
        {
            Empty,
            Full
        }

        [Title("스프라이트")] [SerializeField] private Sprite[] dalgonaSprites;
        [SerializeField] private Sprite[] hpSprites;

        [Title("이미지 컴포넌트")] [SerializeField] private Image dalgonaImage;
        [SerializeField] private Image[] hpImages;

        private int _dalgonaMax;

        private void Start()
        {
            _dalgonaMax = Manager.Get<GameManager>().characterSettings.DalgonaMax;

            //HP를 관찰하고 UI를 업데이트
            Manager.Get<GameManager>().HPObservable
                .Subscribe(OnRefreshHpUI)
                .AddTo(this);

            //달고나를 관찰하고 UI를 업데이트
            Manager.Get<GameManager>().DalganaObservable
                .Subscribe(i => dalgonaImage.sprite = dalgonaSprites[i])
                .AddTo(this);

            //HP와 달고나 개수를 체크하여 HP 회복
            this.UpdateAsObservable()
                .Where(_ => !Manager.Get<GameManager>().IsHpMax()) //HP가 최대치가 아니고,
                .Where(_ => Manager.Get<GameManager>().Dalgana == _dalgonaMax) // 달고나를 최대로 모았을 경우
                .Subscribe(OnAddHPAndResetDalgona) //HP 회복
                .AddTo(this);
        }

        /// <summary>
        /// 체력 회복
        /// </summary>
        /// <param name="obj"></param>
        private void OnAddHPAndResetDalgona(Unit obj)
        {
            Manager.Get<GameManager>().UpHpAndResetDalgona();
            Messager.Send("HpRecovery");
        }

        /// <summary>
        /// HP UI를 새로고침합니다.
        /// </summary>
        /// <param name="hp"></param>
        private void OnRefreshHpUI(int hp)
        {
            #region 해골 HPUI 렌더링

            //HP 이미지들을 모두 비활성화 상태로 렌더링합니다.
            foreach (Image hpImage in hpImages)
                hpImage.sprite = GetHpSprite(HpSpriteType.Empty);

            //가지고 있는 HP만큼 이미지를 활성화 상태로 렌더링합니다.
            for (int i = 0; i < hp; i++)
                hpImages[i].sprite = GetHpSprite(HpSpriteType.Full);

            #endregion
        }

        /// <summary>
        /// type에 알맞는 Hp 스프라이트를 반환합니다.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private Sprite GetHpSprite(HpSpriteType type)
        {
            return type switch
            {
                HpSpriteType.Empty => hpSprites[0],
                HpSpriteType.Full => hpSprites[1],
                _ => hpSprites[0]
            };
        }

        /// <summary>
        /// UI를 왼쪽으로 숨깁니다.
        /// </summary>
        public void OnTriggerHide()
        {
            ((RectTransform)transform).DOLocalMoveX(-300, 1f).SetRelative(true);
        }

        /// <summary>
        /// UI를 왼쪽으로 숨깁니다.
        /// </summary>
        public void OnTriggerShow()
        {
            ((RectTransform)transform).DOLocalMoveX(300, 1f).SetRelative(true);
        }
    }
}