using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Effect
{
    public static class VFXExtensions
    {
        public enum ApplyTarget
        {
            All,
            Position,
            Rotation,
            Scale
        }

        private const string PositionPostfix = "_position";
        private const string RotationPostfix = "_angles";
        private const string ScalePostfix = "_scale";

        public static void SetVFXTransformProperty(this VisualEffect visualEffect, string propertyName,
            Transform transform, ApplyTarget applyTarget = ApplyTarget.All)
        {
            string position = propertyName + PositionPostfix;
            string angles = propertyName + RotationPostfix;
            string scale = propertyName + ScalePostfix;
         
            switch (applyTarget)
            {
                case ApplyTarget.All:
                {
                    visualEffect.SetVector3(position, transform.position);
                    visualEffect.SetVector3(angles, transform.eulerAngles);
                    visualEffect.SetVector3(scale, transform.localScale);
                    break;
                }
                case ApplyTarget.Position:
                    visualEffect.SetVector3(position, transform.position);
                    break;
                case ApplyTarget.Rotation:
                    visualEffect.SetVector3(angles, transform.eulerAngles);
                    break;
                case ApplyTarget.Scale:
                    visualEffect.SetVector3(scale, transform.localScale);
                    break;
            }
        }
    }
}