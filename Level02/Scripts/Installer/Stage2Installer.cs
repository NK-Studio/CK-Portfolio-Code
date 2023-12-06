using SlidePuzzle;
using UnityEngine;
using Zenject;

namespace Installer
{
    public class Stage2Installer : MonoInstaller
    {
        public Transform Goal;
        public SlidePuzzleSystem PuzzleSystem;
        
        public override void InstallBindings()
        {
            Container.Bind<Transform>().WithId("Goal").FromInstance(Goal);
            Container.Bind<SlidePuzzleSystem>().FromInstance(PuzzleSystem);
        }
    }
}