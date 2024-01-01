using System.Collections.Generic;
using Sirenix.OdinInspector;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Settings.Options
{
    public class OptionUICategoryNavigationHelper : MonoBehaviour
    {
        public Transform TargetRoot;
        public List<Selectable> Selectables;
        public Selectable Top;
        public Selectable Bottom;

        private Selectable _category;
        [Button]
        public void Refresh()
        {
            if (!TargetRoot)
            {
                return;
            }
            _category = GetComponent<Selectable>();

            Selectables.Clear();
            var childCount = TargetRoot.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var t = TargetRoot.GetChild(i);
                if (!t.TryGetComponent(out Selectable s) || !s.isActiveAndEnabled)
                {
                    continue;
                }
                Selectables.Add(s);
            }

            if (Selectables.Count <= 0)
            {
                Top = null;
                Bottom = null;
                return;
            }

            if (Selectables.Count <= 1)
            {
                Top = Selectables[0];
                Bottom = Selectables[0];
                Top.SetNavigationPartial(up: _category, down: BottomReceivers[0].Selectable);
                return;
            }
            
            Top = Selectables[0];
            Bottom = Selectables[^1];

            Top.SetNavigationPartial(up: _category, down: Selectables[1]);
            Bottom.SetNavigationPartial(up: Selectables[^2], down: BottomReceivers[0].Selectable);

            for (int i = 1; i < Selectables.Count - 1; i++)
            {
                Selectables[i].SetNavigationPartial(up: Selectables[i - 1], down: Selectables[i + 1]);
            }
        }
        
        [FormerlySerializedAs("Receivers")] public List<NavigationHelper> BottomReceivers = new();
        public void Set()
        {
            if (!_category)
            {
                _category = GetComponent<Selectable>();
            }
            _category.SetDown(Top ?? BottomReceivers[0].Selectable);
            foreach (var helper in BottomReceivers)
            {
                helper.Up = Bottom;
            }
        }
    }
}