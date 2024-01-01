using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Utility
{
    public static class Extensions
    {
        private static readonly Random Rand = new Random((int)DateTime.UtcNow.Ticks);
 
        /// <summary>
        /// Fisher-Yates shuffle 사용해 리스트를 제자리에서 섞습니다.
        /// https://ko.wikipedia.org/wiki/%ED%94%BC%EC%85%94-%EC%98%88%EC%9D%B4%EC%B8%A0_%EC%85%94%ED%94%8C
        /// </summary>
        /// <param name="values"></param>
        /// <typeparam name="T"></typeparam>
        public static void Shuffle<T>(this IList<T> values)
        {
            // i: 마지막 index부터 하나씩 내려감
            for (var i = values.Count - 1; i > 0; i--) {
                // k: 0 ~ i까지 랜덤 선택
                var k = Rand.Next(i + 1);
                // i와 k 교환
                (values[k], values[i]) = (values[i], values[k]);
            }
        }

        public static string JoinToString<T>(this IList<T> list, string separator, Func<T, string> transformer)
        {
            return string.Join(separator, list.Select(transformer));
        }
        public static string JoinToString(this IList<string> list, string separator = ", ")
        {
            return string.Join(separator, list);
        }

        /// <summary>
        /// angularSpeed를 기반으로 RotateTowards를 실행합니다.
        /// </summary>
        public static void LookTowards(this Transform t, Vector3 target, float angularSpeed)
        {
            var direction = target - t.position;
            direction.y = 0f; direction.Normalize();
            t.rotation = Quaternion.RotateTowards(
                t.rotation, 
                Quaternion.LookRotation(direction), 
                angularSpeed * Time.deltaTime
            );
        }
        
        /// <summary>
        /// NavMeshAgent의 angularSpeed를 기반으로 RotateTowards를 실행합니다.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="target"></param>
        public static void LookTowards(this NavMeshAgent agent, Vector3 target)
        {
            agent.transform.LookTowards(target, agent.angularSpeed);
        }

        public static Keyframe Last(this AnimationCurve curve)
        {
            return curve[curve.length - 1];
        }
        
        
        public static float GetLength(this AnimationCurve curve)
        {
            return curve.Last().time;
        }
        
        public static T GetOrAddComponent<T>(this GameObject g) where T : Component
        {
            if (g.TryGetComponent(out T t))
            {
                return t;
            }

            t = g.AddComponent<T>();
            return t;
        }

        public static bool IsNull<T>(this T obj) where T : Object
        {
            return ReferenceEquals(obj, null);
        }
        public static bool IsNotNull<T>(this T obj) where T : Object
        {
            return !obj.IsNull();
        }

        public static bool IsEmpty<T>(this ICollection<T> c)
        {
            return c.Count <= 0;
        }

        public static Color Copy(this Color c, 
            float r = float.NaN, 
            float g = float.NaN, 
            float b = float.NaN,
            float a = float.NaN
        )
        {
            c.r = float.IsNaN(r) ? c.r : r;  
            c.g = float.IsNaN(g) ? c.g : g;  
            c.b = float.IsNaN(b) ? c.b : b;  
            c.a = float.IsNaN(a) ? c.a : a;  
            return c;
        }

        /// <summary>
        /// Vector2Int의 x, y를 각각 최소, 최대값으로 이루어진 정수 범위로 간주하여 그 범위 내의 임의의 정수를 뽑습니다. 
        /// </summary>
        /// <param name="range">[x, y]의 범위로 간주되는 정수 2차원 벡터입니다. 최소와 최대 모두 포함(inclusive)합니다.</param>
        /// <returns></returns>
        public static int Random(this in Vector2Int range)
        {
            return UnityEngine.Random.Range(range.x, range.y + 1);
        }

        /// <summary>
        /// Vector2의 x, y를 각각 최소, 최대값으로 이루어진 실수 범위로 간주하여 그 범위 내의 임의의 실수를 뽑습니다. 
        /// </summary>
        /// <param name="range">[x, y]의 범위로 간주되는 2차원 벡터입니다.</param>
        /// <returns></returns>
        public static float Random(this in Vector2 range)
        {
            return UnityEngine.Random.Range(range.x, range.y);
        }

        /// <summary>
        /// 리스트 내의 원소를 임의로 하나 구합니다.
        /// </summary>
        /// <param name="list">대상 임의 접근 리스트입니다.</param>
        /// <returns>UnityEngine.Random을 사용해 임의로 하나 추출합니다.</returns>
        public static T Random<T>(this List<T> list)
        {
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Vector2를 일종의 Range로 판단해 Lerp(x=a, y=b, t)를 수행합니다.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float Lerp(this in Vector2 range, float t)
        {
            return Mathf.Lerp(range.x, range.y, t);
        }

        public static float Squared(this in float f) => f * f;
    }
}