using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fab.Network
{
    /// <summary>
    /// A class representing a reference to a node in a graph
    /// </summary>
    public class Node
    {
        protected readonly UndirectedGraph owner;
        public UndirectedGraph Owner => owner;

        public Node(UndirectedGraph owner)
        {
            this.owner = owner;
        }
    }

    /// <summary>
    /// A class representing a reference to a node in a graph holding data of type T
    /// </summary>
    public class Node<T> : Node
    {
        private T data;

        public Node(UndirectedGraph owner) : base(owner)
        {
        }

        public T Data { get => data; set => data = value; }
    }

    /// <summary>
    /// A class representing a reference to an edge in the graph.
    /// </summary>
    public class Edge
    {
        protected readonly Node a, b;
        protected readonly UndirectedGraph owner;

        public UndirectedGraph Owner => owner;
        public Node A => a;
        public Node B => b;

        public Edge(UndirectedGraph owner, Node a, Node b)
        {
            this.owner = owner;
            this.a = a;
            this.b = b;
        }
    }

    /// <summary>
    /// A class representing a reference to an edge in the graph holding data of type T
    /// </summary>
    public class Edge<T> : Edge
    {
        private T data;

        public Edge(UndirectedGraph owner, Node a, Node b) : base(owner, a, b)
        {
        }

        public T Data { get => data; set => data = value; }
    }

    /// <summary>
    /// An undirected graph
    /// </summary>
    public class UndirectedGraph
    {
        private List<Node> nodes;
        private List<Edge> edges;

        public List<Node> Nodes => nodes;

        public List<Edge> Edges => edges;

        private Dictionary<Node, List<Edge>> edgesByNodes;

        public UndirectedGraph()
        {
            nodes = new List<Node>();
            edges = new List<Edge>();

            edgesByNodes = new Dictionary<Node, List<Edge>>();
        }

        /// <summary>
        /// Adds a node to the graph
        /// </summary>
        /// <param name="position"></param>
        public Node<T> AddNode<T>()
        {
            Node<T> node = new Node<T>(this);
            nodes.Add(node);
            edgesByNodes.Add(node, new List<Edge>());
            return node;
        }

        /// <summary>
        /// Removes a node from the graph;
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Returns true if the node was successfully removed.</returns>
        public bool RemoveNode(Node node)
        {
            if (nodes.Remove(node))
            {
                //remove all edges connecting this node
                foreach (Edge e in edgesByNodes[node])
                {
                    edges.Remove(e);
                    //remove all the references in the other nodes connected by this edge
                    Node otherNode = e.A == node ? e.B : e.A;
                    edgesByNodes[otherNode].Remove(e);
                }
                edgesByNodes.Remove(node);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Connect two nodes.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public Edge ConnectNodes(Node a, Node b)
        {
            CheckCanConnect(a, b);
            Edge edge = new Edge(this, a, b);
            edges.Add(edge);
            edgesByNodes[a].Add(edge);
            edgesByNodes[b].Add(edge);

            return edge;
        }

        /// <summary>
        /// Connect two nodes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public Edge<T> ConnectNodes<T>(Node a, Node b)
        {
            CheckCanConnect(a, b);

            Edge<T> edge = new Edge<T>(this, a, b);
            edges.Add(edge);
            edgesByNodes[a].Add(edge);
            edgesByNodes[b].Add(edge);

            return edge;
        }

        /// <summary>
        /// Disconnects two nodes, removing the edge between them.
        /// </summary>
        /// <param name="edge"></param>
        public bool DisconnectNodes(Edge edge)
        {
            if (edges.Remove(edge))
            {
                edgesByNodes[edge.A].Remove(edge);
                edgesByNodes[edge.B].Remove(edge);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns all edges connecting this node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IReadOnlyList<Edge> GetNodeEdges(Node node)
        {
            return edgesByNodes[node];
        }

        /// <summary>
        /// Returns all nodes neighbored to this node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IEnumerable<Node> GetNeighbourNodes(Node node)
        {
            return GetNodeEdges(node).Select(e => e.A == node ? e.B : e.A);
        }

        /// <summary>
        /// Clears the complete graph
        /// </summary>
        public void Clear()
        {
            nodes.Clear();
            edges.Clear();
            edgesByNodes.Clear();
        }

        public IEnumerable DFS(Node root)
        {
            List<Node> visited = new List<Node>();

            visited.Add(root);
            foreach (Node neighbour in GetNeighbourNodes(root))
            {
                yield return DFSStep(visited, neighbour);
            }
        }

        private IEnumerable DFSStep(List<Node> visited, Node current)
        {
            foreach (Node neighbour in GetNeighbourNodes(current))
            {
                if (!visited.Contains(neighbour))
                {
                    visited.Add(neighbour);
                    yield return DFSStep(visited, neighbour);
                }
            }
        }

        public IEnumerable<Node> DepthFirstSearch(Node root)
        {
            var stack = new Stack<Node>();
            var visited = new HashSet<Node>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Node current = stack.Pop();
                if (visited.Add(current))
                {
                    yield return current;
                    foreach (Node neighbour in GetNeighbourNodes(current))
                    {
                        if (!visited.Contains(neighbour))
                        {
                            stack.Push(neighbour);
                        }
                    }
                }
            }
        }

        private void CheckCanConnect(Node a, Node b)
        {
            if (a.Owner != b.Owner)
                throw new InvalidOperationException("Nodes need to belong to the same network");

            //check if edge between nodes already exists
            foreach (Edge e in edgesByNodes[a])
            {
                if (e.A == b || e.B == b)
                    throw new InvalidOperationException("Nodes are already connected");
            }
        }

    }
}
