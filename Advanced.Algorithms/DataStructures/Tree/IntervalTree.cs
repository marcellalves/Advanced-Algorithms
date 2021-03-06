﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Advanced.Algorithms.DataStructures
{
    /// <summary>
    /// An interval object to represent multi-dimensional intervals
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DInterval<T> where T : IComparable
    {
        public T[] Start { get; set; }
        public T[] End { get; set; }

        public DInterval(T[] start, T[] end)
        {
            Start = start;
            End = end;
        }
    }

    /// <summary>
    /// A multi-dimensional interval tree implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DIntervalTree<T> where T : IComparable
    {
        private readonly int dimensions;
        private readonly IntervalTree<T> tree;

        public int Count { get; private set; }

        public DIntervalTree(int dimensions)
        {
            if (dimensions <= 0)
            {
                throw new Exception("Dimension should be greater than 0.");
            }

            this.dimensions = dimensions;
            tree = new IntervalTree<T>();
        }

        /// <summary>
        /// validate dimensions for point length
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void validateDimensions(T[] start, T[] end)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (end == null)
            {
                throw new ArgumentNullException(nameof(end));
            }

            if (start.Length != dimensions || start.Length != end.Length)
            {
                throw new Exception($"Expecting {dimensions} points in start and end values for this interval.");
            }

            if (start.Where((t, i) => t.Equals(defaultValue.Value)
                                      || end[i].Equals(defaultValue.Value)).Any())
            {
                throw new Exception("Points cannot contain Minimum Value or Null values");
            }
        }

        /// <summary>
        /// A cached function to override default(T)
        /// So that for value types we can return min value as default
        /// </summary>
        /// <returns></returns>
        private readonly Lazy<T> defaultValue = new Lazy<T>(() =>
        {
            var s = typeof(T);

            bool isValueType;

#if NET40
            isValueType = s.IsValueType;
#else
            isValueType = s.GetTypeInfo().IsValueType;
#endif

            if (isValueType)
            {
                return (T)Convert.ChangeType(int.MinValue, s);
            }

            return default(T);
        });

        /// <summary>
        /// Add a new interval to this interval tree
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void Insert(T[] start, T[] end)
        {
            validateDimensions(start, end);

            var currentTrees = new List<IntervalTree<T>> {tree};


            //get all overlaps
            //and insert next dimension value to each overlapping node
            for (var i = 0; i < dimensions; i++)
            {
                var allOverlaps = new List<IntervalTree<T>>();

                foreach (var tree in currentTrees)
                {
                    tree.Insert(new AsInterval<T>(start[i], end[i]));

                    var overlaps = tree.GetOverlaps(new AsInterval<T>(start[i], end[i]));
                    foreach (var overlap in overlaps)
                    {
                        allOverlaps.Add(overlap.NextDimensionIntervals);
                    }
                }

                currentTrees = allOverlaps;
            }

            Count++;
        }

        /// <summary>
        /// delete this interval
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void Delete(T[] start, T[] end)
        {
            validateDimensions(start, end);

            var allOverlaps = new List<IntervalTree<T>>();
            var overlaps = tree.GetOverlaps(new AsInterval<T>(start[0], end[0]));

            foreach (var overlap in overlaps)
            {
                allOverlaps.Add(overlap.NextDimensionIntervals);
            }

            DeleteOverlaps(allOverlaps, start, end, 1);
            tree.Delete(new AsInterval<T>(start[0], end[0]));

            Count--;
        }

        /// <summary>
        /// recursively delete values from overlaps in next dimension
        /// </summary>
        /// <param name="currentTrees"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="index"></param>
        private void DeleteOverlaps(List<IntervalTree<T>> currentTrees, T[] start, T[] end, int index)
        {
            //base case
            if (index == start.Length)
                return;

            var allOverlaps = new List<IntervalTree<T>>();

            foreach (var tree in currentTrees)
            {
                var overlaps = tree.GetOverlaps(new AsInterval<T>(start[index], end[index]));

                foreach (var overlap in overlaps)
                {
                    allOverlaps.Add(overlap.NextDimensionIntervals);
                }
            }

            //dig in to next dimension to 
            DeleteOverlaps(allOverlaps, start, end, ++index);

            index--;

            //now delete
            foreach (var tree in allOverlaps)
            {
                if (tree.Count > 0)
                {
                    tree.Delete(new AsInterval<T>(start[index], end[index]));
                }

            }
        }

        /// <summary>
        /// does this interval overlap with any interval in this interval tree?
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public bool DoOverlap(T[] start, T[] end)
        {
            validateDimensions(start, end);

            var allOverlaps = GetOverlaps(tree, start, end, 0);

            return allOverlaps.Count > 0;
        }

        /// <summary>
        /// returns a list of matching intervals
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<DInterval<T>> GetOverlaps( T[] start, T[] end)
        {
            return GetOverlaps(tree, start, end, 0);
        }

        /// <summary>
        /// does this interval overlap with any interval in this interval tree?
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private List<DInterval<T>> GetOverlaps(IntervalTree<T> currentTree, 
            T[] start, T[] end, int dimension)
        {
            var nodes = currentTree.GetOverlaps(new AsInterval<T>(start[dimension], end[dimension]));

            if (dimension + 1 == dimensions)
            {
                var result = new List<DInterval<T>>();

                foreach (var node in nodes)
                {
                    var fStart = new T[dimensions];
                    var fEnd = new T[dimensions];

                    fStart[dimension] = node.Start;
                    fEnd[dimension] = node.End[node.MatchingEndIndex];

                    var thisDimResult = new DInterval<T>(fStart, fEnd);
                    
                    result.Add(thisDimResult);
                }

                return result;
            }
            else
            {
                var result = new List<DInterval<T>>();

                foreach (var node in nodes)
                {
                    var nextDimResult = GetOverlaps(node.NextDimensionIntervals, start, end, dimension + 1);

                    foreach (var nextResult in nextDimResult)
                    {
                        nextResult.Start[dimension] = node.Start;
                        nextResult.End[dimension] = node.End[node.MatchingEndIndex];

                        result.Add(nextResult);
                    }
                }

                return result;
            }

        }

    }
    /// <summary>
    /// Interval object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class AsInterval<T> : IComparable where T : IComparable
    {
        /// <summary>
        /// Start of this interval range
        /// </summary>
        public T Start { get; set; }

        /// <summary>
        /// End of this interval range
        /// </summary>
        public List<T> End { get; set; }

        /// <summary>
        /// Max End interval under this interval
        /// </summary>
        internal T MaxEnd { get; set; }

        /// <summary>
        /// holds intervals for the next dimension
        /// </summary>
        internal IntervalTree<T> NextDimensionIntervals { get; set; }

        /// <summary>
        /// Mark the matching end index when overlap search 
        /// so that we can return the overlapping interval
        /// </summary>
        internal int MatchingEndIndex { get; set; }

        public int CompareTo(object obj)
        {
            return Start.CompareTo(((AsInterval<T>) obj).Start);
        }

        public AsInterval(T start, T end)
        {
            Start = start;
            End = new List<T> {end};
            NextDimensionIntervals = new IntervalTree<T>();
        }
    }

    /// <summary>
    /// An overlapping interval tree implementation in 1-dimension using Augmentation Tree method
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class IntervalTree<T> where T : IComparable
    {
        //use a height balanced binary search tree
        private readonly AsIntervalRedBlackTree redBlackTree
            = new AsIntervalRedBlackTree();

        public int Count { get; private set; }

        /// <summary>
        /// Insert a new Interval
        /// </summary>
        /// <param name="newInterval"></param>
        public void Insert(AsInterval<T> newInterval)
        {
            sortInterval(newInterval);
            redBlackTree.Insert(newInterval);
            Count++;
        }

        /// <summary>
        /// Delete this interval
        /// </summary>
        /// <param name="interval"></param>
        public void Delete(AsInterval<T> interval)
        {
            sortInterval(interval);

            redBlackTree.Delete(interval);
            Count--;
        }

        /// <summary>
        ///  Returns an interval in this tree that overlaps with this search interval 
        /// </summary>
        /// <param name="searchInterval"></param>
        /// <returns></returns>
        internal AsInterval<T> GetOverlap(AsInterval<T> searchInterval)
        {
            sortInterval(searchInterval);
            return getOverlap(redBlackTree.Root, searchInterval);
        }

        /// <summary>
        ///  Returns an interval in this tree that overlaps with this search interval 
        /// </summary>
        /// <param name="searchInterval"></param>
        /// <returns></returns>
        internal List<AsInterval<T>> GetOverlaps(AsInterval<T> searchInterval)
        {
            sortInterval(searchInterval);
            return getOverlaps(redBlackTree.Root, searchInterval);
        }

        /// <summary>
        ///  does any interval overlaps with this search interval
        /// </summary>
        /// <param name="searchInterval"></param>
        /// <returns></returns>
        internal bool DoOverlap(AsInterval<T> searchInterval)
        {
            sortInterval(searchInterval);
            return getOverlap(redBlackTree.Root, searchInterval) != null;
        }

        /// <summary>
        /// Swap intervals so that start always appear before end
        /// </summary>
        /// <param name="value"></param>
        private void sortInterval(AsInterval<T> value)
        {
            if (value.Start.CompareTo(value.End[0]) <= 0)
            {
                return;
            }

            var tmp = value.End[0];
            value.End[0] = value.Start;
            value.Start = tmp;
        }

        /// <summary>
        /// Returns an interval that overlaps with this interval
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        private AsInterval<T> getOverlap(AsIntervalRedBlackTreeNode<AsInterval<T>> current, AsInterval<T> searchInterval)
        {
            while (true)
            {
                if (current == null)
                {
                    return null;
                }

                if (doOverlap(current.Value, searchInterval))
                {
                    return current.Value;
                }

                //if left max is greater than search start
                //then the search interval can occur in left sub tree
                if (current.Left != null && current.Left.Value.MaxEnd.CompareTo(searchInterval.Start) >= 0)
                {
                    current = current.Left;
                    continue;
                }

                //otherwise look in right subtree
                current = current.Right;
            }
        }

        /// <summary>
        /// Returns all intervals that overlaps with this interval
        /// </summary>
        /// <returns></returns>
        private List<AsInterval<T>> getOverlaps(AsIntervalRedBlackTreeNode<AsInterval<T>> current,
            AsInterval<T> searchInterval, List<AsInterval<T>> result = null)
        {
            if (result == null)
            {
                result = new List<AsInterval<T>>();
            }

            if (current == null)
            {
                return result;
            }

            if (doOverlap(current.Value, searchInterval))
            {
                result.Add(current.Value);
            }

            //if left max is greater than search start
            //then the search interval can occur in left sub tree
            if (current.Left != null
                && current.Left.Value.MaxEnd.CompareTo(searchInterval.Start) >= 0)
            {
                getOverlaps(current.Left, searchInterval, result);
            }

            //otherwise look in right subtree
            getOverlaps(current.Right, searchInterval, result);

            return result;
        }

        /// <summary>
        /// Does this interval a overlap with b 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool doOverlap(AsInterval<T> a, AsInterval<T> b)
        {
            //lazy reset
            a.MatchingEndIndex = -1;
            b.MatchingEndIndex = -1;

            for (var i = 0; i < a.End.Count; i++)
            {
                for (var j = 0; j < b.End.Count; j++)
                {
                 
                    //a.Start less than b.End and a.End greater than b.Start
                    if (a.Start.CompareTo(b.End[j]) > 0 || a.End[i].CompareTo(b.Start) < 0)
                    {
                        continue;
                    }

                    a.MatchingEndIndex = i;
                    b.MatchingEndIndex = j;

                    return true;
                }

            }

            return false;
        }

        internal class AsIntervalRedBlackTreeNode<T> where T : IComparable
        {
            internal T Value { get; set; }

            internal AsIntervalRedBlackTreeNode<T> Parent { get; set; }

            internal AsIntervalRedBlackTreeNode<T> Left { get; set; }
            internal AsIntervalRedBlackTreeNode<T> Right { get; set; }

            internal bool IsLeaf => Left == null && Right == null;
            internal RedBlackTreeNodeColor NodeColor { get; set; }

            internal AsIntervalRedBlackTreeNode<T> Sibling => this.Parent == null ? null : this.Parent.Left == this ?
                                                    this.Parent.Right : this.Parent.Left;

            internal bool IsLeftChild => this.Parent.Left == this;
            internal bool IsRightChild => this.Parent.Right == this;

            internal AsIntervalRedBlackTreeNode(AsIntervalRedBlackTreeNode<T> parent, T value)
            {
                Parent = parent;
                Value = value;
                NodeColor = RedBlackTreeNodeColor.Red;
            }
        }

        internal class AsIntervalRedBlackTree
        {
            internal AsIntervalRedBlackTreeNode<AsInterval<T>> Root { get; private set; }
            public int Count { get; private set; }

            /// <summary>
            /// A cached function to override default(T)
            /// So that for value types we can return min value as default
            /// </summary>
            /// <returns></returns>
            private readonly Lazy<T> defaultValue = new Lazy<T>(() =>
            {
                var s = typeof(T);

                bool isValueType;

#if NET40
                isValueType = s.IsValueType;
#else
                isValueType = s.GetTypeInfo().IsValueType;
#endif
                if (isValueType)
                {
                    return (T)Convert.ChangeType(int.MinValue, s);
                }

                return default(T);
            });

            internal void Clear()
            {
                Root = null;
                Count = 0;
            }

            /// <summary>
            /// update max end value under each node recursively
            /// </summary>
            /// <param name="node"></param>
            /// <param name="currentMax"></param>
            /// <param name="recurseUp"></param>
            private void updateMax(AsIntervalRedBlackTreeNode<AsInterval<T>> node, T currentMax, bool recurseUp = true)
            {
                while (true)
                {
                    if (node == null)
                    {
                        return;
                    }

                    if (node.Left != null && node.Right != null)
                    {
                        //if current Max is less than current End
                        //then update current Max
                        if (currentMax.CompareTo(node.Left.Value.MaxEnd) < 0)
                        {
                            currentMax = node.Left.Value.MaxEnd;
                        }

                        if (currentMax.CompareTo(node.Right.Value.MaxEnd) < 0)
                        {
                            currentMax = node.Right.Value.MaxEnd;
                        }
                    }
                    else if (node.Left != null)
                    {
                        //if current Max is less than current End
                        //then update current Max
                        if (currentMax.CompareTo(node.Left.Value.MaxEnd) < 0)
                        {
                            currentMax = node.Left.Value.MaxEnd;
                        }
                    }
                    else if (node.Right != null)
                    {
                        if (currentMax.CompareTo(node.Right.Value.MaxEnd) < 0)
                        {
                            currentMax = node.Right.Value.MaxEnd;
                        }
                    }

                    foreach (var v in node.Value.End)
                    {
                        if (currentMax.CompareTo(v) < 0)
                        {
                            currentMax = v;
                        }
                    }

                    node.Value.MaxEnd = currentMax;


                    if (recurseUp)
                    {
                        node = node.Parent;
                        continue;
                    }


                    break;
                }
            }

            /// <summary>
            /// Update Max on new root after rotations
            /// </summary>
            /// <param name="newRoot"></param>
            private void updateMax(AsIntervalRedBlackTreeNode<AsInterval<T>> newRoot, bool recurseUp = true)
            {
                if (newRoot == null)
                    return;

                newRoot.Value.MaxEnd = defaultValue.Value;

                if (newRoot.Left != null)
                {
                    newRoot.Left.Value.MaxEnd = defaultValue.Value;
                    updateMax(newRoot.Left, newRoot.Left.Value.MaxEnd, recurseUp);
                }

                if (newRoot.Right != null)
                {
                    newRoot.Right.Value.MaxEnd = defaultValue.Value;
                    updateMax(newRoot.Right, newRoot.Right.Value.MaxEnd, recurseUp);
                }

                updateMax(newRoot, newRoot.Value.MaxEnd, recurseUp);

            }


            /// <summary>
            /// Rotate right
            /// </summary>
            /// <param name="node"></param>
            private void rightRotate(AsIntervalRedBlackTreeNode<AsInterval<T>> node)
            {
                var prevRoot = node;
                var leftRightChild = prevRoot.Left.Right;

                var newRoot = node.Left;

                //make left child as root
                prevRoot.Left.Parent = prevRoot.Parent;

                if (prevRoot.Parent != null)
                {
                    if (prevRoot.Parent.Left == prevRoot)
                    {
                        prevRoot.Parent.Left = prevRoot.Left;
                    }
                    else
                    {
                        prevRoot.Parent.Right = prevRoot.Left;
                    }
                }

                //move prev root as right child of current root
                newRoot.Right = prevRoot;
                prevRoot.Parent = newRoot;

                //move right child of left child of prev root to left child of right child of new root
                newRoot.Right.Left = leftRightChild;
                if (newRoot.Right.Left != null)
                {
                    newRoot.Right.Left.Parent = newRoot.Right;
                }

                if (prevRoot == Root)
                {
                    Root = newRoot;
                }

                updateMax(newRoot, false);
            }

            /// <summary>
            /// rotate left
            /// </summary>
            /// <param name="node"></param>
            private void leftRotate(AsIntervalRedBlackTreeNode<AsInterval<T>> node)
            {
                var prevRoot = node;
                var rightLeftChild = prevRoot.Right.Left;

                var newRoot = node.Right;

                //make right child as root
                prevRoot.Right.Parent = prevRoot.Parent;

                if (prevRoot.Parent != null)
                {
                    if (prevRoot.Parent.Left == prevRoot)
                    {
                        prevRoot.Parent.Left = prevRoot.Right;
                    }
                    else
                    {
                        prevRoot.Parent.Right = prevRoot.Right;
                    }
                }


                //move prev root as left child of current root
                newRoot.Left = prevRoot;
                prevRoot.Parent = newRoot;

                //move left child of right child of prev root to right child of left child of new root
                newRoot.Left.Right = rightLeftChild;
                if (newRoot.Left.Right != null)
                {
                    newRoot.Left.Right.Parent = newRoot.Left;
                }

                if (prevRoot == Root)
                {
                    Root = newRoot;
                }

                updateMax(newRoot, false);
            }

            /// <summary>
            /// Get max under this element
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            private AsIntervalRedBlackTreeNode<AsInterval<T>> findMax(AsIntervalRedBlackTreeNode<AsInterval<T>> node)
            {
                while (true)
                {
                    if (node.Right == null) return node;
                    node = node.Right;
                }
            }

            //O(log(n)) always
            public void Insert(AsInterval<T> value)
            {
                //empty tree
                if (Root == null)
                {
                    Root = new AsIntervalRedBlackTreeNode<AsInterval<T>>(null, value)
                    {
                        NodeColor = RedBlackTreeNodeColor.Black
                    };
                    updateMax(Root);
                    Count++;
                    return;
                }

                insert(Root, value);
                Count++;
            }

            //O(log(n)) always
            private AsIntervalRedBlackTreeNode<AsInterval<T>> insert(
                AsIntervalRedBlackTreeNode<AsInterval<T>> currentNode, AsInterval<T> newNodeValue)
            {

                var compareResult = currentNode.Value.CompareTo(newNodeValue);

                //current node is less than new item
                if (compareResult < 0)
                {
                    //no right child
                    if (currentNode.Right == null)
                    {
                        //insert
                        var newNode = new AsIntervalRedBlackTreeNode<AsInterval<T>>(currentNode, newNodeValue);
                        currentNode.Right = newNode;
                        updateMax(newNode);
                        balanceInsertion(newNode);
                        return newNode;
                    }
                    else
                    {
                        var newNode = insert(currentNode.Right, newNodeValue);
                        return newNode;
                    }

                }
                //current node is greater than new node

                if (compareResult > 0)
                {

                    if (currentNode.Left == null)
                    {
                        //insert
                        var newNode = new AsIntervalRedBlackTreeNode<AsInterval<T>>(currentNode, newNodeValue);
                        currentNode.Left = newNode;
                        updateMax(newNode);
                        balanceInsertion(newNode);
                        return newNode;
                    }
                    else
                    {
                        var newNode = insert(currentNode.Left, newNodeValue);
                        return newNode;
                    }
                }

                currentNode.Value.End.Add(newNodeValue.End[0]);
                updateMax(currentNode);
                return currentNode;
            }

            /// <summary>
            /// balance after insertion
            /// </summary>
            /// <param name="nodeToBalance"></param>
            private void balanceInsertion(AsIntervalRedBlackTreeNode<AsInterval<T>> nodeToBalance)
            {
                while (true)
                {
                    if (nodeToBalance == Root)
                    {
                        Root.NodeColor = RedBlackTreeNodeColor.Black;
                        updateMax(Root);
                        return;
                    }

                    //if node to balance is red
                    if (nodeToBalance.NodeColor == RedBlackTreeNodeColor.Red)
                    {
                        //red-red relation; fix it!
                        if (nodeToBalance.Parent.NodeColor == RedBlackTreeNodeColor.Red)
                        {
                            //red sibling
                            if (nodeToBalance.Parent.Sibling != null && nodeToBalance.Parent.Sibling.NodeColor == RedBlackTreeNodeColor.Red)
                            {
                                //mark both children of parent as black and move up balancing 
                                nodeToBalance.Parent.Sibling.NodeColor = RedBlackTreeNodeColor.Black;
                                nodeToBalance.Parent.NodeColor = RedBlackTreeNodeColor.Black;

                                //root is always black
                                if (nodeToBalance.Parent.Parent != Root)
                                {
                                    nodeToBalance.Parent.Parent.NodeColor = RedBlackTreeNodeColor.Red;
                                }

                                nodeToBalance = nodeToBalance.Parent.Parent;
                            }
                            //absent sibling or black sibling
                            else if (nodeToBalance.Parent.Sibling == null || nodeToBalance.Parent.Sibling.NodeColor == RedBlackTreeNodeColor.Black)
                            {
                                if (nodeToBalance.IsLeftChild && nodeToBalance.Parent.IsLeftChild)
                                {
                                    var newRoot = nodeToBalance.Parent;
                                    swapColors(nodeToBalance.Parent, nodeToBalance.Parent.Parent);
                                    rightRotate(nodeToBalance.Parent.Parent);

                                    if (newRoot == Root)
                                    {
                                        Root.NodeColor = RedBlackTreeNodeColor.Black;
                                    }

                                    nodeToBalance = newRoot;
                                }
                                else if (nodeToBalance.IsLeftChild && nodeToBalance.Parent.IsRightChild)
                                {
                                    rightRotate(nodeToBalance.Parent);

                                    var newRoot = nodeToBalance;

                                    swapColors(nodeToBalance.Parent, nodeToBalance);
                                    leftRotate(nodeToBalance.Parent);

                                    if (newRoot == Root)
                                    {
                                        Root.NodeColor = RedBlackTreeNodeColor.Black;
                                    }

                                    nodeToBalance = newRoot;
                                }
                                else if (nodeToBalance.IsRightChild && nodeToBalance.Parent.IsRightChild)
                                {
                                    var newRoot = nodeToBalance.Parent;
                                    swapColors(nodeToBalance.Parent, nodeToBalance.Parent.Parent);
                                    leftRotate(nodeToBalance.Parent.Parent);

                                    if (newRoot == Root)
                                    {
                                        Root.NodeColor = RedBlackTreeNodeColor.Black;
                                    }

                                    nodeToBalance = newRoot;
                                }
                                else if (nodeToBalance.IsRightChild && nodeToBalance.Parent.IsLeftChild)
                                {
                                    leftRotate(nodeToBalance.Parent);

                                    var newRoot = nodeToBalance;

                                    swapColors(nodeToBalance.Parent, nodeToBalance);
                                    rightRotate(nodeToBalance.Parent);

                                    if (newRoot == Root)
                                    {
                                        Root.NodeColor = RedBlackTreeNodeColor.Black;
                                    }

                                    nodeToBalance = newRoot;
                                }
                            }
                        }
                    }

                    if (nodeToBalance.Parent != null)
                    {
                        nodeToBalance = nodeToBalance.Parent;
                        continue;
                    }

                    break;
                }
            }

            /// <summary>
            /// swap colors of two nodes
            /// </summary>
            /// <param name="node1"></param>
            /// <param name="node2"></param>
            private void swapColors(AsIntervalRedBlackTreeNode<AsInterval<T>> node1, AsIntervalRedBlackTreeNode<AsInterval<T>> node2)
            {
                var tmpColor = node2.NodeColor;
                node2.NodeColor = node1.NodeColor;
                node1.NodeColor = tmpColor;
            }

            //O(log(n)) always
            public void Delete(AsInterval<T> value)
            {
                if (Root == null)
                {
                    throw new Exception("Empty Tree");
                }

                delete(Root, value, false);
                Count--;

            }


            /// <summary>
            /// Deletes the node and return a reference to the parent of actual node deleted
            /// O(log(n)) always
            /// </summary>
            /// <param name="node"></param>
            /// <param name="value"></param>
            /// <param name="deleteByStartOnly"></param>
            /// <returns></returns>
            private void delete(AsIntervalRedBlackTreeNode<AsInterval<T>> node, AsInterval<T> value,
                bool deleteByStartOnly)
            {
                while (true)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    var compareResult = node.Value.CompareTo(value);

                    //node is less than the search value so move right to find the deletion node
                    if (compareResult < 0)
                    {
                        node = node.Right ?? throw new Exception("Item do not exist");
                    }
                    //node is less than the search value so move left to find the deletion node
                    else if (compareResult > 0)
                    {
                        node = node.Left ?? throw new Exception("Item do not exist");
                    }
                    else
                    {
                        AsIntervalRedBlackTreeNode<AsInterval<T>> nodeToBalance;

                        //if not a leaf deletion caused by replacement 
                        //of an ancestor deleted with this node
                        if (!deleteByStartOnly)
                        {
                            var index = getIndex(node.Value.End, value);

                            if (index == -1)
                            {
                                throw new Exception("Interval do not exist");
                            }

                            if (node.Value.End.Count > 1)
                            {
                                node.Value.End.RemoveAt(index);
                                updateMax(node);
                                return;
                            }
                        }

                        //node is a leaf node
                        if (node.IsLeaf)
                        {
                            //if color is red, we are good; no need to balance
                            if (node.NodeColor == RedBlackTreeNodeColor.Red)
                            {
                                deleteLeaf(node);
                                updateMax(node.Parent);
                                return;
                            }


                            nodeToBalance = handleDoubleBlack(node);
                            deleteLeaf(node);
                            updateMax(node.Parent);
                        }
                        else
                        {
                            //case one - right tree is null (move sub tree up)
                            if (node.Left != null && node.Right == null)
                            {
                                nodeToBalance = handleDoubleBlack(node);
                                deleteLeftNode(node);
                                updateMax(node.Parent);
                            }
                            //case two - left tree is null  (move sub tree up)
                            else if (node.Right != null && node.Left == null)
                            {
                                nodeToBalance = handleDoubleBlack(node);
                                deleteRightNode(node);
                                updateMax(node.Parent);
                            }
                            //case three - two child trees 
                            //replace the node value with maximum element of left subtree (left max node)
                            //and then delete the left max node
                            else
                            {
                                var index = getIndex(node.Value.End, value);

                                if (index == -1)
                                {
                                    throw new Exception("Interval do not exist");
                                }

                                //if this is the only element
                                //do regular bst deletion
                                if (node.Value.End.Count == 1 && index == 0)
                                {
                                    var maxLeftNode = findMax(node.Left);

                                    node.Value = maxLeftNode.Value;
                                    node.Value.MaxEnd = defaultValue.Value;

                                    //delete left max node
                                    node = node.Left;
                                    value = maxLeftNode.Value;
                                    deleteByStartOnly = true;
                                    continue;
                                }

                                //just remove the end
                                node.Value.End.RemoveAt(index);
                                updateMax(node);
                                return;
                            }
                        }

                        //handle six cases
                        while (nodeToBalance != null)
                        {
                            nodeToBalance = handleDoubleBlack(nodeToBalance);
                        }

                        return;
                    }
                }
            }

            /// <summary>
            /// returns the index of a matching value in this End range List
            /// </summary>
            /// <param name="end"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            private int getIndex(List<T> end, AsInterval<T> value)
            {
                var index = -1;
                for (var i = 0; i < end.Count; i++)
                {
                    if (end[i].CompareTo(value.End[0]) == 0)
                    {
                        index = i;
                    }
                }

                return index;
            }

            /// <summary>
            /// deletes leaf
            /// </summary>
            /// <param name="node"></param>
            private void deleteLeaf(AsIntervalRedBlackTreeNode<AsInterval<T>> node)
            {
                //if node is root
                if (node.Parent == null)
                {
                    Root = null;
                }
                //assign nodes parent.left/right to null
                else if (node.IsLeftChild)
                {
                    node.Parent.Left = null;
                }
                else
                {
                    node.Parent.Right = null;
                }
            }

            /// <summary>
            /// deletes right node
            /// </summary>
            /// <param name="node"></param>
            private void deleteRightNode(AsIntervalRedBlackTreeNode<AsInterval<T>> node)
            {
                //root
                if (node.Parent == null)
                {
                    Root.Right.Parent = null;
                    Root = Root.Right;
                    Root.NodeColor = RedBlackTreeNodeColor.Black;
                }
                else
                {
                    //node is left child of parent
                    if (node.IsLeftChild)
                    {
                        node.Parent.Left = node.Right;
                    }
                    //node is right child of parent
                    else
                    {
                        node.Parent.Right = node.Right;
                    }

                    node.Right.Parent = node.Parent;

                    if (node.Right.NodeColor != RedBlackTreeNodeColor.Red)
                    {
                        return;
                    }

                    //black deletion! But we can take its red child and recolor it to black
                    //and we are done!
                    node.Right.NodeColor = RedBlackTreeNodeColor.Black;
                }
            }

            /// <summary>
            /// deletes left node
            /// </summary>
            /// <param name="node"></param>
            private void deleteLeftNode(AsIntervalRedBlackTreeNode<AsInterval<T>> node)
            {
                //root
                if (node.Parent == null)
                {
                    Root.Left.Parent = null;
                    Root = Root.Left;
                    Root.NodeColor = RedBlackTreeNodeColor.Black;
                }
                else
                {
                    //node is left child of parent
                    if (node.IsLeftChild)
                    {
                        node.Parent.Left = node.Left;
                    }
                    //node is right child of parent
                    else
                    {
                        node.Parent.Right = node.Left;
                    }

                    node.Left.Parent = node.Parent;

                    if (node.Left.NodeColor != RedBlackTreeNodeColor.Red)
                    {
                        return;
                    }

                    //black deletion! But we can take its red child and recolor it to black
                    //and we are done!
                    node.Left.NodeColor = RedBlackTreeNodeColor.Black;
                }
            }

            private AsIntervalRedBlackTreeNode<AsInterval<T>> handleDoubleBlack(AsIntervalRedBlackTreeNode<AsInterval<T>> node)
            {
                //case 1
                if (node == Root)
                {
                    node.NodeColor = RedBlackTreeNodeColor.Black;
                    return null;
                }

                //case 2
                if (node.Parent != null
                     && node.Parent.NodeColor == RedBlackTreeNodeColor.Black
                     && node.Sibling != null
                     && node.Sibling.NodeColor == RedBlackTreeNodeColor.Red
                     && (node.Sibling.Left == null && node.Sibling.Right == null
                     || node.Sibling.Left != null && node.Sibling.Right != null
                                                  && node.Sibling.Left.NodeColor == RedBlackTreeNodeColor.Black
                                                  && node.Sibling.Right.NodeColor == RedBlackTreeNodeColor.Black))
                {
                    node.Parent.NodeColor = RedBlackTreeNodeColor.Red;
                    node.Sibling.NodeColor = RedBlackTreeNodeColor.Black;

                    if (node.Sibling.IsRightChild)
                    {
                        leftRotate(node.Parent);
                    }
                    else
                    {
                        rightRotate(node.Parent);
                    }

                    return node;
                }
                //case 3
                if (node.Parent != null
                 && node.Parent.NodeColor == RedBlackTreeNodeColor.Black
                 && node.Sibling != null
                 && node.Sibling.NodeColor == RedBlackTreeNodeColor.Black
                 && (node.Sibling.Left == null && node.Sibling.Right == null
                 || node.Sibling.Left != null && node.Sibling.Right != null
                                              && node.Sibling.Left.NodeColor == RedBlackTreeNodeColor.Black
                                              && node.Sibling.Right.NodeColor == RedBlackTreeNodeColor.Black))
                {
                    //pushed up the double black problem up to parent
                    //so now it needs to be fixed
                    node.Sibling.NodeColor = RedBlackTreeNodeColor.Red;
                    return node.Parent;
                }


                //case 4
                if (node.Parent != null
                     && node.Parent.NodeColor == RedBlackTreeNodeColor.Red
                     && node.Sibling != null
                     && node.Sibling.NodeColor == RedBlackTreeNodeColor.Black
                     && (node.Sibling.Left == null && node.Sibling.Right == null
                     || node.Sibling.Left != null && node.Sibling.Right != null
                                                  && node.Sibling.Left.NodeColor == RedBlackTreeNodeColor.Black
                                                  && node.Sibling.Right.NodeColor == RedBlackTreeNodeColor.Black))
                {
                    //just swap the color of parent and sibling
                    //which will compensate the loss of black count 
                    node.Parent.NodeColor = RedBlackTreeNodeColor.Black;
                    node.Sibling.NodeColor = RedBlackTreeNodeColor.Red;

                    return null;
                }


                //case 5
                if (node.Parent != null
                    && node.Parent.NodeColor == RedBlackTreeNodeColor.Black
                    && node.Sibling != null
                    && node.Sibling.IsRightChild
                    && node.Sibling.NodeColor == RedBlackTreeNodeColor.Black
                    && node.Sibling.Left != null
                    && node.Sibling.Left.NodeColor == RedBlackTreeNodeColor.Red
                    && node.Sibling.Right != null
                    && node.Sibling.Right.NodeColor == RedBlackTreeNodeColor.Black)
                {
                    node.Sibling.NodeColor = RedBlackTreeNodeColor.Red;
                    node.Sibling.Left.NodeColor = RedBlackTreeNodeColor.Black;
                    rightRotate(node.Sibling);

                    return node;
                }

                //case 5 mirror
                if (node.Parent != null
                   && node.Parent.NodeColor == RedBlackTreeNodeColor.Black
                   && node.Sibling != null
                   && node.Sibling.IsLeftChild
                   && node.Sibling.NodeColor == RedBlackTreeNodeColor.Black
                   && node.Sibling.Left != null
                   && node.Sibling.Left.NodeColor == RedBlackTreeNodeColor.Black
                   && node.Sibling.Right != null
                   && node.Sibling.Right.NodeColor == RedBlackTreeNodeColor.Red)
                {
                    node.Sibling.NodeColor = RedBlackTreeNodeColor.Red;
                    node.Sibling.Right.NodeColor = RedBlackTreeNodeColor.Black;
                    leftRotate(node.Sibling);

                    return node;
                }

                //case 6
                if (node.Parent != null
                    && node.Parent.NodeColor == RedBlackTreeNodeColor.Black
                    && node.Sibling != null
                    && node.Sibling.IsRightChild
                    && node.Sibling.NodeColor == RedBlackTreeNodeColor.Black
                    && node.Sibling.Right != null
                    && node.Sibling.Right.NodeColor == RedBlackTreeNodeColor.Red)
                {
                    //left rotate to increase the black count on left side by one
                    //and mark the red right child of sibling to black 
                    //to compensate the loss of Black on right side of parent
                    node.Sibling.Right.NodeColor = RedBlackTreeNodeColor.Black;
                    leftRotate(node.Parent);

                    return null;
                }

                //case 6 mirror
                if (node.Parent != null
                  && node.Parent.NodeColor == RedBlackTreeNodeColor.Black
                  && node.Sibling != null
                  && node.Sibling.IsLeftChild
                  && node.Sibling.NodeColor == RedBlackTreeNodeColor.Black
                  && node.Sibling.Left != null
                  && node.Sibling.Left.NodeColor == RedBlackTreeNodeColor.Red)
                {
                    //right rotate to increase the black count on right side by one
                    //and mark the red left child of sibling to black
                    //to compensate the loss of Black on right side of parent
                    node.Sibling.Left.NodeColor = RedBlackTreeNodeColor.Black;
                    rightRotate(node.Parent);

                    return null;
                }

                return null;
            }

        }
    }


}
