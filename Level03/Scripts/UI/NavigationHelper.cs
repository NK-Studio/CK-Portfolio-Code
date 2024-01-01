using System;
using System.Collections;
using System.Collections.Generic;
using Doozy.Runtime.UIManager.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class NavigationHelper : MonoBehaviour
    {
        public Selectable Selectable;

        private static string NavigationToString(in Navigation navigation)
        {
            if (navigation.mode == Navigation.Mode.Explicit)
            {
                return $"{{up={navigation.selectOnUp?.name ?? "NULL"}, " +
                       $"down={navigation.selectOnDown?.name ?? "NULL"}, " +
                       $"left={navigation.selectOnLeft?.name ?? "NULL"}, " +
                       $"right={navigation.selectOnRight?.name ?? "NULL"}}}";
            }
            return $"{{mode={navigation.mode}, wrapAround={navigation.wrapAround}}}";
        }
        public Navigation Navigation
        {
            get => Selectable.navigation;
            set
            {
                var oldNavigation = Selectable.navigation;
                var newNavigation = value;
                Debug.Log($"{name} - OLD: {NavigationToString(oldNavigation)}");
                Debug.Log($"{name} - NEW: {NavigationToString(newNavigation)}");
                Selectable.navigation = value;
            }
        }

        public Selectable Up
        {
            get => Navigation.selectOnUp;
            set => SetValue((ref Navigation navigation) => navigation.selectOnUp = value);
        }
        public Selectable Down
        {
            get => Navigation.selectOnDown;
            set => SetValue((ref Navigation navigation) => navigation.selectOnDown = value);
        }
        public Selectable Left
        {
            get => Navigation.selectOnLeft;
            set => SetValue((ref Navigation navigation) => navigation.selectOnLeft = value);
        }
        public Selectable Right
        {
            get => Navigation.selectOnRight;
            set => SetValue((ref Navigation navigation) => navigation.selectOnRight = value);
        }

        private delegate void Modifier<T>(ref T data);
        private void SetValue(Modifier<Navigation> action)
        {
            var str = Navigation;
            action.Invoke(ref str);
            Navigation = str;
        }
    }

    public static class SelectableExtensions
    {
        public static void SetNavigation(this Selectable s, Selectable up, Selectable down, Selectable left, Selectable right)
        {
            var str = s.navigation;
            str.mode = Navigation.Mode.Explicit;
            str.selectOnUp = up;
            str.selectOnDown = down;
            str.selectOnLeft = left;
            str.selectOnRight = right;
            s.navigation = str;
        }
        public static void SetNavigationPartial(this Selectable s, Selectable up = null, Selectable down = null, Selectable left = null, Selectable right = null)
        {
            var str = s.navigation;
            str.mode = Navigation.Mode.Explicit;
            str.selectOnUp = up ?? str.selectOnUp;
            str.selectOnDown = down ?? str.selectOnDown;
            str.selectOnLeft = left ?? str.selectOnLeft;
            str.selectOnRight = right ?? str.selectOnRight;
            s.navigation = str;
        }
        public static void SetUp(this Selectable s, Selectable other)
        {
            var str = s.navigation;
            str.mode = Navigation.Mode.Explicit;
            str.selectOnUp = other;
            s.navigation = str;
        }
        public static void SetDown(this Selectable s, Selectable other)
        {
            var str = s.navigation;
            str.mode = Navigation.Mode.Explicit;
            str.selectOnDown = other;
            s.navigation = str;
        }
        public static void SetLeft(this Selectable s, Selectable other)
        {
            var str = s.navigation;
            str.mode = Navigation.Mode.Explicit;
            str.selectOnLeft = other;
            s.navigation = str;
        }
        public static void SetRight(this Selectable s, Selectable other)
        {
            var str = s.navigation;
            str.mode = Navigation.Mode.Explicit;
            str.selectOnRight = other;
            s.navigation = str;
        }
        
        
    }
}
