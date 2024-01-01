using EnumData;
using Managers;
using UnityEngine;

namespace Effect
{
    public class EffectSpawner : MonoBehaviour
    {
        public EffectType Effect;
        public void Spawn()
        {
            if (Effect == EffectType.None)
            {
                return;
            }
            var effect = EffectManager.Instance.Get(Effect);
            var effectTransform = effect.transform;
            var position = transform.TransformPoint(effectTransform.position);
            var rotation = transform.rotation * effectTransform.rotation;
            effect.transform.SetPositionAndRotation(position, rotation);
        }
    }
}