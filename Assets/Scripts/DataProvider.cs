using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.SceneUnderstanding;

public class DataProvider : MonoBehaviour
{
    [Tooltip("The game object that will be the parent of all Scene Understanding-related game objects. If left empty, one will be created with the name 'Root'")]
    public GameObject SceneRoot;

    [Tooltip("Size of the sphere around the hololens to read data for")]
    [Range(5f, 100f)]
    public float BoundingSphereRadiusInMeters;

    [Tooltip("Whether or not to enable inference for the scene understanding")]
    public bool UseInference;

    [Tooltip("The level of detail to use for the scene mesh")]
    public SceneMeshLevelOfDetail LevelOfDetail = SceneMeshLevelOfDetail.Coarse;


    private readonly float MinBoundingSphereRadiusInMeters = 5f;
    private readonly float MaxBoundingSphereRadiusInMeters = 100f;

    private readonly object DataLock = new object();

    private async void Start()
    {
        if(!SceneObserver.IsSupported())
        {
            Debug.LogError("DataProvider: Scene understanding is not supported!");
            return;
        }

        var access = await SceneObserver.RequestAccessAsync();
        if(access != SceneObserverAccessStatus.Allowed)
        {
            Debug.LogError("DataProvider: Could not gain access to scene understanding");
            return;
        }

        Debug.Log("DataProvider: Scene understanding allowed");

        SceneRoot = SceneRoot == null ? new GameObject("Scene Root") : SceneRoot;

        Task.Run(() => RetrieveDataCondinuously());
    }

    private void RetrieveDataCondinuously()
    {
        RetrieveData(SceneMeshLevelOfDetail.Coarse);

        while(true)
        {
            RetrieveData(LevelOfDetail);
        }
    }

    /* Borrowed from microsoft's example: 
     * https://github.com/microsoft/MixedReality-SceneUnderstanding-Samples/blob/main/Assets/SceneUnderstanding/Core/Understanding/Scripts/SceneUnderstandingManager.cs
     */
    private void RetrieveData(SceneMeshLevelOfDetail levelOfDetail)
    {
        try
        {
            SceneQuerySettings querySettings = new SceneQuerySettings();
            querySettings.EnableSceneObjectQuads = true;
            querySettings.EnableSceneObjectMeshes = true;
            querySettings.EnableOnlyObservedSceneObjects = !UseInference;
            querySettings.RequestedMeshLevelOfDetail = levelOfDetail;

            // Ensure that the bounding radius is within the min/max range.
            float boundingSphereRadiusInMeters = Mathf.Clamp(BoundingSphereRadiusInMeters, MinBoundingSphereRadiusInMeters, MaxBoundingSphereRadiusInMeters);

            // Make sure the scene query has completed swap with latestSUSceneData under lock to ensure the application is always pointing to a valid scene.
            Scene scene = SceneObserver.ComputeAsync(querySettings, boundingSphereRadiusInMeters).GetAwaiter().GetResult();

            if(scene != null)
            {
                Debug.Log("Successfully got the scene!");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

    }
}
