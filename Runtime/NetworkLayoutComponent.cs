using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Fab.Network
{
    /// <summary>
    /// MonoBehaviour for automatic layouting of a <see cref="NetworkComponent"/> 
    /// </summary>
    [RequireComponent(typeof(NetworkComponent))]
    [AddComponentMenu("Network/Network Layout")]
    public class NetworkLayoutComponent : MonoBehaviour
    {
        private NetworkComponent networkComponent;

        private NetworkForceLayout forceLayout;

        /// <summary>
        /// Event invoked every time the layout has gone through a cycle of iterations
        /// </summary>
        public event Action layoutChanged;

        /// <summary>
        /// Event invoked when the layouting has converged
        /// </summary>
        public event Action layoutConverged;

        /// <summary>
        /// Factor of repulsion exerted in between all nodes 
        /// </summary>
        [Tooltip("Factor of repulsion exerted in between all nodes ")]
        public float repulsionFactor = 0.025f;

        /// <summary>
        /// Factor of attraction exerted on each connected node
        /// </summary>
        [Tooltip("Factor of attraction exerted on each connected node")]
        public float attractionFactor = 0.1f;

        /// <summary>
        /// Threshold of total forces at which the layouting is considered converged
        /// </summary>
        [Tooltip("Threshold of total forces at which the layouting is considered converged")]
        public float convergeThreshold = 0.05f;

        /// <summary>
        /// Maximum number of iterations per frame
        /// </summary>
        [Tooltip("Maximum number of iterations per frame")]
        public int maxIterations = 200;

        [SerializeField]
        [Tooltip("Toggles whether the network automatically will re-layout when it has been changed.")]
        private bool autoLayout = true;

        [SerializeField]
        [Tooltip("When activated keeps the x coordinate of the nodes constant.")]
        private bool constraintX;
        [SerializeField]
        [Tooltip("When activated keeps the y coordinate of the nodes constant.")]
        private bool constraintY;
        [SerializeField]
        [Tooltip("When activated keeps the z coordinate of the nodes constant.")]
        private bool constraintZ;

        /// <summary>
        /// Toggles whether the network automatically will be re-layout when it has been changed.
        /// </summary>
        public bool AutoLayout { get => autoLayout; set => autoLayout = value; }

        // start with the changed flag set to true
        // so that the layouting will definitely happen
        // the first time this component is added
        // (when auto layout is enabled)
        private bool changedFlag = true;

        /// <summary>
        /// Has the layouting been completed
        /// </summary>
        public bool IsConverged => forceLayout.Converged;

        void Awake()
        {
            networkComponent = GetComponent<NetworkComponent>();
            networkComponent.changed += OnNetworkChanged;
            forceLayout = new NetworkForceLayout();

            forceLayout.RepulsionFactor = repulsionFactor;
            forceLayout.AttractionFactor = attractionFactor;
            forceLayout.ConvergeThreshold = convergeThreshold;
        }

        private void OnValidate()
        {
            if (forceLayout != null)
            {
                forceLayout.RepulsionFactor = repulsionFactor;
                forceLayout.AttractionFactor = attractionFactor;
                forceLayout.ConvergeThreshold = convergeThreshold;
            }

            changedFlag = true;
        }

        private void Update()
        {
            if (autoLayout)
            {
                if(changedFlag)
                {
                    Layout();
                    changedFlag = false;
                }
            }
        }

        private void OnDestroy()
        {
            forceLayout.Dispose();

            if(networkComponent)
                networkComponent.changed -= OnNetworkChanged;
        }

        private void OnNetworkChanged(NetworkComponent.ChangeEventType changeEvent)
        {
            changedFlag = true;
        }

        /// <summary>
        /// Triggers the layouting of the network.
        /// </summary>
        public void Layout()
        {
            // stop any currently running layouting routines
            StopAllCoroutines();

            // build the graph representation for the force layout
            UndirectedGraph network = networkComponent.Network;

            List<float3> pos = new List<float3>(network.Nodes.Count);
            List<float> springLengths = new List<float>(network.Edges.Count);

            foreach (var n in network.Nodes)
            {
                Vector3 p = ((Node<GameObject>)n).Data.transform.position;
                pos.Add(new float3(p.x, p.y, p.z));
            }

            foreach (Edge e in network.Edges)
            {
                springLengths.Add(1f);
            }

            forceLayout.Build(network.Nodes, network.Edges, pos, springLengths);
            StartCoroutine(LayoutCoroutine());
        }

        private IEnumerator LayoutCoroutine()
        {
            // handles iteration across multiple frames
            // until the network has converged

            while (!IterateLayout())
            {
                // apply the changes of the layouting 
                // and raise the change event once every frame
                ApplyLayout();
                layoutChanged?.Invoke();
                yield return null;
            }

            ApplyLayout();
            layoutConverged?.Invoke();
        }

        private bool IterateLayout()
        {
            // handles the iteration for one frame
            // iterate the layouting until either the max iterations are
            // reached or the network has converged
            // return true only when the network has converged
            for (int i = 0; i < maxIterations; i++)
            {
                forceLayout.Iterate();

                if (forceLayout.Converged)
                    return true;
            }
            return false;
        }

        private void ApplyLayout()
        {
            UndirectedGraph network = networkComponent.Network;

            //update all points
            int i = 0;
            foreach (float3 node in forceLayout.Nodes)
            {
                float3 pos = node;

                float x = constraintX ? 0f : pos.x;
                float y = constraintY ? 0f : pos.y;
                float z = constraintZ ? 0f : pos.z;
                ((Node<GameObject>)network.Nodes[i++]).Data.transform.position = new Vector3(x, y, z);
            }

            // update all edges
            foreach (Edge e in network.Edges)
            {
                ((Edge<LineRenderer>)e).Data.SetPosition(0, ((Node<GameObject>)e.A).Data.transform.position);
                ((Edge<LineRenderer>)e).Data.SetPosition(1, ((Node<GameObject>)e.B).Data.transform.position);
            }
        }
    }
}
