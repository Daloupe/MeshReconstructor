using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks its Original and Current Positions
/// Provides a lock to ensure only 1 controller.
/// Tracks How many Faces it's attached to
/// Provides Face Selections
/// Provides Math functions for Position.
/// Multiple Instances of the vert are tracked, each Face records which index in particular it wants.
/// Tris dictionary holds each Tri as its key, and which copyLayer it's apart of as it's value.
/// </summary>
public class MeshVert : IFaces, ICopyable, IMoveable, IMeshElement, IEquatable<MeshVert>
{
    public Vector3 Position { get; set; }
    public Vector3 DefaultPosition { get; set; }
    public bool IsInMesh { get; set; }
    public int MeshIndex { get; set; }
    public bool IsFinished;
    //public int CopyLayerCount { get; set; }

    public Dictionary<MeshFace, CopyLayerInstance> Faces { get; set; }
    CopyLayerInstance DefaultCopyLayerInstance;
    //public List<MeshFace> Faces { get; set; }
    //public List<CopyLayerInstance> CopyLayers { get; set; }

    // Constructor
    public MeshVert(Vector3 pos)
    {
        DefaultPosition = Position = pos;
        Faces = new Dictionary<MeshFace, CopyLayerInstance>();
        //DefaultCopyLayerInstance = new CopyLayerInstance(this);
    }

    // Interface Implementation


    // IMeshElement
    public void Reset()
    {
        IsInMesh = false;
        foreach (var f in Faces)
        {
            f.Value.parentIndex = -1;
            f.Value.IsFinished = false;
        }

        DefaultCopyLayerInstance = null;
        IsFinished = false;
        //CopyLayerCount = Faces.Count;
        //DefaultCopyLayerInstance = Faces.First().Value;
    }

    public int AddToMesh()
    {
        return -1;
    }
    public int AddToMesh(MeshFace face, int triIndex)
    {
        var layer = Faces[face];

        MeshContainer.Instance.Vertices.Add(Position + layer.offsetPosition);
        layer.parentIndex = MeshContainer.Instance.Vertices.Count - 1;
        layer.elementIndex = triIndex;

        if (!IsInMesh || DefaultCopyLayerInstance == null)
        {
            MeshIndex = layer.parentIndex;
            DefaultCopyLayerInstance = layer;
        }

        IsInMesh = true;
        return layer.parentIndex;
    }

    // IMoveable
    public void ResetPosition()
    {
        Position = DefaultPosition;
        foreach (var face in Faces)
            face.Value.offsetPosition = Vector3.zero;
    }

    public void SetPosition(Vector3 pos)//, bool allInstances = true)
    {
        //foreach (var face in Faces.Where(a => a.Value.parentIndex != -1 && a.Value.id))
        //{
        //    MeshContainer.Instance.Vertices[face.Value.parentIndex] = pos + face.Value.offsetPosition;
        //}
    }

    // IFaces
    public void AddFace(MeshFace face)
    {
        Faces.Add(face, new CopyLayerInstance(this, face));
    }

    public List<MeshFace> GetAdjacentFaces(MeshFace face)
    {
        return Faces.Keys.Where(a => a.IsNeighbour(face)).ToList();//matches;
    }

    public int GetFaceIndex(MeshFace face)
    {
        return Faces[face].parentIndex;
    }

    public List<MeshFace> HasFaces(List<MeshFace> faces)
    {
        var matches = new List<MeshFace>();

        foreach (var face in faces)
            if (Faces.ContainsKey(face))
                matches.Add(face);

        return matches;
    }

    public int HasFaces(params MeshFace[] faces)
    {
        var matches = 0;

        foreach (var face in faces)
            if (Faces.ContainsKey(face))
                matches++;

        return matches;
    }

    // ICopyable
    //public void AddElementToCopyLayer(IMeshElement element)
    //{
    //    if (CopyLayers.Last().id != CopyLayerManager.currentLayer.id)
    //        CopyLayers.Add(new CopyLayerInstance(this));

    //    CopyLayers.Last().Elements.Add(element);
    //}

    //public void AddElementToCopyLayer(IMeshElement element, int copyLayerId)
    //{
    //    GetCopyLayerInstance(copyLayerId).Elements.Add(element);
    //}
    public CopyLayerInstance GetCopyLayerInstance(MeshFace face)
    {
        return Faces[face];
    }

    //public void SetLayerAsFinished(MeshFace face)
    //{
    //    Faces[face].IsFinished = true;
    //    //if (AllLayersAreFinished())
    //    //{
    //    //    //Debug.Log("Changing to default index");
    //    //    Faces.Each(a => MeshContainer.Instance.Triangles[a.Value.elementIndex] = DefaultCopyLayerInstance.parentIndex);
    //    //    IsFinished = true;
    //    //}
    //}

    //public void CheckIfFinished()
    //{
    //    if (AllLayersAreFinished())
    //    {
    //        Faces.Each(a => MeshContainer.Instance.Triangles[a.Value.elementIndex] = DefaultCopyLayerInstance.parentIndex);
    //        IsFinished = true;
    //    }
    //}

    public bool AllLayersAreFinished()
    {
        foreach (var face in Faces)
        {
            if (!face.Value.IsFinished)
                return false;
        }
        return true;
    }

    public int GetDefaultIndex()
    {
        return DefaultCopyLayerInstance.parentIndex;
    }

    public void ChangeElementsCopyLayer(MeshFace element, int copyLayerId = -1)
    {
        Faces[element].ChangeCopyLayer(copyLayerId);
    }

    public List<CopyLayerInstance> GetCopyLayerInstances(int copyLayerId)
    {
        return Faces.Values.Where(a => a.id == copyLayerId).ToList();
    }

    public List<MeshFace> GetCopyLayerElements(int copyLayerId)
    {
        return Faces.Where(a => a.Value.id == copyLayerId).Select(a => a.Key).ToList();
    }

    public void ConsolidateAllCopyLayers()
    {
        foreach (var face in Faces.Skip(1))
        {
            //face.Value.RemoveFromCopyLayer();
            //Faces[face.Key] = Faces.First().Value;// DefaultCopyLayerInstance;
            //face.Value.elements.Add(face.Key);
        }
    }

    public void RemoveCopyLayer(int copyLayerId, bool removeInstanceFromLayer = true)
    {
        foreach (var copyLayer in GetCopyLayerInstances(copyLayerId))
            RemoveCopyLayer(copyLayer, removeInstanceFromLayer);
    }

    public void RemoveCopyLayer(CopyLayerInstance copyLayerInstance, bool removeInstanceFromLayer = true)
    {
        //Faces[copyLayerInstance.elements] = Faces.First().Value;
        //DefaultCopyLayerInstance.elements.Add(copyLayerInstance.elements.First());
        if (removeInstanceFromLayer)
            copyLayerInstance.RemoveFromCopyLayer();
    }

    public void SetCopyLayerOffset(Vector3 pos, int copyLayerId, bool addToCurrent)
    {
        foreach (var copyLayerInstance in GetCopyLayerInstances(copyLayerId))
            copyLayerInstance.offsetPosition = addToCurrent ? copyLayerInstance.offsetPosition + pos - Position : pos - Position;
    }



    // Overrides
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public bool Equals(MeshVert other)
    {
        return Position == other.Position;
    }

    //public override bool Equals(object obj)
    //{
    //    return base.Equals(obj);
    //}

    public static bool operator ==(MeshVert a, MeshVert b)
    {
        return a.Position == b.Position;
    }

    public static bool operator !=(MeshVert a, MeshVert b)
    {
        return a.Position != b.Position;
    }

    public static Vector3 operator +(MeshVert a, MeshVert b)
    {
        return a.Position + b.Position;
    }

    public static Vector3 operator -(MeshVert a, MeshVert b)
    {
        return a.Position - b.Position;
    }

}






///// <summary>
///// Adds Tri to the first copyLayer
///// </summary>
///// <param name="tri"></param>
//public void AddTri(MeshTri tri)
//{
//    Tris[tri] = copyLayers.First();
//}

///// <summary>
///// Adds Tri to a new Unique copyLayer
///// </summary>
///// <param name="tri"></param>
//public void AddTriToUniqueInstance(MeshTri tri)
//{
//    copyLayers.Add(new CopyLayerInstance(this));
//    Tris[tri] = copyLayers.Last();
//}

///// <summary>
///// Adds Tri to selected copyLayer
///// </summary>
///// <param name="tri"></param>
///// <param name="copyLayer"></param>
//public void AddTriToInstance(MeshTri tri, int copyLayerId)
//{
//    var copyLayer = GetInstance(copyLayerId);
//    Tris[tri] = copyLayer == null ? copyLayers.First() : copyLayer;
//}

///// <summary>
///// Puts all tris on the same copyLayer, and removes the others
///// </summary>
//public void ConsolidateAllInstances()
//{
//}

///// <summary>
///// Removes selected copyLayer.
///// </summary>
///// <param name="keepTris">True: Moves Tris to first copyLayer</param>
//public void RemoveInstance(CopyLayerInstance copyLayer)
//{
//}

///// <summary>
///// Returns the copyLayer with matching id.
///// </summary>
///// <param name="id"></param>
///// <returns></returns>
//CopyLayerInstance GetInstance(int id)
//{
//    var copyLayer = copyLayers.Where(a => a.id == id).FirstOrDefault();
//    if (copyLayer == null)
//    {
//        Debug.LogError("Unable to find MeshVert Instance from id:" + id);
//        return null;
//    }
//    return copyLayer;
//}



///// <summary>
///// Returns a list of all attached tris.
///// </summary>
///// <returns></returns>
//public IEnumerable<MeshTri> GetTris()
//{
//    foreach (var t in Tris)
//        yield return t.Key;
//}

///// <summary>
///// Returns a list of tris that all share the same copyLayer.
///// </summary>
///// <param name="i"></param>
///// <returns></returns>
//public IEnumerable<MeshTri> GetTris(int i)
//{
//    foreach (var t in Tris)
//    {
//        if (t.Value.id == i)
//            yield return t.Key;
//    }
//}

///// <summary>
///// Returns a list of tris that share at least 2 vertex positions.
///// </summary>
///// <param name="verts"></param>
///// <returns></returns>
//public IEnumerable<MeshTri> GetTris(List<MeshVert> verts)
//{
//    foreach (var t in Tris)
//    {
//        if (t.Key.HasVerts(verts) >= 2)
//            yield return t.Key;
//    }
//}
