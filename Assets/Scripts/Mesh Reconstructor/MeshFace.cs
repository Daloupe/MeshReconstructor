using UnityEngine;
using System.Collections.Generic;

using System;
using System.Linq;

/// <summary>
/// Interface for Face types ie Tri and Quad
/// </summary>
public abstract class MeshFace : IVertices, IMeshElement, IEquatable<MeshFace> //IVertices, 
{
    // Fields
    public List<MeshVert> Vertices { get; set; }
    public HashSet<MeshFace> Neighbours { get; set; }
    public bool IsInMesh { get; set; }
    public int MeshIndex { get; set; }
    public int IteratorIndex = -1;
    //public List<MeshVert> LongSide;
    Vector3 normal, center;
    public Vector3 Normal
    {
        get
        {
            if (normal == Vector3.zero)
                normal = this.CalculateNormal();
            return normal;
        }
        set { }
    }
    public Vector3 Center
    {
        get
        {
            if (center == Vector3.zero)
                center = this.CalculateCenter();
            return center;
        }
        set { }
    }

    // Constructor
    public MeshFace(params MeshVert[] verts)
    {
        //LongSide = new List<MeshVert>(2);
        Vertices = new List<MeshVert>();
        Vertices.AddRange(verts);
        Neighbours = new HashSet<MeshFace>();
    }

    // Methods
    public void GetNeighbours()
    {
        Vertices.ForEach(a => Neighbours.UnionWith(a.GetAdjacentFaces(this)));
    }

    // Interface Implementation
    // IMeshElement
    public void Reset()
    {
        IsInMesh = false;
        IteratorIndex = -1;
    }

    public int AddToMesh()
    {
        if (IsInMesh)
        {
            Debug.LogError("Face already in Mesh");
            return -1;
        }

        IsInMesh = true;
        MeshIndex = MeshContainer.Instance.Triangles.Count;

        for (var i = 0; i < Vertices.Count; i++)
        {
            MeshContainer.Instance.Triangles.Add(Vertices[i].AddToMesh(this, MeshIndex + i));
        }
        //Vertices.ForEach(a => MeshContainer.Instance.Triangles.Add(a.AddToMesh(this)));

        return MeshIndex;
    }

    // IVertices
    public int HasVerts(params Vector3[] verts)
    {
        return verts.ToList().Intersect(Vertices.Select(a => a.Position)).Count();
    }

    public int HasVerts(List<MeshVert> verts)
    {
        return Vertices.Intersect(verts).Count();
    }

    public bool IsNeighbour(IVertices other)
    {
        return Vertices.Intersect(other.Vertices).Count() >= 2;
    }

    //public MeshFace GetLongEdgeNeighbour()
    //{
    //    return Neighbours.First(a => a.HasVerts(LongSide) == 2);
    //}

    public void ChangeVertCopyLayer()
    {
        foreach (var v in Vertices)
        {
            v.ChangeElementsCopyLayer(this);
        }
    }

    public void SetIndexesToDefaultCopyLayer()
    {
        for (var i = 0; i < Vertices.Count; i++)
        {
            //Vertices[i].SetLayerAsFinished(this);
            MeshContainer.Instance.Triangles[MeshIndex + i] = Vertices[i].MeshIndex;
            //if (Vertices[i].AllLayersAreFinished())
            //    MeshContainer.Instance.Triangles[MeshIndex + i] = Vertices[i].GetDefaultIndex();
        }
    }

    // IEquatable
    public virtual bool Equals(MeshFace other)
    {
        return Vertices.ContainsAllFrom(other.Vertices) && other.Vertices.ContainsAllFrom(Vertices);
    }

    public override int GetHashCode()
    {
        return Vertices.Select(a => a.GetHashCode()).Aggregate((a, b) => a.GetHashCode() + b.GetHashCode()).GetHashCode();
    }
}

/// <summary>
/// Stores MeshVert references
/// Stores Neighbouring Faces
/// Provides Face info like the normal, center.
/// </summary>
public class MeshTri : MeshFace
{
    //public int triIndex; // Maybe

    public MeshTri(params MeshVert[] verts)
        : base(verts)
    {
        Vertices.ForEach(v => v.AddFace(this));
        //    LongSide.Add(Vertices[1]);
        //    LongSide.Add(Vertices[0]);

        //    if ((Vertices[2] - Vertices[1]).magnitude > (LongSide[1] - LongSide[0]).magnitude)
        //    {
        //        LongSide[0] = Vertices[2];
        //        LongSide[1] = Vertices[1];
        //    }
        //    if ((Vertices[2] - Vertices[0]).magnitude > (LongSide[1] - LongSide[0]).magnitude)
        //    {
        //        LongSide[0] = Vertices[0];
        //        LongSide[1] = Vertices[2];
        //    }
    }

}

public class MeshQuad : MeshFace
{
    //public int triIndex; // Maybe

    public MeshQuad(params MeshVert[] verts)
        : base(verts)
    {
        Vertices.ForEach(v => v.AddFace(this));
    }

}



