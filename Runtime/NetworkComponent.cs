using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Fab.Network
{
    /// <summary>
    /// A MonoBehaviour representing a Network
    /// </summary>
    [AddComponentMenu("Network/Network")]
    public class NetworkComponent : MonoBehaviour
    {
        private UndirectedGraph network;
        public UndirectedGraph Network => network;

        [SerializeField]
        private GameObject nodePrefab;

        [SerializeField]
        private LineRenderer edgePrefab;

        private Dictionary<GameObject, Node<GameObject>> nodeByGOs;

        public enum ChangeEventType
        {
            Added, 
            Removed,
            Connected,
            Disconnected,
            Updated,
            Cleared
        }

        /// <summary>
        /// Called every time the network was changed
        /// </summary>
        public event Action<ChangeEventType> changed;

        private void Awake()
        {
            network = new UndirectedGraph();
            nodeByGOs = new Dictionary<GameObject, Node<GameObject>>();

        }

        /// <summary>
        /// Adds a node at the given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Node<GameObject> AddNode(Vector3 position)
        {
            Node<GameObject> node = network.AddNode<GameObject>();
            GameObject nodeGO = Instantiate(nodePrefab, position, Quaternion.identity, transform);
            node.Data = nodeGO;

            nodeByGOs.Add(nodeGO, node);

            changed?.Invoke(ChangeEventType.Added);
            return node;
        }

        /// <summary>
        /// Removes a node from the network
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool RemoveNode(Node<GameObject> node)
        {
            if (node.Owner != network)
                return false;

            IReadOnlyList<Edge> edges = network.GetNodeEdges(node);

            if (network.RemoveNode(node))
            {
                nodeByGOs.Remove(node.Data);
                Destroy(node.Data);
                foreach (Edge e in edges)
                    Destroy(((Edge<LineRenderer>)e).Data.gameObject);

                changed?.Invoke(ChangeEventType.Removed);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears the whole network
        /// </summary>
        /// <returns></returns>
        public void Clear()
        {
            nodeByGOs.Clear();

            foreach (Node n in network.Nodes)
                Destroy(((Node<GameObject>)n).Data);

            foreach (Edge e in network.Edges)
                Destroy(((Edge<LineRenderer>)e).Data.gameObject);
     
            network.Clear();
            changed?.Invoke(ChangeEventType.Cleared);
        }

        /// <summary>
        /// Attempts to get the node represented by the given gameObject
        /// </summary>
        /// <param name="nodeGO"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool TryGetNode(GameObject nodeGO, out Node<GameObject> node)
        {
            return nodeByGOs.TryGetValue(nodeGO, out node);
        }

        /// <summary>
        /// Connects two nodes
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public Edge<LineRenderer> ConnectNodes(Node<GameObject> a, Node<GameObject> b)
        {
            Edge<LineRenderer> edge = network.ConnectNodes<LineRenderer>(a, b);

            LineRenderer edgeInst = Instantiate(edgePrefab, transform);
            edgeInst.SetPositions(new Vector3[]
            {
                a.Data.transform.position,
                b.Data.transform.position
            });

            edge.Data = edgeInst;
            changed?.Invoke(ChangeEventType.Connected);
            return edge;
        }

        /// <summary>
        /// Disconnects two nodes connected by an edge.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public bool DisconnectNodes(Edge edge)
        {
            if (network.DisconnectNodes(edge))
            {
                changed?.Invoke(ChangeEventType.Disconnected);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the position of a node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="position"></param>
        public void UpdateNodePosition(Node<GameObject> node, Vector3 position)
        {
            node.Data.transform.position = position;
            // update all edges
            foreach (Edge e in network.GetNodeEdges(node))
            {
                if (e.A == node)
                    ((Edge<LineRenderer>)e).Data.SetPosition(0, position);
                else
                    ((Edge<LineRenderer>)e).Data.SetPosition(1, position);
            }
            changed?.Invoke(ChangeEventType.Updated);
        }

        /// <summary>
        /// Fills the supplied list with all node's position
        /// </summary>
        /// <param name="positions"></param>
        public void GetNodePositions(List<Vector3> positions)
        {
            positions.Clear();
            foreach (Node n in network.Nodes)
                positions.Add(((Node<GameObject>)n).Data.transform.position);
        }
    }
}
