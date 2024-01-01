using System.Collections.Generic;
using System.Linq;
using Managers;
using Settings.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Tutorial.Helper
{
    public class DialogEventCaller : MonoBehaviour
    {
        
        
        [field: SerializeField]
        public DialogTable Table { get; private set; }

        private IList<string> TableDropdownGetter => Table.EventRawTable.Keys.ToList();
        [field: SerializeField, ValueDropdown("TableDropdownGetter")]
        public string Event { get; private set; }

        [field: SerializeField] 
        public bool PlayOnAwake { get; private set; } = true;

        [field: SerializeField] 
        public int PlayOnAwakeEnableCount { get; private set; } = 1;


        private int _enableCount;
        private void OnEnable()
        {
            if(!PlayOnAwake) return;
            if (_enableCount >= PlayOnAwakeEnableCount)
            {
                CallEvent();
            }
            ++_enableCount;
        }
        public void CallEvent()
        {
            DialogManager.Instance.CallEvent(Event);
        }
        public void ResetEvent()
        {
            DialogManager.Instance.ResetEvent(Event);
        }

    }
}