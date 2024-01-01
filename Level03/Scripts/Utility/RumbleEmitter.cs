using System;
using Managers;
using UnityEngine;

namespace Utility
{
    public class RumbleEmitter : RunOnEnable
    {
        public GamePadManager.RumbleSettings Settings;

        public override void Execute()
        {
            base.Execute();
            Settings.Pulse();
        }
    }
}