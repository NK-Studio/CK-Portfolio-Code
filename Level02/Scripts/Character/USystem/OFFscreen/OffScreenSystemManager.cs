using System.Collections.Generic;
using UnityEngine;
using Utility;


namespace System
{
    public class OffScreenSystemManager : MonoBehaviour
    {
        public List<OffScreenSystem> offScreenSystems = new();

        /// <summary>
        /// 인자로 들어온 OffScreenSystem을 등록합니다.
        /// </summary>
        /// <param name="offScreenSystem"></param>
        public void Register(OffScreenSystem offScreenSystem)
        {
            offScreenSystems.Add(offScreenSystem);
        }

        /// <summary>
        /// 인자로 들어온 OffScreenSystem을 제거합니다.
        /// </summary>
        /// <param name="offScreenSystem"></param>
        public void Remove(OffScreenSystem offScreenSystem)
        {
            offScreenSystems.Remove(offScreenSystem);
        }
        
        /// <summary>
        /// 전부 가립니다.
        /// </summary>
        public void AllHide()
        {
            foreach (OffScreenSystem offScreenSystem in offScreenSystems)
                offScreenSystem.SetActive(EHookState.Hide);
        }

        
        /// <summary>
        /// 전부 가립니다.
        /// </summary>
        public void AllImpossible()
        {
            foreach (OffScreenSystem offScreenSystem in offScreenSystems)
                offScreenSystem.SetActive(EHookState.Impossible);
        }

        /// <summary>
        /// 타겟을 훅 상태에 맞춰서 UI를 띄웁니다.
        /// </summary>
        /// <param name="coll">타겟</param>
        /// <param name="hookState">훅 상태</param>
        public void UpdateTargetUI(Collider coll, EHookState hookState)
        {
            foreach (OffScreenSystem screenSystem in offScreenSystems)
            {
                bool isTarget = coll.gameObject.GetInstanceID() == screenSystem.gameObject.GetInstanceID();

                if (isTarget) 
                    screenSystem.SetActive(hookState);
            }
        }
    }
}