using UnityEngine;
using UnityEngine.AI;

namespace Utility
{
    public class NavMeshHandler
    {
        
        public LayerMask LayerMask { get; set; } = new();
        
        /// <summary>
        /// origin에서 target까지 직선으로 이동할 수 있는지 검사합니다. SamplePosition, Raycast를 수행합니다.
        /// <list type="number">
        ///     <item><description>SamplePosition에서 해당 목표 위치에 유효한 NavMesh 점이 있는지 검사합니다. -> 없으면 false</description></item>
        ///     <item><description>해당 방향으로 Raycast하여 직선 거리에 부딪히는 경계선이 있으면 해당 점으로, 없으면 목표 지점으로 그대로 이동합니다.</description></item>
        /// </list>
        /// SampleDistance는 (target-origin).magnitude로 합니다.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <param name="calculatedPosition"></param>
        /// <returns></returns>
        public bool GetStraightMovablePosition(Vector3 origin, Vector3 target, out Vector3 calculatedPosition, bool slideOnEdge = false)
        {
            var originToTarget = target - origin;
            var distance = originToTarget.magnitude;
            return GetStraightMovablePosition(origin, target, out calculatedPosition, distance, slideOnEdge);
        }
        
        /// <summary>
        /// origin에서 target까지 직선으로 이동할 수 있는지 검사합니다. SamplePosition, Raycast를 수행합니다.
        /// <list type="number">
        ///     <item><description>SamplePosition에서 해당 목표 위치에 유효한 NavMesh 점이 있는지 검사합니다. -> 없으면 false</description></item>
        ///     <item><description>해당 방향으로 Raycast하여 직선 거리에 부딪히는 경계선이 있으면 해당 점으로, 없으면 목표 지점으로 그대로 이동합니다.</description></item>
        /// </list>
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        /// <param name="calculatedPosition"></param>
        /// <param name="sampleDistance">SamplePosition을 수행할 거리입니다.</param>
        /// <returns></returns>
        public bool GetStraightMovablePosition(Vector3 origin, Vector3 target, out Vector3 calculatedPosition, float sampleDistance, bool slideOnEdge = false)
        {
            // 가장 가까운 점 구하기: (플레이어 위치 - 목표점).magnitude 니까 실패하기 힘듦
            if (!NavMesh.SamplePosition(target, out var sampleHit, sampleDistance, LayerMask))
            {
                // Sample을 실패하면 .,.. 너도 움직이지 마
                calculatedPosition = Vector3.zero;
                return false;
            }

            // 가장 가까운 점
            calculatedPosition = sampleHit.position;

            // 플레이어 위치에서 목표 방향으로 NavMesh.Raycast
            if (NavMesh.Raycast(origin, calculatedPosition, out var rayHit, LayerMask))
            {
                // 미끄러져야 하는 경우
                if (slideOnEdge)
                {
                    // 넘어간 양 만큼의 벡터
                    var overed = (calculatedPosition - rayHit.position);
                    // normal 기반 직선에 투영
                    var slided = overed + rayHit.normal * (Vector3.Dot(-overed, rayHit.normal));
                    calculatedPosition = rayHit.position + slided;
                }
                // 아니면 edge에 닿으면 해당 edge로 설정
                else
                {
                    calculatedPosition = rayHit.position;
                }
            }
            // edge 안 닿았어도 그냥 그대로 감
            return true;
        }
        
        /// <summary>
        /// origin에서 target까지 이동할 수 있는지 검사합니다. SamplePosition, CalculatePath, Raycast를 수행합니다.
        /// <list type="number">
        ///     <item><description>SamplePosition에서 해당 목표 위치에 유효한 NavMesh 점이 있는지 검사합니다. -> 없으면 false</description></item>
        ///     <item><description>CalculatePath에서 해당 목표 NavMesh까지 이동할 수 있는 장소인지 (막혀있지 않은지) -> 이동할 수 있으면 true</description></item>
        ///     <item><description>CalculatePath 실패 시 첫 SamplePosition 위치를 갈 수 없다는 뜻이므로, 대신 해당 방향으로 Raycast하여 직선 거리에 부딪히는 벽을 찾아 이동 -> 이것도 못하면 false</description></item>
        /// </list>
        /// SampleDistance는 (target-origin).magnitude로 합니다.
        /// </summary>
        /// <param name="origin">출발점입니다.</param>
        /// <param name="target">도착점입니다.</param>
        /// <param name="calculatedPosition">결과 위치 벡터입니다.</param>
        /// <returns></returns>
        public bool GetMovablePosition(Vector3 origin, Vector3 target, out Vector3 calculatedPosition)
        {
            var originToTarget = target - origin;
            var distance = originToTarget.magnitude;
            return GetMovablePosition(origin, target, out calculatedPosition, distance);
        }

        private NavMeshPath _dummyPath;
        /// <summary>
        /// origin에서 target까지 이동할 수 있는지 검사합니다. SamplePosition, CalculatePath, Raycast를 수행합니다.
        /// <list type="number">
        ///     <item><description>SamplePosition에서 해당 목표 위치에 유효한 NavMesh 점이 있는지 검사합니다. -> 없으면 false</description></item>
        ///     <item><description>CalculatePath에서 해당 목표 NavMesh까지 이동할 수 있는 장소인지 (막혀있지 않은지) -> 이동할 수 있으면 true</description></item>
        ///     <item><description>CalculatePath 실패 시 첫 SamplePosition 위치를 갈 수 없다는 뜻이므로, 대신 해당 방향으로 Raycast하여 직선 거리에 부딪히는 벽을 찾아 이동 -> 이것도 못하면 false</description></item>
        /// </list>
        /// </summary>
        /// <param name="origin">출발점입니다.</param>
        /// <param name="target">도착점입니다.</param>
        /// <param name="calculatedPosition">결과 위치 벡터입니다.</param>
        /// <param name="sampleDistance">SamplePosition에서 사용되는 maxDistance입니다.</param>
        /// <returns></returns>
        public bool GetMovablePosition(
            Vector3 origin, 
            Vector3 target, 
            out Vector3 calculatedPosition,
            float sampleDistance)
        {
            // 가장 가까운 점 구하기: (플레이어 위치 - 목표점).magnitude 니까 실패하기 힘듦
            if (!NavMesh.SamplePosition(target, out var sampleHit, sampleDistance, LayerMask))
            {
                // Sample을 실패하면 .,.. 너도 움직이지 마
                calculatedPosition = Vector3.zero;
                return false;
            }

            calculatedPosition = sampleHit.position;

            // 더미 패스 만들기
            _dummyPath ??= new NavMeshPath();

            // 실제로 플레이어가 이동할 수 있는 곳인가? -> 그렇다면 OK
            if (NavMesh.CalculatePath(origin, calculatedPosition, LayerMask, _dummyPath)
                && (calculatedPosition - _dummyPath.corners[^1]).sqrMagnitude <= Vector3.kEpsilon
            ) {
                return true;
            }

            // 플레이어가 직접 이동할 수 없는 곳일 경우 ... (공간이 나뉜 NavMesh인 경우)
            // 플레이어 위치에서 목표 방향으로 NavMesh.Raycast
            // NavMesh.Raycast는 NavMesh상에서 Ray가 목표지점에 도달하기 전에 Edge에 닿으면 true 반환
            // -> 일직선상으로 갈 수 있는, 즉 벽에 부딪히는 효과
            // Raycast 실패하는 경우는 왠만해서는 드물 것
            if (!NavMesh.Raycast(origin, calculatedPosition, out var rayHit, LayerMask))
            {
                return false;
            }

            calculatedPosition = rayHit.position;
            return true;
        }
    }
}