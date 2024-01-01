using FMODPlus;
using UnityEngine;

namespace Effect
{
    public class StunEffect : MonoBehaviour
    {
        public FMODAudioSource AudioSource;
        public void Stop()
        {
            if (AudioSource)
            {
                AudioSource.Stop();
            }
            // TODO 이후 Opacity 조절해야할듯?
            gameObject.SetActive(false);
        }
    }
}