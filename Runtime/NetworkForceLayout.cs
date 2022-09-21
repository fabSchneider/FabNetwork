using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Fab.Network
{
    /// <summary>
    /// Class for force driven layouting of network nodes 
    /// </summary>
    public class NetworkForceLayout : IDisposable
    {
        public float RepulsionFactor { get; set; } = 1f;
        public float AttractionFactor { get; set; } = 1f;

        private NativeArray<float3> nodes;
        private NativeArray<int2> edges;
        private NativeArray<float> springLengths;

        public IEnumerable<float3> Nodes => nodes;

        public float ConvergeThreshold { get; set; } = 0.1f;

        private float totalEnergy = float.PositiveInfinity;
        public bool Converged => totalEnergy < ConvergeThreshold;

        /// <summary>
        /// Builds the graph representation for layouting
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="edges"></param>
        /// <param name="initialPositions"></param>
        public void Build(List<Node> nodes, List<Edge> edges, List<float3> initialPositions, List<float> springLengths)
        {
            if (this.nodes.IsCreated)
                Dispose();

            this.nodes = new NativeArray<float3>(nodes.Count, Allocator.Persistent);
            for (int i = 0; i < initialPositions.Count; i++)
            {
                this.nodes[i] = initialPositions[i];
            }

            this.edges = new NativeArray<int2>(edges.Count, Allocator.Persistent);
            this.springLengths = new NativeArray<float>(springLengths.Count, Allocator.Persistent);
            for (int i = 0; i < edges.Count; i++)
            {
                Edge e = edges[i];
                this.edges[i] = new int2(nodes.IndexOf(e.A), nodes.IndexOf(e.B));
                this.springLengths[i] = springLengths[i];
            }
            totalEnergy = float.PositiveInfinity;
        }

        public void Iterate()
        {
            totalEnergy = 0f;

            // schedule repulsion and attraction jobs
            CalculateRepulsionForceJob repulsionForceJob = new CalculateRepulsionForceJob()
            {
                repulsionFactor = RepulsionFactor,
                nodes = nodes,
                forces = new NativeArray<float3>(nodes.Length, Allocator.TempJob)
            };

            CalculateAttractionForceJob attractionForceJob = new CalculateAttractionForceJob()
            {
                attractionFactor = AttractionFactor,
                nodes = nodes,
                edges = edges,
                springLengths = springLengths,
                forces = new NativeArray<float3>(edges.Length, Allocator.TempJob)
            };

            JobHandle repulsionHandle = repulsionForceJob.Schedule(nodes.Length, 8);
            JobHandle attractionHandle = attractionForceJob.Schedule(edges.Length, 8);

            JobHandle.CompleteAll(ref repulsionHandle, ref attractionHandle);

            NativeArray<float3> forces = new NativeArray<float3>(nodes.Length, Allocator.TempJob);

            // accumulate forces and apply them to the nodes
            for (int i = 0; i < forces.Length; i++)
            {
                forces[i] += repulsionForceJob.forces[i];
            }

            for (int i = 0; i < edges.Length; i++)
            {
                int2 edge = edges[i];
                forces[edge.x] += attractionForceJob.forces[i];
                forces[edge.y] -= attractionForceJob.forces[i];
            }

            for (int i = 0; i < forces.Length; i++)
            {
                float3 force = forces[i];
                nodes[i] += force;
                totalEnergy += math.length(force);
            }

            forces.Dispose();
            repulsionForceJob.forces.Dispose();
            attractionForceJob.forces.Dispose();
        }

        [BurstCompile]
        public struct CalculateRepulsionForceJob : IJobParallelFor
        {
            public float repulsionFactor;

            [ReadOnly]
            public NativeArray<float3> nodes;
            public NativeArray<float3> forces;

            public void Execute(int index)
            {
                float3 posA = nodes[index];

                for (int i = 0; i < nodes.Length; i++)
                {
                    float3 posB = nodes[i];
                    float dist = math.distance(posA, posB);
                    float proximity = math.max(dist, 1f);
                    float force = repulsionFactor / (proximity * proximity);
                    forces[index] += ((posA - posB) / proximity) * force;
                }
            }
        }

        [BurstCompile]
        public struct CalculateAttractionForceJob : IJobParallelFor
        {
            public float attractionFactor;

            [ReadOnly]
            public NativeArray<float3> nodes;
            [ReadOnly]
            public NativeArray<int2> edges;
            [ReadOnly]
            public NativeArray<float> springLengths;
            public NativeArray<float3> forces;

            public void Execute(int index)
            {
                int2 edge = edges[index];
                float3 posA = nodes[edge.x];
                float3 posB = nodes[edge.y];
                float dist = math.distance(posA, posB);
                float force = attractionFactor * (dist - springLengths[index]);
                forces[index] = ((posB - posA) / dist) * force;
            }
        }

        public void Dispose()
        {
            nodes.Dispose();
            edges.Dispose();
            springLengths.Dispose();
        }
    }
}
