using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class BlueprintMeshBuilder : MonoBehaviour
{
    private ARRaycastManager raycastManager;
    [SerializeField] SentisInference inferenceEngine;
    [SerializeField] float paperWidthInMeters = 0.3f;
    private Vector3 blueprintWorldCentre;
    private bool hasValidCentre = false;
    public TMPro.TextMeshProUGUI statusText;
    private MeshFilter meshFilter;

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        meshFilter = GetComponent<MeshFilter>();
    }
    public void TryLockBlueprintCentre()
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        Vector2 screenCentre = new Vector2(Screen.width / 2, Screen.height / 2);
        raycastManager.Raycast(screenCentre, hits, TrackableType.PlaneWithinPolygon);
        if (hits.Count > 0)
        {
            var firstHit = hits[0];
            blueprintWorldCentre = firstHit.pose.position;
            hasValidCentre = true;
            Debug.Log("Blueprint Center Locked at: " + blueprintWorldCentre);
            statusText.text = "Table Locked! Ready to Generate.";
        }
        else
        {
            statusText.text = "Missed the table! Keep moving phone and try again.";
        }
    }
    public void OnGenerateMeshPressed()
    {
        float[] aiData = inferenceEngine.GetModelOutput();
        if (aiData == null)
        {
            Debug.Log("Not Ready");
            statusText.text = "AI Data Not Ready!";
            return;
        }
        if (!hasValidCentre)
        {
            Debug.Log("Please Scan the Surface");
            statusText.text = "Please Scan the table first!";
            return;
        }
        statusText.text = "Generating 3D points...";
        float stepSize = paperWidthInMeters / 224f;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for(int y = 0; y < 224; y++)
        {
            for(int x = 0; x < 224; x++)
            {
                int index = y * 224 + x;
                if (aiData[index] > 0.5f)
                {
                    float localX = (x - 112) * stepSize;
                    float localZ = (y - 112) * stepSize;
                    Vector3 wallWorldPosition = blueprintWorldCentre + new Vector3(localX, 0f, localZ);
                    float halfThick = stepSize / 2;
                }
            }
        }
        
    }

}
