using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Experimental.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;

public class TestScript : MonoBehaviour, IMixedRealitySpatialAwarenessObservationHandler<SpatialAwarenessSceneObject>
{

    [SerializeField]
    private bool InstantiatePrefabs = false;
    [SerializeField]
    private GameObject InstantiatedPrefab = null;
    [SerializeField]
    private Transform InstantiatedParent = null;


    private IMixedRealitySceneUnderstandingObserver observer;

    private List<GameObject> instantiatedPrefabs;

    private Dictionary<SpatialAwarenessSurfaceTypes, Dictionary<int, SpatialAwarenessSceneObject>> observedSceneObjects;

    /// <summary>
    /// Collection that tracks the IDs and count of updates for each active spatial awareness mesh.
    /// </summary>
    private Dictionary<int, uint> meshUpdateData = new Dictionary<int, uint>();

    private bool isRegistered = false;

    #region MonoBehaviour Functions

    protected void Start()
    {
        observer = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySceneUnderstandingObserver>();

        if (observer == null)
        {
            Debug.LogError("Couldn't access Scene Understanding Observer! Please make sure the current build target is set to Universal Windows Platform. "
                + "Visit https://docs.microsoft.com/windows/mixed-reality/mrtk-unity/features/spatial-awareness/scene-understanding for more information.");
            return;
        }

        observer.Enable();
        observer.RequestPlaneData = true;

        instantiatedPrefabs = new List<GameObject>();
        observedSceneObjects = new Dictionary<SpatialAwarenessSurfaceTypes, Dictionary<int, SpatialAwarenessSceneObject>>();
    }
    protected void OnEnable()
    {
        RegisterEventHandlers<IMixedRealitySpatialAwarenessObservationHandler<SpatialAwarenessSceneObject>, SpatialAwarenessSceneObject>();
    }

    protected void OnDisable()
    {
        UnregisterEventHandlers<IMixedRealitySpatialAwarenessObservationHandler<SpatialAwarenessSceneObject>, SpatialAwarenessSceneObject>();
    }

    protected void OnDestroy()
    {
        UnregisterEventHandlers<IMixedRealitySpatialAwarenessObservationHandler<SpatialAwarenessSceneObject>, SpatialAwarenessSceneObject>();
    }

    #endregion MonoBehaviour Functions

    /// <summary>
    /// Registers for the spatial awareness system events.
    /// </summary>
    protected virtual void RegisterEventHandlers<T, U>()
        where T : IMixedRealitySpatialAwarenessObservationHandler<U>
        where U : BaseSpatialAwarenessObject
    {
        if (!isRegistered && (CoreServices.SpatialAwarenessSystem != null))
        {
            Debug.Log("TestScript: Registered to Scene Understanding");

            CoreServices.SpatialAwarenessSystem.RegisterHandler<T>(this);
            isRegistered = true;
        }
    }

    /// <summary>
    /// Unregisters from the spatial awareness system events.
    /// </summary>
    protected virtual void UnregisterEventHandlers<T, U>()
        where T : IMixedRealitySpatialAwarenessObservationHandler<U>
        where U : BaseSpatialAwarenessObject
    {
        if (isRegistered && (CoreServices.SpatialAwarenessSystem != null))
        {
            Debug.Log("TestScript: Unregistered from Scene Understanding");

            CoreServices.SpatialAwarenessSystem.UnregisterHandler<T>(this);
            isRegistered = false;
        }
    }

    public void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessSceneObject> eventData)
    {
        // This method called everytime a SceneObject created by the SU observer
        // The eventData contains everything you need do something useful

        AddToData(eventData.Id);

        if (observedSceneObjects.TryGetValue(eventData.SpatialObject.SurfaceType, out Dictionary<int, SpatialAwarenessSceneObject> sceneObjectDict))
        {
            sceneObjectDict.Add(eventData.Id, eventData.SpatialObject);
        }
        else
        {
            observedSceneObjects.Add(eventData.SpatialObject.SurfaceType, new Dictionary<int, SpatialAwarenessSceneObject> { { eventData.Id, eventData.SpatialObject } });
        }

        if (InstantiatePrefabs && eventData.SpatialObject.Quads.Count > 0)
        {
            var prefab = Instantiate(InstantiatedPrefab);
            prefab.transform.SetPositionAndRotation(eventData.SpatialObject.Position, eventData.SpatialObject.Rotation);
            float sx = eventData.SpatialObject.Quads[0].Extents.x;
            float sy = eventData.SpatialObject.Quads[0].Extents.y;
            prefab.transform.localScale = new Vector3(sx, sy, .1f);
            if (InstantiatedParent)
            {
                prefab.transform.SetParent(InstantiatedParent);
            }
            instantiatedPrefabs.Add(prefab);
        }
        else
        {
            foreach (var quad in eventData.SpatialObject.Quads)
            {
                quad.GameObject.GetComponent<Renderer>().material.color = ColorForSurfaceType(eventData.SpatialObject.SurfaceType);
            }

        }
    }

    public void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessSceneObject> eventData)
    {
        UpdateData(eventData.Id);

        if (observedSceneObjects.TryGetValue(eventData.SpatialObject.SurfaceType, out Dictionary<int, SpatialAwarenessSceneObject> sceneObjectDict))
        {
            observedSceneObjects[eventData.SpatialObject.SurfaceType][eventData.Id] = eventData.SpatialObject;
        }
        else
        {
            observedSceneObjects.Add(eventData.SpatialObject.SurfaceType, new Dictionary<int, SpatialAwarenessSceneObject> { { eventData.Id, eventData.SpatialObject } });
        }
    }

    public void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessSceneObject> eventData)
    {
        RemoveFromData(eventData.Id);

        foreach (var sceneObjectDict in observedSceneObjects.Values)
        {
            sceneObjectDict?.Remove(eventData.Id);
        }
    }

    /// <summary>
    /// Gets the color of the given surface type
    /// </summary>
    /// <param name="surfaceType">The surface type to get color for</param>
    /// <returns>The color of the type</returns>
    private Color ColorForSurfaceType(SpatialAwarenessSurfaceTypes surfaceType)
    {
        // shout-out to solarized!

        switch (surfaceType)
        {
            case SpatialAwarenessSurfaceTypes.Unknown:
                return new Color32(220, 50, 47, 255); // red
            case SpatialAwarenessSurfaceTypes.Floor:
                return new Color32(38, 139, 210, 255); // blue
            case SpatialAwarenessSurfaceTypes.Ceiling:
                return new Color32(108, 113, 196, 255); // violet
            case SpatialAwarenessSurfaceTypes.Wall:
                return new Color32(181, 137, 0, 255); // yellow
            case SpatialAwarenessSurfaceTypes.Platform:
                return new Color32(133, 153, 0, 255); // green
            case SpatialAwarenessSurfaceTypes.Background:
                return new Color32(203, 75, 22, 255); // orange
            case SpatialAwarenessSurfaceTypes.World:
                return new Color32(211, 54, 130, 255); // magenta
            case SpatialAwarenessSurfaceTypes.Inferred:
                return new Color32(42, 161, 152, 255); // cyan
            default:
                return new Color32(220, 50, 47, 255); // red
        }
    }
    /// <summary>
    /// Increments the update count of the mesh with the id.
    /// </summary>
    protected void UpdateData(int eventDataId)
    {
        // A mesh has been updated. Find it and increment the update count.
        if (meshUpdateData.TryGetValue(eventDataId, out uint updateCount))
        {
            // Set the new update count.
            meshUpdateData[eventDataId] = ++updateCount;

            Debug.Log($"Mesh {eventDataId} has been updated {updateCount} times.");
        }
    }

    /// <summary>
    /// Records the mesh id when it is first added.
    /// </summary>
    protected void AddToData(int eventDataId)
    {
        // A new mesh has been added.
        Debug.Log($"Started tracking mesh {eventDataId}");
        meshUpdateData.Add(eventDataId, 0);
    }

    /// <summary>
    /// Removes the mesh id.
    /// </summary> 
    protected void RemoveFromData(int eventDataId)
    {
        // A mesh has been removed. We no longer need to track the count of updates.
        if (meshUpdateData.ContainsKey(eventDataId))
        {
            Debug.Log($"No longer tracking mesh {eventDataId}.");
            meshUpdateData.Remove(eventDataId);
        }
    }
}