using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


public interface IMeshElement
{
    int MeshIndex { get; set; }
    bool IsInMesh { get; set; }

    int AddToMesh();
    void Reset();
}

public interface IFaces
{
    Dictionary<MeshFace, CopyLayerInstance> Faces { get; set; }

    /// <summary>
    /// Adds a face to the list
    /// </summary>
    /// <param name="face"></param>
    /// <returns></returns>
    void AddFace(MeshFace face);

    /// <summary>
    /// Checks for faces which share an edge
    /// </summary>
    /// <param name="face"></param>
    /// <returns>Returns a list of matching faces</returns>
    List<MeshFace> GetAdjacentFaces(MeshFace face);

    /// <summary>
    /// Checks for matching faces.
    /// </summary>
    /// <param name="faces"></param>
    /// <returns>Returns a list of matches</returns>
    List<MeshFace> HasFaces(List<MeshFace> faces);

    /// <summary>
    /// Returns vertex index for an attached face.
    /// </summary>
    /// <param name="face"></param>
    /// <returns></returns>
    int GetFaceIndex(MeshFace face);

    /// <summary>
    /// Checks for matchign faces
    /// </summary>
    /// <param name="faces"></param>
    /// <returns>Returns number of matches</returns>
    int HasFaces(params MeshFace[] faces);
}

public interface IVertices
{
    List<MeshVert> Vertices { get; set; }
    Vector3 Normal { get; set; }
    Vector3 Center { get; set; }

    /// <summary>
    /// Checks for matching Vertices
    /// </summary>
    /// <param name="verts"></param>
    /// <returns>Returns the number of matches</returns>
    int HasVerts(List<MeshVert> verts);

    /// <summary>
    /// Checks for matching Vertices
    /// </summary>
    /// <param name="verts"></param>
    /// <returns>Returns the number of matches</returns>
    int HasVerts(params Vector3[] verts);

    /// <summary>
    /// Checks if 2 vertices are shared
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    bool IsNeighbour(IVertices other);
}

public static class IVerticesExtensions
{
    public static Vector3 CalculateNormal(this IVertices source)
    {
        var dir = Vector3.Cross(source.Vertices[1] - source.Vertices[0], source.Vertices[2] - source.Vertices[0]);
        return Vector3.Normalize(dir);
    }

    public static Vector3 CalculateCenter(this IVertices source)
    {
        var sum = Vector3.zero;
        source.Vertices.ForEach(v => sum += v.Position);
        return sum / source.Vertices.Count;
    }

    //public static IEnumerable<MeshVert> MakeUnique(this IVertices source, MeshFace self)
    //{
    //    source.Vertices.ForEach(v => v.)
    //}

}

public interface IMoveable
{
    Vector3 Position { get; set; }
    Vector3 DefaultPosition { get; set; }

    void SetPosition(Vector3 pos);
    void ResetPosition();
}

public interface ICopyable
{
    //List<CopyLayerInstance> CopyLayers { get; set; }
    //int CopyLayerCount { get; set; }
    ///// <summary>
    ///// Adds an element to a current copy layer.
    ///// </summary>
    ///// <param name="element"></param>
    //void AddElementToCopyLayer(IMeshElement element);

    ///// <summary>
    ///// Adds an element to a copy layer.
    ///// </summary>
    ///// <param name="element"></param>
    ///// <param name="copyLayerId"></param>
    //void AddElementToCopyLayer(IMeshElement element, int copyLayerId);

    /// <summary>
    /// Returns index of the default element.
    /// </summary>
    /// <returns></returns>
    int GetDefaultIndex();

    /// <summary>
    /// Moves an element onto a new Instance
    /// </summary>
    /// <param name="element"></param>
    /// <param name="copyLayerId"></param>
    void ChangeElementsCopyLayer(MeshFace element, int copyLayerId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="copyLayerId"></param>
    /// <returns>Return an Instance Layer object</returns>
    List<CopyLayerInstance> GetCopyLayerInstances(int copyLayerId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="copyLayerId"></param>
    /// <returns>Returns all elements on an instance layer</returns>
    List<MeshFace> GetCopyLayerElements(int copyLayerId);

    /// <summary>
    /// Removes all instance layers and changes thiers elements back to the original instance.
    /// </summary>
    /// <returns>Returns all elements that were moved</returns>
    void ConsolidateAllCopyLayers();

    /// <summary>
    /// Removes all instance layer and changes it's elements back to the original instance.
    /// </summary>
    /// <param name="copyLayerId"></param>
    /// <returns>Returns all elements that were moved</returns>
    void RemoveCopyLayer(int copyLayerId, bool removeInstanceFromLayer);

    /// <summary>
    /// Removes all instance layer and changes it's elements back to the original instance.
    /// </summary>
    /// <param name="copyLayerInstance"></param>
    /// <param name="removeInstanceFromLayer"></param>
    void RemoveCopyLayer(CopyLayerInstance copyLayerInstance, bool removeInstanceFromLayer);

    /// <summary>
    /// Changes the position offset an instance layer.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="copyLayerId"></param>
    /// <param name="addToCurrent">Adds offset to the current offset</param>
    void SetCopyLayerOffset(Vector3 pos, int copyLayerId, bool addToCurrent);
}
