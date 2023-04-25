using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Experimental.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;

public class TestScript : MonoBehaviour, IMixedRealitySpatialAwarenessObservationHandler<SpatialAwarenessSceneObject>
{
    private IMixedRealitySceneUnderstandingObserver observer;


    private List<GameObject> instantiatedPrefabs;

    private Dictionary<SpatialAwarenessSurfaceTypes, Dictionary<int, SpatialAwarenessSceneObject>> observedSceneObjects;

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
        Debug.Log("TestScript: Observation Added");
    }

    public void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessSceneObject> eventData)
    {
        Debug.Log("TestScript: Observation Updated");
    }

    public void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessSceneObject> eventData)
    {
        Debug.Log("TestScript: Observation Removed");
    }
}