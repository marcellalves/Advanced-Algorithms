﻿using System;

namespace Advanced.Algorithms.Sorting
{
    public class QuickSort<T> where T : IComparable
    {
        //O(nlog(n)), worst O(n^2)
        public static T[] Sort(T[] array)
        {
            if (array.Length <= 1)
            {
                return array;
            }

            sort(array, 0, array.Length - 1);

            return array;
        }

        private static void sort(T[] array, int startIndex, int endIndex)
        {
            while (true)
            {
                //if only one element the do nothing
                if (startIndex < 0 || endIndex < 0 || endIndex - startIndex < 1)
                {
                    return;
                }

                //set the wall to the left most index
                var wall = startIndex;

                //pick last index element on array as comparison pivot
                var pivot = array[endIndex];

                //swap elements greater than pivot to the right side of wall
                //others will be on left
                for (var j = wall; j <= endIndex; j++)
                {
                    if (pivot.CompareTo(array[j]) <= 0 && j != endIndex)
                    {
                        continue;
                    }

                    var temp = array[wall];
                    array[wall] = array[j];
                    array[j] = temp;
                    //increment to exclude the minimum element in subsequent comparisons
                    wall++;
                }

                //soft left
                sort(array, startIndex, wall - 2);
                //sort right
                startIndex = wall;
            }
        }
    }
}
