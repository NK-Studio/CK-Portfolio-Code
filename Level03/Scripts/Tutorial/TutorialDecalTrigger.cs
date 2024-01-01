using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tutorial
{
    public class TutorialDecalTrigger : MonoBehaviour
    {
        private static readonly int InnerCircleOffset = Shader.PropertyToID("_Inner_Circle_Offset");
        
        public DecalProjector Projector;

        public float FadeOutDuration = 0.5f;
        public float ExpandDuration = 1f;
        
        public float FadeInDuration = 0.5f;
        // public float ContractDuration = 1f;

        private float GetFade() => Projector.fadeFactor;
        private void SetFade(float value) => Projector.fadeFactor = value;

        private float GetRadius() => Projector.material.GetFloat(InnerCircleOffset);
        private void SetRadius(float value) => Projector.material.SetFloat(InnerCircleOffset, value);
        
        public void StartDecal()
        {
            Projector.gameObject.SetActive(false);
            Projector.gameObject.SetActive(true);
            SetFade(0f);
            DOTween.To(GetFade, SetFade, 1f, FadeOutDuration).Play();
            SetRadius(0f);
            DOTween.To(GetRadius, SetRadius, 1f, ExpandDuration).Play();
        }

        public async UniTask EndDecal()
        {
            Projector.gameObject.SetActive(false);
            Projector.gameObject.SetActive(true);
            SetFade(1f);
            SetRadius(1f);
            await DOTween.To(GetFade, SetFade, 0f, FadeInDuration);
        }
    }
}