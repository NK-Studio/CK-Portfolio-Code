using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Micosmo.SensorToolkit;
using Micosmo.SensorToolkit.BehaviorDesigner;
using UnityEngine;

namespace Utility
{
    [TaskCategory("SensorToolkit")]
    [TaskIcon("Assets/Gizmos/SensorToolkit/RAY.png")]
    [TaskDescription("IsObstructed를 지정된 Layer 검사와 함께 수행합니다.")]
    public class IsObstructedWithLayer : IsObstructed
    {

        public SharedLayerMask layerTest;
        
        public override TaskStatus OnUpdate() {
            var actualSensor = (sensor?.Value as IRayCastingSensor);

            if (actualSensor == null) {
                return TaskStatus.Failure;
            }

            var hit = actualSensor.GetObstructionRayHit();
            if (!hit.IsObstructing) {
                return TaskStatus.Failure;
            }

            // 지정된 레이어인가?
            if (((1 << hit.GameObject.layer) & layerTest.Value.value) == 0)
            {
                return TaskStatus.Failure;
            }

            return TaskStatus.Success;
        }
    }
}