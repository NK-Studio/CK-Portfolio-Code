using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class LayoutUIHelper : MonoBehaviour
    {
        public RectTransform Target;

        public bool RebuildImmediatelyOnStart = false;
        public void RebuildImmediately()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(Target);
        }
        public bool RebuildMarkOnStart = false;
        public void RebuildMark()
        {
            LayoutRebuilder.MarkLayoutForRebuild(Target);
        }

        private void Start()
        {
            if (RebuildImmediatelyOnStart)
            {
                RebuildImmediately();
            }

            if (RebuildMarkOnStart)
            {
                RebuildMark();
            }
        }
    }
}