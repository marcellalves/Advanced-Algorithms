﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Advanced.Algorithms.DataStructures
{
    /// A priority queue implementation using min heap,
    /// assuming that higher values have a higher priority.
    public class MaxPriorityQueue<T>: IEnumerable<T> where T : IComparable
    {
        private readonly BMaxHeap<T> maxHeap = new BMaxHeap<T>();

        /// <summary>
        /// Time complexity:O(log(n)).
        /// </summary>
        public void Enqueue(T item)
        {
            maxHeap.Insert(item);
        }

        /// <summary>
        /// Time complexity:O(log(n)).
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            return maxHeap.ExtractMax();
        }

        /// <summary>
        /// Time complexity:O(1).
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            return maxHeap.PeekMax();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return maxHeap.GetEnumerator();
        }
    }
}
