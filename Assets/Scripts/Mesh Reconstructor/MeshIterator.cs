using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks the whole iteration process
/// </summary>
public class MeshIterator
{
    //float IterationTime;
    //float IterationSpeed;
    public List<MeshIteration> Iterations;
    public bool InProgress;
    public MeshContainer meshContainer;
    public int iterationIndex;

    public MeshIterator(MeshContainer container) //, float iterationTime, float iterationSpeed
    {
        meshContainer = container;
        //IterationTime = iterationTime;
        //IterationSpeed = iterationSpeed;
        Iterations = new List<MeshIteration>();
    }

    public void CreateIteration(MeshFace startingFace)
    {
        Iterations.Clear();
        CopyLayerManager.GotoNextLayer();
        var iterator = new MeshIteration();
        iterator.Create(startingFace);
        Iterations.Add(iterator);
        AddIteration(iterator);
    }

    void AddIteration(MeshIteration previous)
    {
        CopyLayerManager.GotoNextLayer();
        var iterator = new MeshIteration();
        if (iterator.CreateFromIteration(previous))
        {
            Iterations.Add(iterator);
            AddIteration(iterator);
        }
    }

    public void StartIterating()
    {
        //meshContainer.StopAllCoroutines();
        iterationIndex = 0;
        InProgress = true;
        meshContainer.StartCoroutine(FireIteration());
    }

    IEnumerator FireIteration()
    {
        var time = Time.realtimeSinceStartup + meshContainer.IterationTime;
        meshContainer.StartCoroutine(Iterations[iterationIndex].AnimateIteration(this));

        while (Time.realtimeSinceStartup < time)
            yield return null;// new UnityEngine.WaitForEndOfFrame();

        iterationIndex++;
        if (iterationIndex < Iterations.Count)
            meshContainer.StartCoroutine(FireIteration());
        else
            meshContainer.StartCoroutine(WaitForLastIteration(Iterations[iterationIndex - 1]));//InProgress = false;// meshContainer.StartCoroutine(WaitForLastIteration(Iterations[iterationIndex-1]));//
    }

    IEnumerator WaitForLastIteration(MeshIteration iteration)
    {
        var time = Time.realtimeSinceStartup + (meshContainer.IterationSpeed + meshContainer.IterationTime) * 2;
        while (iteration.InProgress || Time.realtimeSinceStartup > time)
        {
            yield return null;
        }
        InProgress = false;
        CleanUp();
    }

    void CleanUp()
    {
        Debug.Log("Iterator Cleanup");
        //foreach (var ie in iteration.IterationElements)
        //{
        //    ie.RemoveUniqueVertices();
        //    meshContainer.UpdateMesh();
        //}
        //Iterations.ForEach(a => a.IterationElements.ForEach(b => b.RemoveUniqueVertices()));
        meshContainer.UpdateMesh();
        meshContainer.CleanUp();

    }
}
/// <summary>
/// Keeps tracks of all the elements in an interation
/// Tracks progress
/// 
/// </summary>
public class MeshIteration
{
    public List<FaceIterationElement> IterationElements;
    public int index = 0;
    public bool InProgress;

    public MeshIteration()
    {
        IterationElements = new List<FaceIterationElement>();
    }

    public void Create(MeshFace startElement)
    {
        IterationElements.Add(new FaceIterationElement(startElement, null, 0));
    }

    public bool CreateFromIteration(MeshIteration iteration)
    {
        index = iteration.index + 1;
        foreach (var iterElement in iteration.IterationElements)
        {
            foreach (var neighbour in iterElement.element.Neighbours)
            {
                if (neighbour.IteratorIndex != -1) continue;
                // Add Option to limit number of neighbours allowed to be added.
                IterationElements.Add(new FaceIterationElement(neighbour, iterElement, index));

                //var longEdge = neighbour.GetLongEdgeNeighbour();
                //if (longEdge.IteratorIndex != -1) continue;
                //IterationElements.Add(new FaceIterationElement(longEdge, iterElement, index));
            }
        }
        return IterationElements.Count > 0;
    }

    public IEnumerator AnimateIteration(MeshIterator iterator)
    {
        InProgress = true;
        IterationElements.ForEach(a => a.AddToMesh());
        iterator.meshContainer.UpdateMesh();

        var timer = 0.0f;
        while (timer < 1)
        {
            var tick = Time.deltaTime / iterator.meshContainer.IterationSpeed;
            timer = 1 - timer <= tick * 2 ? 1 : timer + tick;

            foreach (var ie in IterationElements)
                foreach (var fv in ie.floatingVerts)
                    iterator.meshContainer.Vertices[fv.copyLayer.parentIndex] = Vector3.Lerp(fv.StartPosition, fv.meshVert.Position, EasingCurves.easeOutCubic(0, 1, timer));

            iterator.meshContainer.UpdateVertices();

            yield return null;// new UnityEngine.WaitForEndOfFrame();
        }

        foreach (var ie in IterationElements)
            ie.RemoveUniqueVertices();

        InProgress = false;
    }
}

/// <summary>
/// Interface for various iteration types
/// </summary>
public interface MeshIterationElement//<T> where T : IMeshElement
{
    IMeshElement element { get; set; }
    MeshIterationElement parentIterationElement { get; set; }

    void AddToMesh();
}

/// <summary>
/// Responsible for keeping track of the shape it's revealing, and animating the floating verts.
/// No 2 RevealIterations can have control over the same floating verts.
/// Vert Locks are obtained, and efforts to change a locked vert will cause no effect.
/// Tracks triangles index in the mesh.... but what about vertex indexes, they would need to be kept multiple times....
/// </summary>
public class FaceIterationElement// : MeshIterationElement
{
    public MeshFace element { get; set; }
    public FaceIterationElement parentIterationElement { get; set; }

    public List<MeshVert> anchorVerts;
    public List<FloatingMeshVert> floatingVerts;

    public FaceIterationElement(MeshFace _element, FaceIterationElement _parentIterationElement, int iteratorIndex)
    {
        element = _element;
        element.IteratorIndex = iteratorIndex;
        parentIterationElement = _parentIterationElement;
        element.ChangeVertCopyLayer();

        anchorVerts = new List<MeshVert>();
        floatingVerts = new List<FloatingMeshVert>();
    }

    public void AddToMesh()
    {
        element.AddToMesh();
        SetFloatingVerts();
    }

    public void RemoveUniqueVertices()
    {
        element.SetIndexesToDefaultCopyLayer();
    }

    public void SetFloatingVerts()
    {
        //if (parentIterationElement == null)
        //{
        //    anchorVerts.Add(element.Vertices.SelectRandom());
        //    anchorVerts.Add(element.Vertices.Except(anchorVerts).SelectRandom());
        //}
        //else
        //{
        //    anchorVerts = element.Vertices.Intersect(parentIterationElement.element.Vertices).ToList();
        //}

        //var halfway = (anchorVerts[0] + anchorVerts[1]) * 0.5f;
        //foreach (var v in element.Vertices.Except(anchorVerts))
        //{
        //    floatingVerts.Add(new FloatingMeshVert(v, halfway + (-element.Normal * halfway.magnitude * 0.2f), v.GetCopyLayerInstance(element)));
        //}

        foreach (var v in element.Vertices)
        {
            floatingVerts.Add(new FloatingMeshVert(v, element.Center + element.Normal, v.GetCopyLayerInstance(element)));
        }
    }
}

public struct FloatingMeshVert
{
    public Vector3 StartPosition;
    public CopyLayerInstance copyLayer;
    public MeshVert meshVert;

    public FloatingMeshVert(MeshVert _meshVert, Vector3 _pos, CopyLayerInstance _copyLayer)
    {
        meshVert = _meshVert;
        StartPosition = _pos;
        copyLayer = _copyLayer;
    }
}
