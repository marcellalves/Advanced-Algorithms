﻿using Advanced.Algorithms.DataStructures.Graph.AdjacencyList;
using Advanced.Algorithms.Sorting;
using System;
using System.Collections.Generic;
using Advanced.Algorithms.DataStructures;

namespace Advanced.Algorithms.Graph
{
    /// <summary>
    /// MST edge object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="W"></typeparam>
    public class MSTEdge<T, W> : IComparable where W: IComparable
    {
        public T Source { get; }
        public T Destination { get; }
        public W Weight { get;  }

        internal MSTEdge(T source, T dest, W weight)
        {
            Source = source;
            Destination = dest;
            Weight = weight;
        }
        public int CompareTo(object obj)
        {
            return Weight.CompareTo(((MSTEdge<T, W>) obj).Weight);
        }
    }

    /// <summary>
    /// A Kruskal's alogorithm implementation
    /// using merge sort & disjoint set
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TW"></typeparam>
    public class Kruskals<T, TW> where TW : IComparable
    {
        /// <summary>
        /// Find Minimum Spanning Tree of given weighted graph
        /// </summary>
        /// <param name="graph"></param>
        /// <returns>List of MST edges</returns>
        public List<MSTEdge<T,TW>>
            FindMinimumSpanningTree(WeightedGraph<T, TW> graph)
        {
            var edges = new List<MSTEdge<T,TW>>();


            //gather all unique edges
            dfs(graph.ReferenceVertex, new System.Collections.Generic.HashSet<T>(), 
                new Dictionary<T, System.Collections.Generic.HashSet<T>>(),
                edges);

            //quick sort preparation
            var sortArray = new MSTEdge<T,TW>[edges.Count];
            for (int i = 0; i < edges.Count; i++)
            {
                sortArray[i] = edges[i];
            }

            //quick sort edges
            var sortedEdges = MergeSort<MSTEdge<T,TW>>.Sort(sortArray);

            var result = new List<MSTEdge<T,TW>>();
            var disJointSet = new DisJointSet<T>();

            //create set
            foreach (var vertex in graph.Vertices)
            {
                disJointSet.MakeSet(vertex.Key);
            }


            //pick each edge one by one
            //if both source & target belongs to same set 
            //then don't add the edge to result
            //otherwise add it to result and union sets
            for (int i = 0; i < edges.Count; i++)
            {
                var currentEdge = sortedEdges[i];

                var setA = disJointSet.FindSet(currentEdge.Source);
                var setB = disJointSet.FindSet(currentEdge.Destination);

                //can't pick edge with both ends already in MST
                if (setA.Equals(setB))
                {
                    continue;
                }

                result.Add(currentEdge);

                //union picked edge vertice sets
                disJointSet.Union(setA, setB);
            }

            return result;
        }

        /// <summary>
        /// Do DFS to find all unique edges
        /// </summary>
        /// <param name="currentVertex"></param>
        /// <param name="visitedVertices"></param>
        /// <param name="visitedEdges"></param>
        /// <param name="result"></param>
        private void dfs(WeightedGraphVertex<T, TW> currentVertex, System.Collections.Generic.HashSet<T> visitedVertices, Dictionary<T, System.Collections.Generic.HashSet<T>> visitedEdges,
            List<MSTEdge<T,TW>> result)
        {
            if (!visitedVertices.Contains(currentVertex.Value))
            {
                visitedVertices.Add(currentVertex.Value);

                foreach (var edge in currentVertex.Edges)
                {
                    if (!visitedEdges.ContainsKey(currentVertex.Value)
                        || !visitedEdges[currentVertex.Value].Contains(edge.Key.Value))
                    {
                        result.Add(new MSTEdge<T, TW>(currentVertex.Value, edge.Key.Value, edge.Value));

                        //update visited edge
                        if(!visitedEdges.ContainsKey(currentVertex.Value))
                        {
                            visitedEdges.Add(currentVertex.Value, new System.Collections.Generic.HashSet<T>());
                        }

                        visitedEdges[currentVertex.Value].Add(edge.Key.Value);

                        //update visited back edge
                        if (!visitedEdges.ContainsKey(edge.Key.Value))
                        {
                            visitedEdges.Add(edge.Key.Value, new System.Collections.Generic.HashSet<T>());
                        }

                        visitedEdges[edge.Key.Value].Add(currentVertex.Value);
                    }

                    dfs(edge.Key, visitedVertices, visitedEdges, result);
                }

            }
        
        }

    }
}
