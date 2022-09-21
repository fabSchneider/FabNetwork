using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fab.Network
{ 
    /// <summary>
    /// MonoBehaviour for runtime network building
    /// </summary>
    [AddComponentMenu("Network/Network Builder")]
    [RequireComponent(typeof(NetworkComponent))]
    public class NetworkBuilder : MonoBehaviour
    {
        private NetworkComponent networkComponent;
        private Camera _camera;

        private Node<GameObject> selectedNode;
        public Node<GameObject> SelectedNode
        {
            get => selectedNode;
            set
            {
                if (selectedNode == value)
                    return;

                // deselect current (check if data of that node is null in case it has been destroyed
                if (selectedNode != null && selectedNode.Data != null)
                    selectedNode.Data.GetComponent<Renderer>().material.color = Color.white;

                selectedNode = null;

                //select new node
                if (value != null)
                {
                    selectedNode = value;
                    selectedNode.Data.GetComponent<Renderer>().material.color = Color.red;
                }
            }
        }

        private Node<GameObject> lockedNodes;

        private void Awake()
        {
            networkComponent = GetComponent<NetworkComponent>();
            _camera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // if no node is selected
                if (SelectedNode == null)
                {
                    // check if a node is under the pointer and select in that case
                    if (TryGetNodeAtScreenPos(Input.mousePosition, out Node<GameObject> node))
                        SelectedNode = node;
                    // if not and shift is pressed add a node and select it
                    else if (Input.GetKey(KeyCode.LeftShift))
                    {
                        SelectedNode = AddNode(Input.mousePosition);
                    }

                }
                // if a node is selected
                else
                {
                    // check if a node is under the pointer
                    if (TryGetNodeAtScreenPos(Input.mousePosition, out Node<GameObject> node))
                    {
                        if (node != SelectedNode)
                        {
                            // if the node is not the same as the selected node and shift is hold down
                            // connect the two nodes
                            if (Input.GetKey(KeyCode.LeftShift))
                            {
                                try
                                {
                                    networkComponent.ConnectNodes(SelectedNode, node);
                                }
                                catch (InvalidOperationException)
                                {
                                    Debug.LogWarning("Nodes are already connected!");
                                }

                            }
                            //select the node
                            SelectedNode = node;
                        }
                    }
                    // if no node is under the pointer and shift is hold down add a new node
                    // and connect it to the selected node
                    else if (Input.GetKey(KeyCode.LeftShift))
                    {
                        node = AddNode(Input.mousePosition);
                        try
                        {
                            networkComponent.ConnectNodes(SelectedNode, node);
                        }
                        catch (InvalidOperationException)
                        {
                            Debug.LogWarning("Nodes are already connected!");
                        }
                        SelectedNode = node;
                    }
                    // otherwise deselect the current node
                    else
                    {
                        SelectedNode = null;
                    }
                }
            }

            // if a node is selected,
            else if (SelectedNode != null)
            {
                // if delete is pressed remove that node from the network.
                if (Input.GetKeyDown(KeyCode.Delete))
                {
                    networkComponent.RemoveNode(SelectedNode);

                    SelectedNode = null;
                }
                // if the mouse button is hold down (without shift) move the node.
                else if (Input.GetMouseButton(0) &&
                    !Input.GetKey(KeyCode.LeftShift))
                {
                    MoveNode(SelectedNode, Input.mousePosition);
                }
            }
        }

        private void MoveNode(Node<GameObject> node, Vector2 screenPos)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(networkComponent.transform.up, 0f);
            if (plane.Raycast(ray, out float enter))
                networkComponent.UpdateNodePosition(node, ray.GetPoint(enter));
        }

        private Node<GameObject> AddNode(Vector2 screenPos)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(networkComponent.transform.up, 0f);
            if (plane.Raycast(ray, out float enter))
            {
                Node<GameObject> node = networkComponent.AddNode(ray.GetPoint(enter));
                return node;
            }
            return null;
        }

        private bool TryGetNodeAtScreenPos(Vector2 screenPos, out Node<GameObject> node)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
                return networkComponent.TryGetNode(hit.collider.gameObject, out node);

            node = null;
            return false;
        }


    }
}

