using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the CopyLayers
/// </summary>
public static class CopyLayerManager
{
    public static CopyLayer currentLayer;

    static List<CopyLayer> Layers;
    static Queue<int> idQueue = new Queue<int>();
    static int highestId;

    static CopyLayerManager()
    {
        //idQueue = new Queue<int>();
        //Layers = new List<CopyLayer>();
        //GenerateNewIds(100);
    }

    /// <summary>
    /// Generates a batch of id's for usage.
    /// </summary>
    /// <param name="amount"></param>
    static void GenerateNewIds(int amount)
    {
        highestId += amount;
        for (int i = 0; i < highestId + 1; i++)
            idQueue.Enqueue(i);
    }

    /// <summary>
    /// Sets the next Layer up
    /// </summary>
    public static void GotoNextLayer()
    {
        if (currentLayer != null && currentLayer.Count() == 0) return;
        if (idQueue.Count == 0) GenerateNewIds(100);

        currentLayer = new CopyLayer(idQueue.Dequeue());
        Layers.Add(currentLayer);
    }

    /// <summary>
    /// Returns the number of layers
    /// </summary>
    /// <returns></returns>
    public static int NumberOfLayers()
    {
        return Layers.Count();
    }


    /// <summary>
    /// Returns a layer from an id.
    /// </summary>
    /// <param name="copyLayerId"></param>
    /// <returns></returns>
    public static CopyLayer GetLayer(int copyLayerId = -1)
    {
        if (copyLayerId == -1 || currentLayer.id == copyLayerId)
        {
            if (currentLayer == null)
                GotoNextLayer();
            return currentLayer;
        }

        return Layers.FirstOrDefault(l => l.id == copyLayerId);
    }

    /// <summary>
    /// Removes a Layer, requeuing it's id.
    /// </summary>
    /// <param name="removedCopyLayerId"></param>
    public static void RemoveLayer(int removedCopyLayerId)
    {
        RemoveLayer(GetLayer(removedCopyLayerId));
    }

    /// <summary>
    /// Removes a Layer, requeuing it's id.
    /// </summary>
    /// <param name="copyLayer"></param>
    public static void RemoveLayer(CopyLayer copyLayer)
    {
        idQueue.Enqueue(copyLayer.id);
        Layers.Remove(copyLayer);
        copyLayer.RemoveInstances();
    }

    public static void ResetAllIndexes()
    {
        Layers.ForEach(a => a.ResetInstanceIndexes());
    }

    public static void ResetLayers()
    {
        idQueue = new Queue<int>();
        Layers = new List<CopyLayer>();
        GenerateNewIds(100);
    }

    //public static void SetLayerAsFinished(int layerId)
    //{
    //    Layers.FirstOrDefault(a => a.id == layerId).SetInstancesAsFinished();
    //}
}

/// <summary>
/// Contains all the elements that reference this layer
/// </summary>
public class CopyLayer
{
    public int id { get; private set; }
    List<CopyLayerInstance> Instances;

    public CopyLayer(int _id)
    {
        id = _id;
        Instances = new List<CopyLayerInstance>();
    }

    /// <summary>
    /// Returns CopyLayerInstance Count
    /// </summary>
    /// <returns></returns>
    public int Count()
    {
        return Instances.Count();
    }

    /// <summary>
    /// Adds a new CopyLayerInstance
    /// </summary>
    /// <param name="instance"></param>
    public void Add(CopyLayerInstance instance)
    {
        Instances.Add(instance);
    }

    /// <summary>
    /// Calls RemoveElement on each CopyLayerInstance
    /// </summary>
    public void RemoveInstances()
    {
        if (!Instances.IsNotNullOrEmpty()) return;
        Instances.ForEach(cli => cli.RemoveFromParent());
    }

    public void RemoveInstance(CopyLayerInstance instance)
    {
        Instances.Remove(instance);
        if (Instances.Count == 0)
            CopyLayerManager.RemoveLayer(this);
    }

    public void ResetInstanceIndexes()
    {
        Instances.ForEach(a => a.parentIndex = -1);
    }

    //public void SetInstancesAsFinished()
    //{
    //    Instances.ForEach(a => a.SetFinished());
    //}

    public List<CopyLayerInstance> GetInstances()
    {
        return Instances;
    }
}

/// <summary>
/// Forms one element that makes up a copy layer
/// </summary>
public class CopyLayerInstance
{
    public int parentIndex = -1;
    public int elementIndex;
    public Vector3 offsetPosition;
    public MeshFace element;
    public bool IsFinished;
    CopyLayer copyLayer;
    public MeshVert parent;

    public int id { get { return copyLayer.id; } }

    public CopyLayerInstance(MeshVert _parent, MeshFace _element, int copyLayerId = -1)
    {
        parent = _parent;
        element = _element;
        offsetPosition = Vector3.zero;
        copyLayer = CopyLayerManager.GetLayer(copyLayerId);
        copyLayer.Add(this);
    }

    public void RemoveFromCopyLayer()
    {
        copyLayer.RemoveInstance(this);
    }

    public void RemoveFromParent()
    {
        parent.RemoveCopyLayer(this, false);
    }

    public void ChangeCopyLayer(int copyLayerId = -1)
    {
        copyLayer.RemoveInstance(this);
        copyLayer = CopyLayerManager.GetLayer(copyLayerId);
        copyLayer.Add(this);
    }

    //public void SetFinished()
    //{
    //    IsFinished = true;
    //    //parent.CopyLayerCount--;
    //    //if (parent.CopyLayerCount == 0)
    //    //    parent.SetLayersToDefault();
    //}
}

