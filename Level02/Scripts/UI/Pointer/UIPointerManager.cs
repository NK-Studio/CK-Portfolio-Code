using System;
using AutoManager;
using DG.Tweening;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Pointer
{
    public class UIPointerManager : MonoBehaviour
    {
        public enum MoveStyle
        {
            Horizontal,
            Vertical
        }

        public enum AnimationStyle
        {
            Move,
            AlphaAppear
        }

        [Title("해골 포인터 이동")] [SerializeField] private Image pointImage;
        [SerializeField] private RectTransform[] buttons;
        [SerializeField] private Ease ease = Ease.InOutCubic;
        [SerializeField] private float duration = 0.3f;

        [Title("애니메이션 스타일")] [SerializeField] private MoveStyle moveStyle = MoveStyle.Vertical;
        [SerializeField] private AnimationStyle animationStyle = AnimationStyle.Move;
        [SerializeField] private Vector2 offset = Vector2.zero;
        [SerializeField] private RectTransform[] pointCoordinate;
        [SerializeField] private bool ignoreTimeScale;

        [Title("사운드")] [SerializeField] private EventReference[] SFX;

        public void MovePoint(int index)
        {
            switch (animationStyle)
            {
                case AnimationStyle.Move:
                {
                    //포인트의 위치를 기록
                    Vector2 pos = pointImage.rectTransform.anchoredPosition;

                    //수직이면 수직으로만 이동한다.
                    if (moveStyle == MoveStyle.Vertical)
                        pos.y = buttons[index].anchoredPosition.y + offset.y;
                    //수평이면 수평으로만 이동한다.
                    else
                        pos.x = buttons[index].anchoredPosition.x + offset.x;

                    //이동
                    pointImage.rectTransform.DOLocalMove(pos, duration).SetEase(ease).SetUpdate(ignoreTimeScale);
                    break;
                }
                case AnimationStyle.AlphaAppear:
                {
                    pointImage.rectTransform.position = pointCoordinate[index].position;

                    Color initColor = pointImage.color;
                    initColor.a = 0f;
                    pointImage.color = initColor;
                    pointImage.DOFade(1, duration).SetEase(ease).SetUpdate(ignoreTimeScale);
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            pointImage.DOKill();
        }

        /// <summary>
        /// 마우스가 닿았을 때 사운드를 재생합니다.
        /// </summary>
        public void PlayEnterSound()
        {
            Manager.Get<AudioManager>().PlayOneShot(SFX[0]);
        }

        /// <summary>
        /// 클릭했을 때 사운드를 재생합니다.
        /// </summary>
        public void PlayClickSound()
        {
            Manager.Get<AudioManager>().PlayOneShot(SFX[1]);
        }
    }
}