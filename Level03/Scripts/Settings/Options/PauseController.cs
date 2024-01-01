using UnityEngine;

namespace Option
{
    public class PauseController : MonoBehaviour
    {
        public Animator[] KonaAnimators;

        public ParticleSystem Effect;

        public void Pause(bool isPause)
        {
            if (isPause)
            {
                foreach (var konaAnimator in KonaAnimators)
                    konaAnimator.speed = 0;

                Effect.Pause();
            }
            else
            {
                foreach (var konaAnimator in KonaAnimators)
                    konaAnimator.speed = 1;

                Effect.Play();
            }
        }
    }
}