using UnityEngine;
using UnityEngine.Serialization;
using Utility;

namespace Tutorial
{
    public class TutorialSetFakeChildTarget : TutorialBase
    {
        public FakeChild TargetFakeChild;
        public Transform TargetParent;
        public FakeChild.Mode TargetFollowMode;

        public override void Enter() { }

        public override Result Execute()
        {
            if (!TargetFakeChild) return Result.Done;
            TargetFakeChild.TargetParent = TargetParent;
            TargetFakeChild.FollowMode = TargetFollowMode;
            return Result.Done;
        }

        public override void Exit() { }
    }
}