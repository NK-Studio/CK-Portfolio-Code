using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Enemy.Behavior.TurretMonster
{
    public class TurretMonsterRangeProjector : MonoBehaviour, IRangeProjector
    {
        [SerializeField] private DecalProjector _outline;
        [SerializeField] private DecalProjector _filler;
        public float Radius
        {
            get => _outline.size.x * 0.5f;
            set => _outline.size = new Vector3(value * 2f, value * 2f, _outline.size.z);
        }
        public float Progress
        {
            set => _filler.size = new Vector3(Radius * 2f * value, Radius * 2f * value, _filler.size.z);
        }

    }
}
