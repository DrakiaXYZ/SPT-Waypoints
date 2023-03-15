using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace DrakiaXYZ.Waypoints.Components
{
    internal class NavMeshDebugComponent : MonoBehaviour, IDisposable
    {
        private static NavMeshDebugComponent instance;
        private NavMeshTriangulation meshData;

        public static NavMeshDebugComponent Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NavMeshDebugComponent();
                }

                return instance;
            }
        }

        public static bool Instantiated
        {
            get
            {
                return instance != null;
            }
        }

        public void Dispose()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var navMeshDebugComponentObject = gameWorld.GetComponent<NavMeshDebugComponent>();
            Destroy(navMeshDebugComponentObject);
            instance = null;
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            if (!Singleton<IBotGame>.Instantiated)
            {
                Console.WriteLine("Can't create NavMeshDebuh with no BotGame");
                return;
            }

            // Setup our gameObject
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();

            //sphere.GetComponent<Renderer>().material.color = color;

            // Build a dictionary of sub areas
            meshData = NavMesh.CalculateTriangulation();
            Console.WriteLine($"NavMeshTriangulation Found. Areas: {meshData.areas.Length}");

            //string jsonString = JsonConvert.SerializeObject(meshData, Formatting.Indented);
            //string exportFile = "navMesh.json";
            //if (File.Exists(exportFile))
            //{
            //    File.Delete(exportFile);
            //}
            //File.Create(exportFile).Dispose();
            //StreamWriter streamWriter = new StreamWriter(exportFile);
            //streamWriter.Write(jsonString);
            //streamWriter.Flush();
            //streamWriter.Close();

            Dictionary<int, List<int>> submeshIndices = new Dictionary<int, List<int>>();
            int currentGroup = 0;
            for (int i = 0; i < meshData.indices.Length; i += 3)
            {
                if (!submeshIndices.ContainsKey(currentGroup))
                {
                    submeshIndices.Add(currentGroup, new List<int>());
                }

                submeshIndices[currentGroup].Add(meshData.indices[i]);
                submeshIndices[currentGroup].Add(meshData.indices[i + 1]);
                submeshIndices[currentGroup].Add(meshData.indices[i + 2]);

                float y1 = meshData.vertices[meshData.indices[i]].y;
                float y2 = meshData.vertices[meshData.indices[i + 1]].y;
                float y3 = meshData.vertices[meshData.indices[i + 2]].y;
                float yDiffMax = Math.Max(Math.Abs(y1 - y2), Math.Max(Math.Abs(y1 - y3), y2 - y3));
                if (yDiffMax > 5f)
                {
                    Console.WriteLine($"Triangle {i} has a y-diff greater than 5: {yDiffMax}");
                }

                if (i > 50000)
                {
                    currentGroup++;
                }
            }

            // Create as many materials as we have sub meshes
            Material baseMaterial = gameObject.GetComponent<MeshRenderer>().material;
            List<Material> materials = new List<Material>();
            for (int i = 0; i < submeshIndices.Count; i++)
            {
                materials.Add(new Material(baseMaterial));
            }
            gameObject.GetComponent<MeshRenderer>().materials = materials.ToArray();

            // Create our new mesh and add all the vertices
            Mesh mesh = new Mesh();
            mesh.vertices = meshData.vertices;

            // Add sub meshes
            Console.WriteLine($"SubMesh Count: {submeshIndices.Count}");
            mesh.subMeshCount = submeshIndices.Count;
            int index = 0;
            foreach (var entry in submeshIndices)
            {
                mesh.SetTriangles(entry.Value.ToArray(), index++);
            }

            // Set mesh of our gameObject
            GetComponent<MeshFilter>().mesh = mesh;
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<NavMeshDebugComponent>();
            }
        }

        public static void Disable()
        {
            if (Instantiated)
            {
                Instance.Dispose();
            }
        }
    }
}
