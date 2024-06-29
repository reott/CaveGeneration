using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ProceduralNoiseProject;
using Common.Unity.Drawing;

namespace MarchingCubesProject
{
    public enum MARCHING_MODE { CUBES, TETRAHEDRON };

    public class Example : MonoBehaviour
    {
        public Material material;
        public List<GameObject> mushroomPrefabs;
        public List<GameObject> plantPrefabs;
        public List<GameObject> crystalPrefabs;
        public List<GameObject> fireflyPrefabs;
        public MARCHING_MODE mode = MARCHING_MODE.CUBES;
        public int seed = 0;
        public int size;
        public int objHeight;
        public bool drawNormals = false;
        [Range(0, 1)]
        public float mushroomSpawnChance = 1;
        [Range(0, 1)]
        public float plantSpawnChance = 1;
        [Range(0, 1)]
        public float crystalSpawnChance = 1;
        [Range(0, 1)]
        public float fireflySpawnChance = 1;

        private List<GameObject> meshes = new List<GameObject>();
        private List<Vector3> mushroomPositions = new List<Vector3>();
        private List<Vector3> plantPositions = new List<Vector3>();
        private List<Vector3> crystalPositions = new List<Vector3>();
        private List<Vector3> fireflyPositions = new List<Vector3>();
        private NormalRenderer normalRenderer;

        void Start()
        {
            INoise perlin = new PerlinNoise(seed, 1.0f);
            FractalNoise fractal = new FractalNoise(perlin, 3, 1.0f);

            Marching marching = null;
            if (mode == MARCHING_MODE.TETRAHEDRON)
                marching = new MarchingTertrahedron();
            else
                marching = new MarchingCubes();

            marching.Surface = 0.0f;

            int width = size;
            int height = objHeight;
            int depth = size;

            var voxels = new VoxelArray(width, height, depth);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        float u = x / (width - 1.0f);
                        float v = y / (height - 1.0f);
                        float w = z / (depth - 1.0f);

                        voxels[x, y, z] = fractal.Sample3D(u, v, w);
                    }
                }
            }

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            marching.Generate(voxels.Voxels, verts, indices);

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 p = verts[i];
                float u = p.x / (width - 1.0f);
                float v = p.y / (height - 1.0f);
                float w = p.z / (depth - 1.0f);

                Vector3 n = voxels.GetNormal(u, v, w);
                normals.Add(n);
            }

            normalRenderer = new NormalRenderer();
            normalRenderer.DefaultColor = Color.red;
            normalRenderer.Length = 0.25f;
            normalRenderer.Load(verts, normals);

            for (int i = 0; i < verts.Count; i++)
            {
                float x, y, z;
                Vector3 p = verts[i];
                var normal = normals[i];
                x = Mathf.Abs(normal.x);
                y = Mathf.Abs(normal.y);
                z = Mathf.Abs(normal.z);

                if (x > y)
                {
                    if (x > z)
                    {
                        uvs.Add(new Vector2(p.y / (float)height, p.z / (float)size));
                    }
                    else
                    {
                        uvs.Add(new Vector2(p.x / (float)size, p.y / (float)height));
                    }
                }
                else if (y > z)
                {
                    uvs.Add(new Vector2(p.x / (float)size, p.z / (float)size));
                }
                else
                {
                    uvs.Add(new Vector2(p.x / (float)size, p.y / (float)height));
                }
            }

            var position = new Vector3(-width / 2, -height / 2, -depth / 2);
            CreateMesh32(verts, normals, indices, uvs, position);
            SpawnObjectsOnSurface(verts, normals, position);
            SpawnFireflies(verts, normals, position);
        }

        private void CreateMesh32(List<Vector3> verts, List<Vector3> normals, List<int> indices, List<Vector2> uvs, Vector3 position)
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(indices, 0);
            mesh.SetUVs(0, uvs);

            if (normals.Count > 0)
                mesh.SetNormals(normals);
            else
                mesh.RecalculateNormals();

            mesh.RecalculateBounds();

            GameObject go = new GameObject("Mesh");
            go.transform.parent = transform;
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshCollider>();
            go.GetComponent<Renderer>().material = material;
            go.GetComponent<MeshFilter>().mesh = mesh;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
            go.transform.localPosition = position;

            meshes.Add(go);
        }

        private void SpawnObjectsOnSurface(List<Vector3> verts, List<Vector3> normals, Vector3 meshPosition)
        {
            System.Random random = new System.Random();

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 vert = transform.TransformPoint(verts[i] + meshPosition);
                Vector3 normal = normals[i];

                // Spawn mushrooms
                if (Vector3.Angle(Vector3.up, normal) <= 30f && normal.y > 0)
                {
                    if (random.NextDouble() < mushroomSpawnChance)
                    {
                        bool canSpawn = true;

                        foreach (var spawnedPosition in mushroomPositions)
                        {
                            if (Vector3.Distance(spawnedPosition, vert) < 5f)
                            {
                                canSpawn = false;
                                break;
                            }
                        }

                        if (canSpawn)
                        {
                            GameObject selectedPrefab = mushroomPrefabs[random.Next(mushroomPrefabs.Count)];
                            GameObject instance = Instantiate(selectedPrefab, vert, Quaternion.identity);
                            instance.transform.up = normal;
                            mushroomPositions.Add(vert);
                        }
                    }
                }

                // Spawn plants
                if (Vector3.Angle(Vector3.up, normal) <= 70f && normal.y > 0)
                {
                    if (random.NextDouble() < plantSpawnChance)
                    {
                        bool canSpawn = true;

                        foreach (var spawnedPosition in mushroomPositions)
                        {
                            if (Vector3.Distance(spawnedPosition, vert) < 4f)
                            {
                                canSpawn = false;
                                break;
                            }
                        }

                        foreach (var spawnedPosition in plantPositions)
                        {
                            if (Vector3.Distance(spawnedPosition, vert) < 2f)
                            {
                                canSpawn = false;
                                break;
                            }
                        }

                        if (canSpawn)
                        {
                            GameObject selectedPrefab = plantPrefabs[random.Next(plantPrefabs.Count)];
                            GameObject instance = Instantiate(selectedPrefab, vert, Quaternion.identity);
                            instance.transform.up = normal;
                            plantPositions.Add(vert);
                        }
                    }
                }

                // Spawn crystals
                if (Vector3.Angle(Vector3.up, normal) >= 60f)
                {
                    if (random.NextDouble() < crystalSpawnChance)
                    {
                        bool canSpawn = true;

                        foreach (var spawnedPosition in mushroomPositions)
                        {
                            if (Vector3.Distance(spawnedPosition, vert) < 2f)
                            {
                                canSpawn = false;
                                break;
                            }
                        }

                        foreach (var spawnedPosition in plantPositions)
                        {
                            if (Vector3.Distance(spawnedPosition, vert) < 2f)
                            {
                                canSpawn = false;
                                break;
                            }
                        }

                        foreach (var spawnedPosition in crystalPositions)
                        {
                            if (Vector3.Distance(spawnedPosition, vert) < 10f)
                            {
                                canSpawn = false;
                                break;
                            }
                        }

                        if (canSpawn)
                        {
                            GameObject selectedPrefab = crystalPrefabs[random.Next(crystalPrefabs.Count)];
                            GameObject instance = Instantiate(selectedPrefab, vert, Quaternion.identity);
                            instance.transform.up = normal;
                            crystalPositions.Add(vert);
                        }
                    }
                }
            }
        }

        private void SpawnFireflies(List<Vector3> verts, List<Vector3> normals, Vector3 meshPosition)
        {
            System.Random random = new System.Random();

            List<Vector3> potentialSpawnPositions = new List<Vector3>();
            List<Vector3> allPositions = new List<Vector3>(mushroomPositions);
            allPositions.AddRange(plantPositions);
            allPositions.AddRange(crystalPositions);

            // Generate potential spawn positions for fireflies
            for (int i = 0; i < allPositions.Count; i++)
            {
                Vector3 vert = allPositions[i];
                Vector3 normal = normals[i];
                Vector3 spawnPosition = vert + normal * 3;
                potentialSpawnPositions.Add(spawnPosition);
            }

            // Attempt to spawn fireflies at potential positions
            for (int i = 0; i < potentialSpawnPositions.Count; i++)
            {
                if (random.NextDouble() < fireflySpawnChance)
                {
                    Vector3 worldPosition = transform.TransformPoint(potentialSpawnPositions[i]);

                    // Check if the position is valid (not inside wall)
                    if (!IsInsideWall(worldPosition, normals[i]))
                    {
                        // Check distance from surface
                        bool isFarFromSurface = true;
                        foreach (var vert in mushroomPositions)
                        {
                            if (Vector3.Distance(worldPosition, vert) < 2f)
                            {
                                isFarFromSurface = false;
                                break;
                            }
                        }

                        foreach (var vert in plantPositions)
                        {
                            if (Vector3.Distance(worldPosition, vert) < 2f)
                            {
                                isFarFromSurface = false;
                                break;
                            }
                        }

                        foreach (var vert in crystalPositions)
                        {
                            if (Vector3.Distance(worldPosition, vert) < 2f)
                            {
                                isFarFromSurface = false;
                                break;
                            }
                        }

                        // Check distance from other fireflies
                        if (isFarFromSurface)
                        {
                            bool isFarFromOtherFireflies = true;
                            foreach (var fireflyPosition in fireflyPositions)
                            {
                                if (Vector3.Distance(fireflyPosition, worldPosition) < 15f)
                                {
                                    isFarFromOtherFireflies = false;
                                    break;
                                }
                            }

                            if (isFarFromOtherFireflies)
                            {
                                GameObject selectedPrefab = fireflyPrefabs[random.Next(fireflyPrefabs.Count)];
                                GameObject instance = Instantiate(selectedPrefab, worldPosition, Quaternion.identity);
                                fireflyPositions.Add(worldPosition);
                            }
                        }
                    }
                }
            }
        }

        private bool IsInsideWall(Vector3 position, Vector3 normal)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(position - normal * 0.1f, normal, out hitInfo, 0.2f))
            {
                return true;
            }
            return false;
        }

        private void OnRenderObject()
        {
            if (normalRenderer != null && meshes.Count > 0 && drawNormals)
            {
                var m = meshes[0].transform.localToWorldMatrix;

                normalRenderer.LocalToWorld = m;
                normalRenderer.Draw();
            }
        }
    }
}
