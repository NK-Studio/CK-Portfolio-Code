using UnityEngine;

namespace Utility
{
    public static class CameraUtility
    {
        private static readonly Vector2 RenderMarginMinimum = new Vector2(-0.1f, -0.1f);
        private static readonly Vector2 RenderMarginMaximum = new Vector2(1.1f, 1.1f);

        public static bool IsRenderedSimple(this Camera camera, Vector3 position)
            => IsRenderedSimple(camera, position, RenderMarginMinimum, RenderMarginMaximum);
        public static bool IsRenderedSimple(this Camera camera, Vector3 position, Vector2 minimumMargin, Vector3 maximumMargin)
        {
            var positionVS = camera.WorldToViewportPoint(position);
            
            if (positionVS.x < minimumMargin.x 
                || positionVS.y < minimumMargin.y 
                || positionVS.x > maximumMargin.x 
                || positionVS.y > maximumMargin.y)
            {
                return false;
            }

            return true;
        }
    }
}