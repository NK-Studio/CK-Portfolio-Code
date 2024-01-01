#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEditor.Splines;
using UnityEngine.Splines;

namespace NKStudio
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SplineInstantiate))]
    public class MakeChainModule : MonoBehaviour
    {
        public SplineInstantiate SplineItemGenerate;

        [Space(10), Tooltip("간격")] [Min(0.1f)] public float Spacing = 0.5f;

        private void Update()
        {
            if (Application.isEditor)
                UpdateSpacing();
        }

        /// <summary>
        /// 간격을 업데이트 합니다.
        /// </summary>
        private void UpdateSpacing()
        {
            if (SplineItemGenerate)
            {
                SplineItemGenerate.MinSpacing = Spacing;
                SplineItemGenerate.MaxSpacing = Spacing;
                SplineItemGenerate.UpdateInstances();
                TurnChain();
            }
        }

        private void OnEnable()
        {
            if (Application.isEditor)
                EditorSplineUtility.AfterSplineWasModified += EditorSplineUtilityOnAfterSplineWasModified;
        }

        private void OnDisable()
        {
            if (Application.isEditor)
                EditorSplineUtility.AfterSplineWasModified -= EditorSplineUtilityOnAfterSplineWasModified;
        }

        private void EditorSplineUtilityOnAfterSplineWasModified(Spline spline)
        {
            if (SplineItemGenerate.Container != null && SplineItemGenerate.Container.Spline == spline)
                DelayUpdate().Forget();
        }

        /// <summary>
        /// 5ms 정도 지난 후 동작시켜야 재대로 반영됨
        /// </summary>
        private async UniTaskVoid DelayUpdate()
        {
            await UniTask.Delay(5, cancellationToken: this.GetCancellationTokenOnDestroy());
            TurnChain();
        }

        [ContextMenu("Force Scale")]
        private void ForceScale()
        {
            //플레이 모드라면 동작하지 않습니다.
            if (Application.isPlaying) return;

            if (SplineItemGenerate)
            {
                GameObject rootItem = GameObject.Find("root-" + SplineItemGenerate.GetInstanceID());
                rootItem.transform.localScale = transform.localScale;
            }
        }

        /// <summary>
        /// 짝수번째 사슬 고리를 90도 회전시킵니다.
        /// </summary>
        [ContextMenu("Force Turn Chain")]
        private void TurnChain()
        {
            //플레이 모드라면 동작하지 않습니다.
            if (Application.isPlaying) return;

            // 스프라인이 없으면 리턴
            if (!SplineItemGenerate)
                return;

            GameObject rootItem = GameObject.Find("root-" + SplineItemGenerate.GetInstanceID());

            if (!rootItem) return;

            // rootItem의 자식들의 트랜스폼을 모두 가져온 다음, 짝수번째 트랜스폼의 회전에 90을 더해준다
            for (int i = 0; i < rootItem.transform.childCount; i++)
                // 홀수번째만 90도 회전
                if (i % 2 == 1)
                    rootItem.transform.GetChild(i).rotation *= Quaternion.Euler(0, 0, 90);
        }

        [ContextMenu("Force Delete Chain")]
        private void DeleteChain()
        {
            //플레이 모드라면 동작하지 않습니다.
            if (Application.isPlaying) return;

            GameObject rootItem = GameObject.Find("root-" + SplineItemGenerate.GetInstanceID());
            DestroyImmediate(rootItem);
        }

        [ContextMenu("Bake Chain")]
        public void BakeChain()
        {
            //플레이 모드라면 동작하지 않습니다.
            if (Application.isPlaying) return;

            GameObject rootItem = GameObject.Find("root-" + SplineItemGenerate.GetInstanceID());
            Transform myParent = transform.parent;

            if (rootItem)
            {
                GameObject root = Instantiate(rootItem, rootItem.transform.position, rootItem.transform.rotation);
                root.transform.SetParent(transform);
                root.transform.localScale = rootItem.transform.localScale;
                root.transform.SetParent(myParent ? myParent : null);
                root.name = gameObject.name + "-Bake"; // 이름 변경합니다.
                root.hideFlags = HideFlags.None; // 보이게 합니다.

                // 자식들 개수를 가져옵니다.
                int count = root.transform.childCount;

                // 자식들을 모두 보이게 합니다.
                for (int i = 0; i < count; i++)
                    root.transform.GetChild(i).gameObject.hideFlags = HideFlags.None;

                gameObject.SetActive(false);
            }
        }
    }
}
#endif