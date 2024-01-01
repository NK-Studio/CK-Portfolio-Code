using System;
using UnityEngine;

namespace Utility
{
    public static class BoundUtils
    {
        public static void EncapsulateOrSet(this ref Bounds to, Bounds from)
        {
            if (to.size.sqrMagnitude <= 0f)
            {
                to = from;
                return;
            }
            to.Encapsulate(from);
        }

    }
}