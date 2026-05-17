using UnityEngine;

namespace Scrapout.Weapons
{
    public static class JointTapeGenerator
    {
        public static void GenerateTape(Transform socket, GameObject attachedPart, Material tapeMaterial, float bandWidth = 0.08f)
        {
            if (socket == null || attachedPart == null || tapeMaterial == null) return;

            GameObject tapeObj = new GameObject("DuctTape_" + socket.name);
            tapeObj.transform.SetParent(socket, false);
            tapeObj.transform.localPosition = Vector3.zero;
            tapeObj.transform.localRotation = Quaternion.identity;

            MeshFilter mf = tapeObj.AddComponent<MeshFilter>();
            MeshRenderer mr = tapeObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = tapeMaterial;

            // Determine bounds of the attached part to make the tape wrap around it nicely
            Vector3 extents = new Vector3(0.04f, 0.06f, 0.04f); // Fallback defaults

            MeshRenderer[] renderers = attachedPart.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length > 0)
            {
                Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
                bool hasBounds = false;

                for (int i = 0; i < renderers.Length; i++)
                {
                    Bounds wBounds = renderers[i].bounds;
                    Vector3 centerLocal = socket.InverseTransformPoint(wBounds.center);
                    
                    // Approximate local size from world bounds
                    Vector3 sizeLocal = socket.InverseTransformVector(wBounds.size);
                    sizeLocal = new Vector3(Mathf.Abs(sizeLocal.x), Mathf.Abs(sizeLocal.y), Mathf.Abs(sizeLocal.z));

                    Bounds b = new Bounds(centerLocal, sizeLocal);
                    if (!hasBounds)
                    {
                        localBounds = b;
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(b);
                    }
                }

                if (hasBounds)
                {
                    Vector3 size = localBounds.size;
                    
                    // Move the tape object to the center of the bounds
                    tapeObj.transform.localPosition = localBounds.center;
                    
                    // Auto-rotate the tape so its tube hole (Z-axis) points along the longest axis of the part.
                    // This creates a tight band perfectly wrapping around the object's girth.
                    if (size.x >= size.y && size.x >= size.z)
                    {
                        tapeObj.transform.localRotation = Quaternion.Euler(0, 90, 0);
                        extents = new Vector3(localBounds.extents.z, localBounds.extents.y, bandWidth / 2f);
                    }
                    else if (size.y >= size.x && size.y >= size.z)
                    {
                        tapeObj.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                        extents = new Vector3(localBounds.extents.x, localBounds.extents.z, bandWidth / 2f);
                    }
                    else
                    {
                        tapeObj.transform.localRotation = Quaternion.identity;
                        extents = new Vector3(localBounds.extents.x, localBounds.extents.y, bandWidth / 2f);
                    }
                    
                    // Add tiny offset so the tape sits roughly outside the mesh
                    extents += new Vector3(0.005f, 0.005f, 0f);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "GeneratedTapeBoxMesh";

            // Generate a noisy box sleeve (top, bottom, left, right faces - NO front/back caps)
            int segmentsX = 4;
            int segmentsY = 4;
            int rings = 5; // Along Z axis
            
            int totalRingVertices = (segmentsX * 2 + segmentsY * 2);
            Vector3[] vertices = new Vector3[totalRingVertices * rings];
            Vector2[] uvs = new Vector2[totalRingVertices * rings];
            int[] triangles = new int[totalRingVertices * (rings - 1) * 6];

            for (int r = 0; r < rings; r++)
            {
                float zPercent = (float)r / (rings - 1);
                float z = Mathf.Lerp(-extents.z, extents.z, zPercent);

                int vIndex = r * totalRingVertices;
                
                // Walk around the perimeter of the box
                // We'll create vertices along the Top, Right, Bottom, Left edges
                
                // Edge 1: Top (Left to Right)
                for(int i = 0; i < segmentsX; i++) {
                    float t = (float)i / segmentsX;
                    float x = Mathf.Lerp(-extents.x, extents.x, t);
                    vertices[vIndex] = GetNoisyPos(x, extents.y, z, r, rings);
                    uvs[vIndex] = new Vector2(t * 0.25f, zPercent);
                    vIndex++;
                }
                // Edge 2: Right (Top to Bottom)
                for(int i = 0; i < segmentsY; i++) {
                    float t = (float)i / segmentsY;
                    float y = Mathf.Lerp(extents.y, -extents.y, t);
                    vertices[vIndex] = GetNoisyPos(extents.x, y, z, r, rings);
                    uvs[vIndex] = new Vector2(0.25f + t * 0.25f, zPercent);
                    vIndex++;
                }
                // Edge 3: Bottom (Right to Left)
                for(int i = 0; i < segmentsX; i++) {
                    float t = (float)i / segmentsX;
                    float x = Mathf.Lerp(extents.x, -extents.x, t);
                    vertices[vIndex] = GetNoisyPos(x, -extents.y, z, r, rings);
                    uvs[vIndex] = new Vector2(0.5f + t * 0.25f, zPercent);
                    vIndex++;
                }
                // Edge 4: Left (Bottom to Top)
                for(int i = 0; i < segmentsY; i++) {
                    float t = (float)i / segmentsY;
                    float y = Mathf.Lerp(-extents.y, extents.y, t);
                    vertices[vIndex] = GetNoisyPos(-extents.x, y, z, r, rings);
                    uvs[vIndex] = new Vector2(0.75f + t * 0.25f, zPercent);
                    vIndex++;
                }
            }

            int tTri = 0;
            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < totalRingVertices; s++)
                {
                    int current = r * totalRingVertices + s;
                    int next = current + 1;
                    if (s == totalRingVertices - 1) next = r * totalRingVertices; // Wrap around

                    int topCurrent = current + totalRingVertices;
                    int topNext = next + totalRingVertices;

                    triangles[tTri++] = current;
                    triangles[tTri++] = topCurrent;
                    triangles[tTri++] = next;

                    triangles[tTri++] = next;
                    triangles[tTri++] = topCurrent;
                    triangles[tTri++] = topNext;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.mesh = mesh;
        }

        private static Vector3 GetNoisyPos(float x, float y, float z, int ringIndex, int totalRings)
        {
            float noiseX = Random.Range(-0.002f, 0.002f);
            float noiseY = Random.Range(-0.002f, 0.002f);
            float noiseZ = Random.Range(-0.002f, 0.002f);

            // Tear the edges 
            if (ringIndex == 0 || ringIndex == totalRings - 1)
            {
                noiseZ += Random.Range(-0.012f, 0.012f);
                
                // Pull edges slightly inward to look tighter
                x *= 0.95f;
                y *= 0.95f;
            }

            return new Vector3(x + noiseX, y + noiseY, z + noiseZ);
        }
    }
}
