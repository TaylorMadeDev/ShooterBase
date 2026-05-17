using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralTapeJoiner : MonoBehaviour
{
    [Header("Objects To Join")]
    public Transform objectA;
    public Transform objectB;

    [Header("Tape Settings")]
    public Material tapeMaterial;
    public float tapeWidth = 0.25f;
    public float surfaceOffset = 0.025f;
    [Range(1, 32)]
    public int segmentsPerSide = 8; // Increased for a tighter, smoother fit

    [Header("Debug")]
    public bool regenerateOnStart = true;
    public bool updateInRealTime = true;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private Vector3 lastPosA, lastPosB;
    private Quaternion lastRotA, lastRotB;
    private Vector3 lastScaleA, lastScaleB;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (tapeMaterial != null)
            meshRenderer.sharedMaterial = tapeMaterial;
    }

    private void OnValidate()
    {
        if (updateInRealTime && meshFilter != null && meshRenderer != null)
        {
            // Delay call to ensure we don't get warnings about creating meshes during OnValidate
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && gameObject.activeInHierarchy)
                    GenerateTape();
            };
        }
    }

    private void Start()
    {
        if (regenerateOnStart && Application.isPlaying)
            GenerateTape();
    }

    private void Update()
    {
        if (!updateInRealTime || objectA == null || objectB == null) return;

        if (objectA.position != lastPosA || objectA.rotation != lastRotA || objectA.localScale != lastScaleA ||
            objectB.position != lastPosB || objectB.rotation != lastRotB || objectB.localScale != lastScaleB ||
            transform.hasChanged)
        {
            GenerateTape();
            lastPosA = objectA.position;
            lastPosB = objectB.position;
            lastRotA = objectA.rotation;
            lastRotB = objectB.rotation;
            lastScaleA = objectA.localScale;
            lastScaleB = objectB.localScale;
            transform.hasChanged = false;
        }
    }

    [ContextMenu("Generate Tape")]
    public void GenerateTape()
    {
        if (objectA == null || objectB == null)
        {
            Debug.LogWarning("Assign Object A and Object B.");
            return;
        }

        Bounds boundsA = GetWorldBounds(objectA);
        Bounds boundsB = GetWorldBounds(objectB);

        Vector3 direction = (boundsB.center - boundsA.center).normalized;
        Vector3 joinAxis = GetMainAxis(direction);

        Vector3 widthAxis;
        Vector3 heightAxis;

        if (Mathf.Abs(joinAxis.x) > 0.5f)
        {
            widthAxis = Vector3.forward;
            heightAxis = Vector3.up;
        }
        else if (Mathf.Abs(joinAxis.y) > 0.5f)
        {
            widthAxis = Vector3.right;
            heightAxis = Vector3.forward;
        }
        else
        {
            widthAxis = Vector3.right;
            heightAxis = Vector3.up;
        }

        CreateTapeLoop(boundsA, boundsB, joinAxis, widthAxis, heightAxis);
    }

    private void CreateTapeLoop(
        Bounds boundsA,
        Bounds boundsB,
        Vector3 joinAxis,
        Vector3 widthAxis,
        Vector3 heightAxis)
    {
        segmentsPerSide = Mathf.Max(1, segmentsPerSide);

        int pointsPerLoop = segmentsPerSide * 4;
        
        int numLoops = 4;
        int vertexCount = (pointsPerLoop * numLoops) + (pointsPerLoop * 2) + 2;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        int segments = numLoops - 1;
        int tubeTriangleCount = pointsPerLoop * 6 * segments;
        int capTriangleCount = pointsPerLoop * 3 * 2;
        int[] triangles = new int[tubeTriangleCount + capTriangleCount];

        // Generate base loops centered at zero
        Vector3[] loopA = GenerateLoopPoints(boundsA, widthAxis, heightAxis);
        Vector3[] loopB = GenerateLoopPoints(boundsB, widthAxis, heightAxis);

        float lengthA = Mathf.Abs(Vector3.Dot(boundsA.size, joinAxis));
        float lengthB = Mathf.Abs(Vector3.Dot(boundsB.size, joinAxis));

        float dirSign = Mathf.Sign(Vector3.Dot(boundsB.center - boundsA.center, joinAxis));
        if (dirSign == 0f) dirSign = 1f;

        // Calculate positions of the 4 loops along the join axis
        Vector3 edgeA = boundsA.center + joinAxis * (dirSign * (lengthA * 0.5f));
        Vector3 edgeB = boundsB.center - joinAxis * (dirSign * (lengthB * 0.5f));

        Vector3 startA = edgeA - joinAxis * (dirSign * (lengthA * 0.25f));
        Vector3 endB = edgeB + joinAxis * (dirSign * (lengthB * 0.25f));

        Vector3[][] allLoops = new Vector3[][] { loopA, loopA, loopB, loopB };
        Vector3[] loopCenters = new Vector3[] { startA, edgeA, edgeB, endB };

        // Tube vertices
        for (int l = 0; l < numLoops; l++)
        {
            float uvV = l / (float)(numLoops - 1);
            for (int i = 0; i < pointsPerLoop; i++)
            {
                vertices[l * pointsPerLoop + i] = transform.InverseTransformPoint(allLoops[l][i] + loopCenters[l]);

                float uvU = i / (float)pointsPerLoop;
                uvs[l * pointsPerLoop + i] = new Vector2(uvU, uvV);
            }
        }

        // Cap vertices
        int capOffset = pointsPerLoop * numLoops;
        int endCapCenter1 = capOffset + pointsPerLoop * 2;
        int endCapCenter2 = endCapCenter1 + 1;

        vertices[endCapCenter1] = transform.InverseTransformPoint(startA);
        vertices[endCapCenter2] = transform.InverseTransformPoint(endB);
        uvs[endCapCenter1] = new Vector2(0.5f, 0.5f);
        uvs[endCapCenter2] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < pointsPerLoop; i++)
        {
            vertices[capOffset + i] = vertices[i]; // Cap 1 outline (loop 0)
            vertices[capOffset + pointsPerLoop + i] = vertices[(numLoops - 1) * pointsPerLoop + i]; // Cap 2 outline (loop 3)
            
            uvs[capOffset + i] = new Vector2(0.5f, 0.5f);
            uvs[capOffset + pointsPerLoop + i] = new Vector2(0.5f, 0.5f); 
        }

        int tri = 0;

        // Tube triangles
        for (int l = 0; l < segments; l++)
        {
            int loopAStart = l * pointsPerLoop;
            int loopBStart = (l + 1) * pointsPerLoop;

            for (int i = 0; i < pointsPerLoop; i++)
            {
                int next = (i + 1) % pointsPerLoop;

                int a = loopAStart + i;
                int b = loopBStart + i;
                int c = loopAStart + next;
                int d = loopBStart + next;

                triangles[tri++] = a;
                triangles[tri++] = c;
                triangles[tri++] = b;

                triangles[tri++] = b;
                triangles[tri++] = c;
                triangles[tri++] = d;
            }
        }

        // Cap triangles
        for (int i = 0; i < pointsPerLoop; i++)
        {
            int next = (i + 1) % pointsPerLoop;

            // Cap 1
            if (dirSign < 0) {
                triangles[tri++] = capOffset + i;
                triangles[tri++] = capOffset + next;
                triangles[tri++] = endCapCenter1;
            } else {
                triangles[tri++] = capOffset + next;
                triangles[tri++] = capOffset + i;
                triangles[tri++] = endCapCenter1;
            }

            // Cap 2
            if (dirSign < 0) {
                triangles[tri++] = capOffset + pointsPerLoop + next;
                triangles[tri++] = capOffset + pointsPerLoop + i;
                triangles[tri++] = endCapCenter2;
            } else {
                triangles[tri++] = capOffset + pointsPerLoop + i;
                triangles[tri++] = capOffset + pointsPerLoop + next;
                triangles[tri++] = endCapCenter2;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Procedural Tape Mesh";

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }

    private Vector3[] GenerateLoopPoints(Bounds bounds, Vector3 widthAxis, Vector3 heightAxis)
    {
        int pointsPerLoop = segmentsPerSide * 4;
        Vector3[] loopPoints = new Vector3[pointsPerLoop];

        float widthLength = Mathf.Abs(Vector3.Dot(bounds.size, widthAxis)) + surfaceOffset * 2f;
        float heightLength = Mathf.Abs(Vector3.Dot(bounds.size, heightAxis)) + surfaceOffset * 2f;

        int index = 0;

        for (int side = 0; side < 4; side++)
        {
            for (int i = 0; i < segmentsPerSide; i++)
            {
                float t = i / (float)segmentsPerSide;

                Vector3 point = Vector3.zero;

                if (side == 0)
                {
                    point += widthAxis * Mathf.Lerp(-widthLength / 2f, widthLength / 2f, t);
                    point += heightAxis * (heightLength / 2f);
                }
                else if (side == 1)
                {
                    point += widthAxis * (widthLength / 2f);
                    point += heightAxis * Mathf.Lerp(heightLength / 2f, -heightLength / 2f, t);
                }
                else if (side == 2)
                {
                    point += widthAxis * Mathf.Lerp(widthLength / 2f, -widthLength / 2f, t);
                    point += heightAxis * (-heightLength / 2f);
                }
                else
                {
                    point += widthAxis * (-widthLength / 2f);
                    point += heightAxis * Mathf.Lerp(-heightLength / 2f, heightLength / 2f, t);
                }

                Vector3 outward = point;
                if (outward.sqrMagnitude > 0.001f)
                {
                    point += outward.normalized * surfaceOffset;
                }

                loopPoints[index] = point;
                index++;
            }
        }

        return loopPoints;
    }

    private Bounds GetWorldBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return new Bounds(target.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private Vector3 GetMainAxis(Vector3 direction)
    {
        direction = direction.normalized;

        float x = Mathf.Abs(Vector3.Dot(direction, Vector3.right));
        float y = Mathf.Abs(Vector3.Dot(direction, Vector3.up));
        float z = Mathf.Abs(Vector3.Dot(direction, Vector3.forward));

        if (x > y && x > z)
            return Vector3.right * Mathf.Sign(direction.x);

        if (y > x && y > z)
            return Vector3.up * Mathf.Sign(direction.y);

        return Vector3.forward * Mathf.Sign(direction.z);
    }
}