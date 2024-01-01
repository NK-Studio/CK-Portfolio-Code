using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Utility
{
    public class RunOnEnableIfRendered : RunOnEnable
    {
        private Camera _camera;
        public Vector2 Minimum = new Vector2(-0.1f, -0.1f);
        public Vector2 Maximum = new Vector2(1.1f, 1.1f);
        
        protected override void OnEnable()
        {
            if (!_camera)
            {
                _camera = Camera.main;
            }
            Run().Forget();
        }

        private async UniTaskVoid Run()
        {
            await UniTask.Yield();
            if (!_camera.IsRenderedSimple(transform.position, Minimum, Maximum))
            {
                return;
            }
            
            base.OnEnable();
        }
    }
}