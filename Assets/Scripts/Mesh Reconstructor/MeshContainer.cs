using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Holds all the MeshVert and MeshFace data
/// Performs operations on the data as a whole, like finding neighbours, edges.
/// </summary>
public class MeshContainer : Singleton<MeshContainer>, IPointerClickHandler
{
    /// <summary>
    /// Time before firing next Iteration.
    /// </summary>
    public float IterationTime;
    /// <summary>
    /// Time each iteration takes to complete.
    /// </summary>
    public float IterationSpeed;

    public MeshFilter meshFilter;
    public List<Mesh> meshes;
    MeshCollider meshCollider;

    public List<MeshVert> MeshVerts = new List<MeshVert>();
    public List<MeshTri> MeshFaces = new List<MeshTri>();
    //List<MeshQuad> MeshQuads;
    //List<MeshEdge> MeshEdges;

    public List<Vector3> Vertices = new List<Vector3>();
    public List<int> Triangles = new List<int>();

    public MeshIterator iterator;

    public bool Dirty;

    public void Start()
    {
        iterator = new MeshIterator(this);//, IterationTime, IterationSpeed
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.mesh;

        //if (clearOnStart)
        meshFilter.mesh.Clear();

        Init(2);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
            Camera.main.GetComponent<MouseOrbitZoom>().SetDesiredDistance(meshFilter.mesh.bounds.size.z + 1);
        else
        {
            var hit = new RaycastHit();
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                return;

            //MeshCollider meshCollider = hit.collider as MeshCollider;
            //if (meshCollider == null || meshCollider.sharedMesh == null)
            //    return;

            if (eventData.button == PointerEventData.InputButton.Left)
                Restart(hit.triangleIndex);
        }

    }

    public void Init(int meshIndex)
    {
        // Stop Previous Animation
        StopAllCoroutines();

        // Clear Data
        meshFilter.mesh.Clear();
        //initialTriIndex = -1;
        MeshVerts.Clear();
        MeshFaces.Clear();
        Vertices.Clear();
        Triangles.Clear();

        CopyLayerManager.ResetLayers();

        ConvertMesh(meshes[meshIndex]);

        //if (clearOnStart)
        //meshFilter.mesh.Clear();
    }

    public void Restart(int triangleIndex = -1)
    {
        Debug.Log("Restarting");
        StopAllCoroutines();

        if (Dirty)
            CleanUp();

        meshFilter.mesh.Clear();

        iterator.CreateIteration(triangleIndex == -1 ? MeshFaces.SelectRandom() : MeshFaces[triangleIndex]);

        iterator.StartIterating();
        Dirty = true;
    }

    public void CleanUp()
    {
        //Debug.Log("Container Cleanup");
        //foreach (var v in MeshVerts)
        //    if (!v.IsFinished)
        //        Debug.Log("Vert Not Finished");

        Vertices.Clear();
        Triangles.Clear();

        MeshFaces.ForEach(a => a.Reset());
        MeshVerts.ForEach(a => a.Reset());

        //CopyLayerManager.ResetAllIndexes();

        Dirty = false;
    }

    public void UpdateVertices()
    {
        meshFilter.mesh.vertices = Vertices.ToArray();
        meshFilter.mesh.RecalculateNormals();
    }

    public void UpdateMesh()
    {
        meshFilter.mesh.Clear();
        meshFilter.mesh.vertices = Vertices.ToArray();
        meshFilter.mesh.triangles = Triangles.ToArray();
        meshFilter.mesh.RecalculateNormals();
    }

    void ConvertMesh(Mesh mesh)
    {
        meshFilter.mesh = mesh;
        var meshVertices = mesh.vertices;
        var meshTriangles = mesh.triangles;

        // Create Verts
        foreach (var v in meshVertices)
        {
            var newVert = new MeshVert(v);
            //newVert.index = verts.Count;
            var index = MeshVerts.IndexOf(newVert);

            if (index == -1)
                MeshVerts.Add(newVert);
            else
                MeshVerts.Add(MeshVerts[index]);
        }

        // Create Tris and Find Neighbours
        for (var i = 0; i < meshTriangles.Length; i += 3)
            MeshFaces.Add(new MeshTri(MeshVerts[meshTriangles[i]], MeshVerts[meshTriangles[i + 1]], MeshVerts[meshTriangles[i + 2]]));

        foreach (var face in MeshFaces)
            face.GetNeighbours();

        // Clean Up
        MeshVerts = MeshVerts.Distinct().ToList();
        //meshFilter.mesh.MarkDynamic();
        transform.position = new Vector3(0, mesh.bounds.extents.y, 0);
        //sizeOffset = 1 + (meshTriangles.Length / 3 / 800);
        Camera.main.GetComponent<MouseOrbitZoom>().SetDesiredDistance(mesh.bounds.size.z + mesh.bounds.extents.y);
        meshCollider.sharedMesh = mesh;
    }
}
