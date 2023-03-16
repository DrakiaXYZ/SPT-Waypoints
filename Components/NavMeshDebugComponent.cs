using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Components
{
    internal class NavMeshDebugComponent : MonoBehaviour, IDisposable
    {
        private NavMeshTriangulation meshData;
        private static List<UnityEngine.Object> gameObjects = new List<UnityEngine.Object>();

        public void Dispose()
        {
            gameObjects.ForEach(Destroy);
            gameObjects.Clear();
        }

        public void Start()
        {
            if (!Singleton<IBotGame>.Instantiated)
            {
                Console.WriteLine("Can't create NavMeshDebug with no BotGame");
                return;
            }

            // Setup our gameObject
            gameObjects.Add(gameObject.AddComponent<MeshFilter>());
            gameObjects.Add(gameObject.AddComponent<MeshRenderer>());

            // Build a dictionary of sub areas
            meshData = NavMesh.CalculateTriangulation();
            Console.WriteLine($"NavMeshTriangulation Found. Vertices: {meshData.vertices.Length}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // We need to limit each sub-mesh to 65,000 or less vertices, so just split it up into ~50,000 vector sections
            List<Vector3> newVerticesList = new List<Vector3>();
            List<Vector3> currentVerticesList = new List<Vector3>();
            List<List<int>> submeshIndices = new List<List<int>>();
            List<int> submeshVectorCounts = new List<int>();

            int currentSubmesh = 0;
            for (int i = 0; i < meshData.indices.Length; i += 3)
            {
                if (submeshIndices.Count <= currentSubmesh)
                {
                    submeshIndices.Add(new List<int>());
                }

                Vector3 v1 = meshData.vertices[meshData.indices[i]] + new Vector3(0f, 0.03f, 0f);
                Vector3 v2 = meshData.vertices[meshData.indices[i + 1]] + new Vector3(0f, 0.03f, 0f); ;
                Vector3 v3 = meshData.vertices[meshData.indices[i + 2]] + new Vector3(0f, 0.03f, 0f); ;

                // This will result in duplicate vectors, but it's faster than checking
                submeshIndices[currentSubmesh].Add(currentVerticesList.Count);
                currentVerticesList.Add(v1);
                submeshIndices[currentSubmesh].Add(currentVerticesList.Count);
                currentVerticesList.Add(v2);
                submeshIndices[currentSubmesh].Add(currentVerticesList.Count);
                currentVerticesList.Add(v3);

                if (currentVerticesList.Count > 50000)
                {
                    currentSubmesh++;
                    submeshVectorCounts.Add(currentVerticesList.Count);
                    newVerticesList.AddRange(currentVerticesList);
                    currentVerticesList.Clear();
                }
            }
            submeshVectorCounts.Add(currentVerticesList.Count);
            newVerticesList.AddRange(currentVerticesList);
            currentVerticesList.Clear();

            stopwatch.Stop();
            Console.WriteLine($"Broke navmesh up into {submeshIndices.Count} sections. Took {stopwatch.ElapsedMilliseconds}ms");

            // Create as many materials as we have sub meshes
            Material baseMaterial = new Material(Shader.Find("Standard"));
            baseMaterial.color = new Color(1.0f, 0.0f, 1.0f, 0.5f);

            List<Material> materials = new List<Material>();
            for (int i = 0; i < submeshIndices.Count; i++)
            {
                materials.Add(new Material(baseMaterial));
            }
            gameObject.GetComponent<MeshRenderer>().materials = materials.ToArray();

            // Create our new mesh and add all the vertices
            Mesh mesh = new Mesh();
            mesh.vertices = newVerticesList.ToArray();

            // Add sub meshes
            mesh.subMeshCount = submeshIndices.Count;
            int index = 0;
            int offset = 0;
            foreach (var submesh in submeshIndices)
            {
                mesh.SetTriangles(submesh.ToArray(), index, true, offset);
                offset += submeshVectorCounts[index];
                index++;
            }

            // Set mesh of our gameObject
            GetComponent<MeshFilter>().mesh = mesh;
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameObjects.Add(gameWorld.GetOrAddComponent<NavMeshDebugComponent>());
            }
        }

        public static void Disable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetComponent<NavMeshDebugComponent>()?.Dispose();
            }
        }
    }
}
