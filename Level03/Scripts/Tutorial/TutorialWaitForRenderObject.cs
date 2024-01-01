using System;
using UnityEngine;
using Utility;

namespace Tutorial
{
    public class TutorialWaitForRenderObject : TutorialBase
    {
        public GameObject TargetObject;
        public Camera Camera;

        public override void Enter()
        {
            if(!Camera)
                Camera = Camera.main;
        }

        public override Result Execute()
        {
            if (!TargetObject)
            {
                return Result.Done;
            }

            if (Camera.IsRenderedSimple(TargetObject.transform.position))
            {
                return Result.Done;
            }
            
            return Result.Running;
        }

        public override void Exit()
        {
            
        }
    }
}