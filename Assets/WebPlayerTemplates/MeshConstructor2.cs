using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class MeshConstructor : Singleton<MeshConstructor>, IPointerClickHandler
{
    public List<Mesh> meshes;
    public bool clearOnStart;
    public float foldSpeed;
    public Slider slider;
    public Text speedText;

    [HideInInspector]
    public MeshFilter meshFilter;
    MeshCollider collider;

    [HideInInspector]
    public List<Vert> verts = new List<Vert>();
    [HideInInspector]
    public List<Tri> tris = new List<Tri>();

    [HideInInspector]
    public List<Vector3> vertexData = new List<Vector3>();
    [HideInInspector]
    public List<int> triangleData = new List<int>();

    List<List<Tri>> foldingStages = new List<List<Tri>>();
    int stageIndex;
    int initialTriIndex = -1;
    bool animationFinished;
    int maximumStageTris;
    float sizeOffset;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        collider = GetComponent<MeshCollider>();
        collider.sharedMesh = meshFilter.mesh;

        //if (clearOnStart)
        meshFilter.mesh.Clear();

        foldSpeed = slider.value;
        speedText.text = foldSpeed.ToString();

        Init(2);
    }

    public void SetSpeed(float value)
    {
        foldSpeed = value;
        speedText.text = foldSpeed.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
            Camera.main.GetComponent<MouseOrbitZoom>().SetDesiredDistance(meshFilter.mesh.bounds.size.z + 1);
        else
        {
            StopAllCoroutines();
            var hit = new RaycastHit();
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                return;

            MeshCollider meshCollider = hit.collider as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null)
                return;

            //var sharedMesh = meshCollider.sharedMesh;
            //var vertices = sharedMesh.vertices;
            //var triangles = sharedMesh.triangles;
            //Debug.Log("Hit tri B index is " + hit.triangleIndex);

            //var p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];// -Vector3.up;
            //var p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];// -Vector3.up;
            //var p2 = vertices[triangles[hit.triangleIndex * 3 + 2]];// -Vector3.up;
            //var hitTransform = hit.collider.transform;
            //p0 = hitTransform.TransformPoint(p0);
            //p1 = hitTransform.TransformPoint(p1);
            //p2 = hitTransform.TransformPoint(p2);
            //Debug.DrawLine(p0, p1, Color.white, 3, false);
            //Debug.DrawLine(p1, p2, Color.white, 3, false);
            //Debug.DrawLine(p2, p0, Color.white, 3, false);

            if (eventData.button == PointerEventData.InputButton.Left)
                Restart(hit.triangleIndex);

            //if (eventData.button == PointerEventData.InputButton.Right)
            //{
            //    var tri = tris.First(t => t.HasVerts(p0, p1, p2));
            //    //Debug.DrawLine(tri.triCenter, tri.floatingVert.targetPosition, Color.green, 3, false);

            //    foreach (var n in tri.neighbours)
            //    {
            //        if (n == tri.sourceTri)
            //            Debug.DrawLine(tri.triCenter, n.triCenter, Color.yellow, 3, false);
            //        else
            //            Debug.DrawLine(tri.triCenter, n.triCenter, Color.red, 3, false);
            //    }
            //    //Debug.Log("Tri.neighbours.Count: " + tri.neighbours.Count);
            //    //Debug.Log("Tri.stageIndex: " + tri.stageIndex);
            //    //Debug.Log("Tri.floatingVert.floatingCount : " + tri.floatingVert.floatingCount);
            //}
        }
    }

    public void Init(int meshIndex)
    {
        // Stop Previous Animation
        StopAllCoroutines();

        // Clear Data
        meshFilter.mesh.Clear();
        initialTriIndex = -1;

        ConvertMesh(meshes[meshIndex]);

        if (clearOnStart)
            meshFilter.mesh.Clear();
    }

    public void Restart(int initialIndex)
    {
        meshFilter.mesh.Clear();

        if (!animationFinished)
        {
            StopAllCoroutines();
            FinalizeAnimation();
        }

        if (initialTriIndex != initialIndex)
        {
            initialTriIndex = initialIndex;
            CalculateStages(initialTriIndex, true);
        }

        animationFinished = false;
        StartCoroutine(AnimateStage());
    }

    public void FinalizeAnimation()
    {
        meshFilter.mesh.Clear();
        meshFilter.mesh.vertices = vertexData.ToArray();
        meshFilter.mesh.triangles = triangleData.ToArray();
        meshFilter.mesh.RecalculateNormals();
        //meshFilter.mesh.Optimize();

        tris.ForEach(t => t.Reset());
        verts.ForEach(v => v.Reset());
        verts.RemoveAll(v => !v.originalVert);

        vertexData.Clear();
        triangleData.Clear();

        maximumStageTris = 0;
        stageIndex = 0;
        animationFinished = true;
    }

    void ConvertMesh(Mesh mesh)
    {
        verts.Clear();
        tris.Clear();

        meshFilter.mesh = mesh;
        var meshVertices = mesh.vertices;
        var meshTriangles = mesh.triangles;

        // Create Verts
        foreach (var v in meshVertices)
        {
            var newVert = new Vert(v);
            newVert.index = verts.Count;
            var index = verts.IndexOf(newVert);

            if (index == -1)
                verts.Add(newVert);
            else
                verts.Add(verts[index]);
        }

        // Create Tris and Find Neighbours
        for (var i = 0; i < meshTriangles.Length; i += 3)
            tris.Add(new Tri(verts[meshTriangles[i]], verts[meshTriangles[i + 1]], verts[meshTriangles[i + 2]]));

        foreach (var tri in tris)
            tri.CheckForNeighbours();

        // Clean Up
        verts = verts.Distinct().ToList();
        //meshFilter.mesh.MarkDynamic();
        transform.position = new Vector3(0, mesh.bounds.extents.y, 0);
        sizeOffset = 1 + (meshTriangles.Length / 3 / 800);
        Camera.main.GetComponent<MouseOrbitZoom>().SetDesiredDistance(mesh.bounds.size.z + mesh.bounds.extents.y);
        collider.sharedMesh = mesh;
    }

    void CalculateStages(int initialIndex = 0, bool keepVertexData = false)
    {
        if (!keepVertexData)
            vertexData = new List<Vector3>();
        triangleData = new List<int>();
        foldingStages = new List<List<Tri>>();

        var index = 0;
        var stage = new List<Tri>();

        //// Set Initial Stage
        stage.Add(tris[initialIndex]);
        stage[0].stageIndex = 0;
        foldingStages.Add(stage);

        // Add New Stages
        while (true)
        {
            var nextStage = new List<Tri>();
            var count = foldingStages[index].Count;
            for (var i = 0; i < count; i++)
            {
                var tri = foldingStages[index][i];
                foreach (var neighbour in tri.neighbours.Where(x => x.stageIndex == -1))
                {
                    neighbour.stageIndex = foldingStages.Count;
                    neighbour.SetFloatingVerts(tri);
                    if (neighbour.floatingVerts.Count == 2)
                    {
                        neighbour.stageIndex--;
                        foldingStages[index].Insert(i, neighbour);
                        count++;
                        i++;
                    }
                    else
                        nextStage.Add(neighbour);
                }
            }
            if (nextStage.Count > maximumStageTris)
                maximumStageTris = nextStage.Count;
            if (nextStage.Count == 0)
                break;
            foldingStages.Add(nextStage);
            index++;
        }
    }

    IEnumerator AnimateStage()
    {
        //print("Coroutine statred");
        // Add New Mesh Data
        foreach (var t in foldingStages[stageIndex])
        {
            Debug.DrawLine(t.triCenter, t.triCenter - t.Normal, Color.green, 3, false);
            //if (t.floatingVerts.Count > 0)
            //{
            //    Debug.DrawLine(t.triCenter, t.triCenter + Vector3.up, Color.green, 3, false);
            //    Debug.DrawLine(t.triCenter + Vector3.up, t.floatingVerts[0].targetPosition, Color.green, 3, false);
            //}
            t.AddTriToMesh();
        }
        // Update Mesh
        meshFilter.mesh.Clear();
        meshFilter.mesh.vertices = vertexData.ToArray();
        meshFilter.mesh.triangles = triangleData.ToArray();
        meshFilter.mesh.RecalculateNormals();

        // Animate FloatingVert
        if (stageIndex == 0)
        {
            //print("stageindex == 0");
            foreach (var v in foldingStages[0][0].vertices)
            {
                vertexData[v.index] = v.position = v.targetPosition;
            }
            meshFilter.mesh.vertices = vertexData.ToArray();
            meshFilter.mesh.RecalculateNormals();
        }
        else
        {
            // Set Length Lerp
            var lengthLerp = 1.0f;
            if (stageIndex != 0 && foldingStages.Count != 0)
            {
                var ratio = ((float)stageIndex / foldingStages.Count);
                var crossover = 0.3f;
                if (stageIndex < foldingStages.Count * crossover)
                {
                    ratio = (ratio) * (1 / crossover);
                    lengthLerp = EasingCurves.easeInQuint(1.0f, 2.0f, ratio);
                }
                else
                {
                    ratio = (ratio - crossover) * (1 / (1 - crossover));
                    lengthLerp = EasingCurves.easeOutQuint(2.0f, 1.25f, ratio);
                }
            }

            // Set Density Lerp
            var densityLerp = 1.0f;
            if (foldingStages[stageIndex].Count != 0 && maximumStageTris != 0)
            {
                var ratio = ((float)foldingStages[stageIndex].Count / maximumStageTris);
                densityLerp = EasingCurves.easeInOutQuint(0.15f, 1.0f, ratio);
            }

            var foldSpeedLerp = foldSpeed * sizeOffset * lengthLerp * (1 + densityLerp * .2f);

            var timer = 0.0f;
            while (timer < 1)
            {
                var tick = Time.deltaTime * foldSpeedLerp;
                timer = 1 - timer <= tick * 2 ? 1 : timer + tick;

                foreach (var t in foldingStages[stageIndex])
                {

                    //// Stocastic anchoredVerts Animations
                    //if (t.sourceTri != null)
                    //{
                    //    vertexData[t.anchoredVerts[0].index] = t.anchoredVerts[0].position += ((t.sourceTri.triCenter - t.anchoredVerts[0].position)) * .1f * densityLerp; //
                    //    vertexData[t.anchoredVerts[1].index] = t.anchoredVerts[1].position += ((t.sourceTri.triCenter - t.anchoredVerts[1].position)) * .1f * densityLerp; //(t.anchoredVerts[0].position)
                    //}

                    //// 
                    //if (stageIndex < foldingStages.Count - 1)
                    //{
                    //    foreach (var n in t.neighbours.Where(nt => foldingStages[stageIndex + 1].Contains(nt)))
                    //    {
                    //        //Debug.Log("yep " + n.stageIndex);
                    //        var v1 = n.sourceTri.floatingVerts[0];
                    //        if (vertexData.Count < v1.index) continue;
                    //        vertexData[v1.index] = v1.position += (t.triCenter - v1.position) * 1.0f * densityLerp;

                    //    }
                    //}

                    // Animate floatingVert
                    foreach (var floatingVert in t.floatingVerts)
                    {
                        var offset = (t.sourceTri.triCenter - floatingVert.initialPosition) * densityLerp; // (t.triCenter - floatingVert.initialPosition) //(((floatingVert.targetPosition + floatingVert.initialPosition) * 0.5f) - t.triCenter)
                        vertexData[floatingVert.index] = floatingVert.position = Vector3.Lerp(floatingVert.initialPosition,
                                                                                                floatingVert.targetPosition,
                                                                                                EasingCurves.easeOutCubic(0, 1, timer));
                    }

                }

                meshFilter.mesh.vertices = vertexData.ToArray();
                meshFilter.mesh.RecalculateNormals();
                yield return new WaitForEndOfFrame();
            }

            // Reuse Shared Verts
            foreach (var t in foldingStages[stageIndex])
            {
                var triIndex = t.triIndex;
                t.RemoveUniqueVerts();
                foreach (var v in t.vertices)
                {
                    vertexData[v.index] = v.position = v.initialPosition = v.targetPosition;
                    triangleData[triIndex] = v.index;
                    triIndex++;
                }
            }
        }

        // Increment Stage
        stageIndex++;
        if (stageIndex < foldingStages.Count)
            StartCoroutine(AnimateStage());
        else
            FinalizeAnimation();
    }

    public class Vert : IEquatable<Vert>
    {
        public bool isInMesh;

        public int index = -1;
        public Vector3 position;
        public List<Vert> unsharedVerts;
        public List<Tri> tris { get; private set; }

        public Vector3 initialPosition;
        public Vector3 targetPosition;

        public int useCount;
        //public bool alreadyFloated;
        public bool originalVert;

        public Vert(Vector3 _position, bool _originalVert = true)
        {
            position = targetPosition = initialPosition = _position;
            tris = new List<Tri>();
            unsharedVerts = new List<Vert>();
            originalVert = _originalVert;
            if (originalVert)
                unsharedVerts.Add(this);
        }

        public void Reset()
        {
            isInMesh = false;
            //alreadyFloated = false;
            position = initialPosition = targetPosition;
            useCount = 0;

            foreach (var usv in unsharedVerts)
            {
                if (usv == this) continue;
                tris.AddRange(usv.tris);
            }
            unsharedVerts.Clear();
            unsharedVerts.Add(this);
        }

        public void AddTri(Tri tri)
        {
            tris.Add(tri);
        }

        public void UnshareVert(Tri tri, Vert newVert)
        {
            tris.Remove(tri);
            newVert.tris.Add(tri);

            foreach (var v in unsharedVerts)
            {
                if (!v.unsharedVerts.Contains(newVert))
                    v.unsharedVerts.Add(newVert);
            }
            newVert.unsharedVerts.AddRange(unsharedVerts);
        }

        public void ReshareVert(Vert newVert)
        {
            tris.Add(newVert.tris[0]);
            unsharedVerts.Remove(newVert);
            foreach (var v in unsharedVerts)
            {
                if (v.unsharedVerts.Contains(newVert))
                    v.unsharedVerts.Remove(newVert);
            }
        }

        public void AddUse(bool recursive = true)
        {
            useCount++;
            if (recursive)
            {
                foreach (var unv in unsharedVerts)
                {
                    if (unv == this) continue;
                    unv.AddUse(false);
                }
            }
        }

        public bool Equals(Vert other)
        {
            return targetPosition == other.targetPosition;
        }

        public static bool operator ==(Vert a, Vector3 b)
        {
            return a.targetPosition == b;
        }

        public static bool operator !=(Vert a, Vector3 b)
        {
            return a.targetPosition != b;
        }

        public static Vector3 operator +(Vert a, Vert b)
        {
            return a.targetPosition + b.targetPosition;
        }

        public static Vector3 operator -(Vert a, Vert b)
        {
            return a.targetPosition - b.targetPosition;
        }
    }



    public interface SurfaceShape
    {
        void CheckForNeighbours();
    }

    public class Tri : SurfaceShape, IEquatable<Tri>
    {
        public bool isInMesh;

        public List<Vert> vertices { get; private set; }
        public List<Tri> neighbours { get; private set; }

        public List<Vert> anchoredVerts;
        //public List<Tri> potentialSources;
        public List<Vert> floatingVerts;
        public Tri sourceTri;
        public Vector3 triCenter;

        public Vector3 Normal
        {
            get
            {
                var dir = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);
                var norm = Vector3.Normalize(dir);
                return norm;
            }
        }

        public Vector3 Center
        {
            get
            {
                return (vertices[0].targetPosition + vertices[1].targetPosition + vertices[2].targetPosition) / 3;
            }
        }

        public int stageIndex;
        public int triIndex;
        public bool hasUniqueVerts;

        public Tri(Vert a, Vert b, Vert c)
        {
            neighbours = new List<Tri>(3);
            vertices = new List<Vert>(3);
            anchoredVerts = new List<Vert>(2);
            floatingVerts = new List<Vert>(2);
            //potentialSources = new List<Tri>();
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            a.AddTri(this);
            b.AddTri(this);
            c.AddTri(this);

            triIndex = MeshConstructor.Instance.tris.Count;
            stageIndex = -1;
            triCenter = (a.targetPosition + b.targetPosition + c.targetPosition) / 3;
        }

        public void Reset()
        {
            isInMesh = false;
            stageIndex = -1;
            floatingVerts.Clear();
            sourceTri = null;
            anchoredVerts.Clear();
            //potentialSources.Clear();

            if (!hasUniqueVerts) return;
            for (var i = 0; i < 3; i++)
            {
                vertices[i] = vertices[i].unsharedVerts[0];
            }
            hasUniqueVerts = false;
        }

        public bool Equals(Tri other)
        {
            var matches = 0;
            foreach (var v1 in vertices)
            {
                foreach (var v2 in other.vertices)
                {
                    if (v1 == v2)
                    {
                        matches++;
                        break;
                    }
                }
            }
            return matches >= 3;
        }

        public bool HasVerts(Vector3 a, Vector3 b, Vector3 c)
        {
            return vertices[0] == a && vertices[1] == b && vertices[2] == c;
        }

        public void CheckForNeighbours()
        {
            if (neighbours.Count == 3)
                return;

            //var triComparer = new TriIndexEqualityComparer();
            //var vertComparer = new VertIndexEqualityComparer();

            // Gather potential Tris
            var potentialTris = new List<Tri>();
            foreach (var v in vertices)
                potentialTris.AddRange(v.tris);
            potentialTris = potentialTris.Distinct().ToList(); //triComparer
            potentialTris.Remove(this);

            // Add Tris that share 2 Verts
            foreach (var t in potentialTris)
            {
                if (neighbours.Contains(t) || vertices.Except(t.vertices).Count() != 1) continue; //vertComparer

                neighbours.Add(t);
                t.neighbours.Add(this);

                if (neighbours.Count == 3)
                    break;
            }
        }

        public void CreateUniqueVerts()
        {
            var verts = MeshConstructor.Instance.verts;
            for (var i = 0; i < vertices.Count; i++)
            {
                var newVert = new Vert(vertices[i].targetPosition, false);
                verts.Add(newVert);

                vertices[i].UnshareVert(this, newVert);
                vertices[i] = newVert;
                hasUniqueVerts = true;
            }
        }

        public void RemoveUniqueVerts()
        {
            if (!hasUniqueVerts) return;
            for (var i = 0; i < vertices.Count; i++)
            {
                var uniqueVert = vertices[i];
                //vertexData.RemoveAt(rangeIndex);
                vertices[i] = uniqueVert.unsharedVerts[0];
                vertices[i].ReshareVert(uniqueVert);

            }
            hasUniqueVerts = false;
        }

        public void AddTriToMesh()
        {
            if (isInMesh)
                return;

            var vertexData = MeshConstructor.Instance.vertexData;
            var triangleData = MeshConstructor.Instance.triangleData;
            triIndex = triangleData.Count;
            // Loop through each Vert
            foreach (var v in vertices)
            {
                // Add any Verts not already in the Mesh 
                if (!v.isInMesh)
                {
                    v.index = vertexData.Count;
                    vertexData.Add(v.position);
                    v.isInMesh = true;
                    //floatingVert = v;
                }
                triangleData.Add(v.index);
            }

            isInMesh = true;
        }

        public void SetFloatingVerts(Tri source)
        {
            sourceTri = source;

            if (neighbours.Where(n => n.stageIndex != -1).Count() >= 2)
                CreateUniqueVerts();

            foreach (var v in vertices)
            {
                if (sourceTri.vertices.Contains(v))
                    anchoredVerts.Add(v);
            }

            floatingVerts.Add(vertices.Except(anchoredVerts).First());

            var halfway = (anchoredVerts[0].position + anchoredVerts[1].position) * 0.5f;
            var direction = source.stageIndex % 2 == 0 ? 1 : -1;
            floatingVerts[0].initialPosition = halfway + (Normal * halfway.magnitude * 0.33f * direction);

            foreach (var v in vertices)
            {
                v.AddUse();
            }
        }
    }

    class TriIndexEqualityComparer : IEqualityComparer<Tri>
    {

        public bool Equals(Tri a, Tri b)
        {
            var matches = 0;
            foreach (var v1 in a.vertices)
            {
                foreach (var v2 in b.vertices)
                {
                    if (v1.index == v2.index)
                    {
                        matches++;
                        break;
                    }
                }
            }

            return matches >= 3;
        }

        public int GetHashCode(Tri tri)
        {
            int hCode = tri.vertices[0].index ^ tri.vertices[1].index ^ tri.vertices[2].index;
            return hCode.GetHashCode();
        }

    }

    class VertIndexEqualityComparer : IEqualityComparer<Vert>
    {

        public bool Equals(Vert a, Vert b)
        {
            return a.index == b.index;
        }

        public int GetHashCode(Vert tri)
        {
            int hCode = tri.position.GetHashCode() ^ tri.index;
            return hCode.GetHashCode();
        }

    }
}
////float scale;
////Vector3 center;
////bool dirty;

////public void ApplyScale()
////{
////    // if dirty, center = (vertices[0] + vertices[1] + vertices[2])/3;
////    // foreach vertex, vertex.position = center + (center - vertex.position) * scale;
////    // foreach neighbour, neighbour.dirty = true;

////    // v1 = c2 + (c2 - v1) * (s2 + s1 - s2)
////}

////public class Edge
////{
////    List<Vert> vertices;
////    List<Edge> neighbours;
////    Tri tri;
////}
