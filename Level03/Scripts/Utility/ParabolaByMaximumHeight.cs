using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Utility
{
    [Serializable]
    public struct ParabolaByMaximumHeightGenerator
    {
        public Vector3 Start;
        public Vector3 End;
        public float HighestFromLeap;

        public ParabolaByMaximumHeight Parabola;

        [Button]
        public void Generate()
        {
            Parabola = new ParabolaByMaximumHeight(Start, End, HighestFromLeap);
        }

    }

    [Serializable]
    public struct ParabolaByMaximumHeight
    {
        public Vector3 Start;
        public Vector3 End;
        public float A; // a
        public float P; // p

        public Vector3 Direction;
        public Vector3 HorizontalDirection;
        public float HorizontalLength;
        public bool Valid;
    
        /// <summary>
        /// 월드 공간 기준 start에서 end로 향하는 포물선을 정의합니다.
        /// 수식은 https://www.desmos.com/calculator/wejssotbxw?lang=ko를 참조하세요.
        /// </summary>
        /// <param name="start">포물선이 시작하는 월드 좌표입니다. 수식에서의 원점입니다.</param>
        /// <param name="end">포물선이 도달하는 월드 좌표입니다. 수식에서의 (x1, y1)입니다.</param>
        public ParabolaByMaximumHeight(Vector3 start, Vector3 end, float highest) {
            Start = start;
            End = end;
            Vector3 between = end - start;
            Direction = between.normalized;
            HorizontalDirection = new Vector3(between.x, 0, between.z);
            HorizontalDirection.Normalize();
            // 순수 y축
            float y1 = between.y;
        
            // xz평면의 크기
            between.y = 0;
            float x1 = HorizontalLength = between.magnitude;

            if (x1 == 0) {
                DebugX.LogWarning("ParabolaByMaximumHeight의 상대좌표의 x축 값이 0입니다 !!!");
                A = 1f;
                P = 0f;
                Valid = false;
            }
            else
            {
                float rawK = highest;
                float k = Mathf.Abs(rawK);
                float m = y1 / x1;
                float a = A = (rawK >= 0 ? 1f : -1f) * ((y1 - 2 * (k + Mathf.Sqrt(k * (k - y1)))) / (x1 * x1));
                P = x1 - m / a;
                Valid = true;
            }
        }

        /// <summary>
        /// 월드 공간 기준 start에서 end로 향하는 포물선을 정의합니다.
        /// 수식은 https://www.desmos.com/calculator/wejssotbxw?lang=ko를 참조하세요.
        /// </summary>
        /// <param name="start">포물선이 시작하는 월드 좌표입니다. 수식에서의 원점입니다.</param>
        /// <param name="end">포물선이 도달하는 월드 좌표입니다. 수식에서의 (x1, y1)입니다.</param>
        /// <param name="distanceFromOrigin">수식에서의 l값입니다.</param>
        /// <param name="distanceX">수식에서의 q값입니다.</param>
        public ParabolaByMaximumHeight(Vector3 start, Vector3 end, float distanceFromOrigin, float distanceX) {
            Start = start;
            End = end;
            Vector3 between = end - start;
            Direction = between.normalized;
            HorizontalDirection = new Vector3(between.x, 0, between.z);
            HorizontalDirection.Normalize();
            // 순수 y축
            float y1 = between.y;
        
            // xz평면의 크기
            between.y = 0;
            float x1 = HorizontalLength = between.magnitude;

            if (x1 == 0) {
                DebugX.LogWarning("RopeParabolaInfo의 상대좌표의 x축 값이 0입니다 !!!");
                A = 1f;
                P = 0f;
                Valid = false;
            }
            else {
                float rawK = Mathf.Max(0, y1) + distanceFromOrigin / (Mathf.Abs(y1) - distanceX);
                float k = Mathf.Abs(rawK);
                float m = y1 / x1;
                float a = A = (k >= 0 ? 1f : -1f) * ((y1 - 2 * (k + Mathf.Sqrt(k * (k - y1)))) / (x1 * x1));
                P = x1 - m / a;
                Valid = true;
            }
        }

        /// <summary>
        /// 포물선의 특정 x값의 y좌표를 구합니다.
        /// </summary>
        /// <param name="relativeX"></param>
        /// <returns></returns>
        public float GetRelativeY(float relativeX) => A * relativeX * (relativeX - P);

        // (worldPosition - Start) -> 시작점에서 worldPosition으로 향하는 벡터
        // 내적 -> _horizontalDirection이 이루는 직선에 투영
        // -> 직선 상에 Start 기준으로 어디에 있는지?
        /// <summary>
        /// 월드 좌표를 포물선 상의 X축 상대좌표로 변환합니다.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public float GetRelativeX(Vector3 worldPosition) {
            float depth = Vector3.Dot(HorizontalDirection, worldPosition - Start);
            // DebugX.Log($"GetRelativeX({worldPosition}) => {depth:F5} (x1: {HorizontalLength})");
            return depth;
        }
        // 
        /// <summary>
        /// 월드 좌표에서 변환한 X축 상대좌표를 수평 길이로 나눠서 [0, 1]로 정규화합니다.
        /// 이 함수를 통해 이 포물선에서 해당 위치가 포물선의 얼만큼인지를 구할 수 있습니다.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public float GetRelativeXPercentage(Vector3 worldPosition) => Mathf.Clamp01(GetRelativeX(worldPosition) / HorizontalLength);
        public float GetYFromWorld(Vector3 worldPosition) {
            return GetRelativeY(GetRelativeX(worldPosition));
        }

        /// <summary>
        /// [0, 1]의 값을 통해 포물선 궤적 상 어느 한 점의 절대좌표를 구합니다. 
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public Vector3 GetPosition(float percentage) {
            var normalized = Mathf.Clamp01(percentage);
            Vector3 position = Start + HorizontalDirection * (normalized * HorizontalLength);
            position.y += GetRelativeYFromPercentage(normalized);
            return position;
        }

        /// <summary>
        /// 실제 상대 x값을 통해 포물선 궤적 상 어느 한 점의 절대좌표를 구합니다.
        /// </summary>
        /// <param name="relativeX"></param>
        /// <returns></returns>
        public Vector3 GetPositionByRelativeX(float relativeX)
        {
            Vector3 position = Start + HorizontalDirection * relativeX;
            position.y += GetRelativeY(relativeX);
            return position;
        }

        /// <summary>
        /// [0, 1]의 값을 통해 포물선 궤적 상 어느 한 점의 y축 좌표를 구합니다.
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public float GetRelativeYFromPercentage(float percentage) => GetRelativeY(percentage * HorizontalLength);
    }
}