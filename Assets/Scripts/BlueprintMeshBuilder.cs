using UnityEngine;
using Unity.InferenceEngine;
using System;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using NUnit.Framework;

public class BlueprintMeshBuilder : MonoBehaviour
{
    private ARRaycastManager raycastManager;
    [SerializeField] SentisInference inferenceEngine;
    [SerializeField] float paperWidthInMeters = 0.3f;
    private Vector3 blueprintWorldCentre;
    private bool hasValidCentre = false;
    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }
    public void TryLockBlueprintCentre()
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        Vector2 screenCentre = new Vector2(Screen.width / 2, Screen.height / 2);
    }

}
