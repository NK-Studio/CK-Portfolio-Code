using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utility
{
    public struct HookShotPoint
    {
        public int ListIndex;
        public int ColliderIndex;
        public float Distance;

        public HookShotPoint(int listIndex, int colliderIndex, float distance)
        {
            ListIndex = listIndex;
            ColliderIndex = colliderIndex;
            Distance = distance;
        }
    }
    
    public class USorting 
    {
        public static void QuickSort(HookShotPoint[] array)
        {
            QuickSort(array, 0, array.Length - 1);
        }

        public static void QuickSort(HookShotPoint[] array, int start, int end)
        {
            int partition = Partition(array, start, end);

            //왼쪽 파티션
            if (start < partition - 1)
                QuickSort(array, start, partition - 1);

            if (partition < end)
                QuickSort(array, partition, end);
        }

        private static int Partition(HookShotPoint[] array, int start, int end)
        {
            int center = (start + end) / 2;
            float pivot = array[center].Distance;
            
            while (start <= end)
            {
                //Start 값이 Pivot보다 작아야 앞으로 감
                while (array[start].Distance < pivot) start++;
            
                //End 값이 Pivot보다 커야 앞으로 감
                while (array[end].Distance > pivot) end--;
            
                if (start <= end)
                {
                    Swap(array,start, end);
                    start++;
                    end--;
                }
            }

            return start;
        }

        private static void Swap(HookShotPoint[] array,int start, int end)
        {
            int tempIndex = array[start].ColliderIndex;
            float tempDistance = array[start].Distance;

            array[start].ColliderIndex = array[end].ColliderIndex;
            array[start].Distance = array[end].Distance;

            array[end].ColliderIndex = tempIndex;
            array[end].Distance = tempDistance;
        }
    }
}
