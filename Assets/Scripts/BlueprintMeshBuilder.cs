using System.Collections.Generic;
using UnityEngine;
using Unity.InferenceEngine;
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
    private float wallthreshold = 0.2f;

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
        for(int y = 0; y < 223; y++)
        {
            for(int x = 0; x < 223; x++)
            {

                int blIndex = y * 224 + x;
                int brIndex = y * 224 + (x + 1);
                int tlIndex = (y + 1) * 224 + x;
                int trIndex = (y + 1) * 224 + (x + 1);
                float vBL = aiData[blIndex];
                float vBR = aiData[brIndex];
                float vTL = aiData[tlIndex];
                float vTR = aiData[trIndex];
                int state = 0;
                if (vBL > wallthreshold) state |= 1;
                if (vBR > wallthreshold) state |= 2;
                if (vTR > wallthreshold) state |= 4;
                if (vTL > wallthreshold) state |= 8;
                if (state == 0) continue;
                float localX = (x - 112) * stepSize;
                float localZ = (y - 112) * stepSize;
                Vector3 pBL = blueprintWorldCentre + new Vector3(localX, 0.05f, localZ);
                Vector3 pBR = blueprintWorldCentre + new Vector3(localX + stepSize, 0.05f, localZ);
                Vector3 pTL = blueprintWorldCentre + new Vector3(localX, 0.05f, localZ + stepSize);
                Vector3 pTR = blueprintWorldCentre + new Vector3(localX + stepSize, 0.05f, localZ + stepSize);
                Vector3 edgeBottom = GetLerpedPoint(pBL, pBR, vBL, vBR);
                Vector3 edgeRight = GetLerpedPoint(pBR, pTR, vBR, vTR);
                Vector3 edgeTop = GetLerpedPoint(pTL, pTR, vTL, vTR);
                Vector3 edgeLeft = GetLerpedPoint(pBL, pTL, vBL, vTL);

                BuildCellGeometry(state, pBL, pBR, pTL, pTR, edgeBottom, edgeRight, edgeTop, edgeLeft, vertices, triangles);

            }
        }
        Mesh proceduralMesh = new Mesh();
        proceduralMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        proceduralMesh.vertices = vertices.ToArray();
        proceduralMesh.triangles = triangles.ToArray();
        proceduralMesh.RecalculateNormals();
        meshFilter.mesh = stepSize;
        statusText.text = "Mesh successfully generated";
    }
    private Vector3 GetLerpedPoint(Vector3 p1, Vector3 p2, float val1, float val2)
    {
        if (Mathf.Approximately(val1, val2)) return Vector3.Lerp(p1, p2, 0.5f);
        float t = (wallthreshold - val1) / (val2 - val1);
        return Vector3.Lerp(p1, p2, t);
    }
    private void BuildCellGeometry(int state, Vector3 bl, Vector3 br, Vector3 tl, Vector3 tr, Vector3 eb, Vector3 er, Vector3 et, Vector3 el, List<Vector3> verts, List<int> tris)
    {
        void AddTri(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            int cv = verts.Count;
            verts.Add(p1); verts.Add(p2); verts.Add(p3);
            tris.Add(cv); tris.Add(cv + 1); tris.Add(cv + 2);
        }
        void AddExtrusionWall(Vector3 top1, Vector3 top2)
        {
            int cv = verts.Count;
            Vector3 bot1 = new Vector3(top1.x, top1.y - .05f, top1.z);
            Vector3 bot2 = new Vector3(top2.x, top2.y - .05f, top2.z);
            verts.Add(top1); verts.Add(top2);
            verts.Add(bot1); verts.Add(bot2);
            tris.Add(cv); tris.Add(cv + 1); tris.Add(cv + 2);
            tris.Add(cv + 1); tris.Add(cv + 3); tris.Add(cv + 2);
        }
        switch (state)
        {
            case 1: AddTri(bl, eb, el); AddExtrusionWall(el, eb); break;
            case 2: AddTri(eb, br, er); AddExtrusionWall(eb, er); break;
            case 3: AddTri(bl, br, er); AddTri(el, bl, er); AddExtrusionWall(el, er); break;
            case 4: AddTri(et, tr, er); AddExtrusionWall(er, et); break;
            case 5: AddTri(bl, eb, el); AddTri(et, tr, er); AddExtrusionWall(el, eb); AddExtrusionWall(er, et); break;
            case 6: AddTri(eb, br, tr); AddTri(et, eb, tr); AddExtrusionWall(eb, et); break;
            case 7: AddTri(bl, br, tr); AddTri(el, bl, tr); AddTri(et, el, tr); AddExtrusionWall(el, et); break;
            case 8: AddTri(tl, et, el); AddExtrusionWall(et, el); break;
            case 9: AddTri(bl, eb, et); AddTri(tl, bl, et); AddExtrusionWall(et, eb); break;
            case 10: AddTri(tl, et, el); AddTri(eb, br, er); AddExtrusionWall(et, el); AddExtrusionWall(eb, er); break;
            case 11: AddTri(tl, bl, br); AddTri(et, tl, br); AddTri(er, et, br); AddExtrusionWall(et, er); break;
            case 12: AddTri(tl, er, el); AddTri(tr, er, tl); AddExtrusionWall(er, el); break;
            case 13: AddTri(bl, eb, er); AddTri(tl, bl, er); AddTri(tr, tl, er); AddExtrusionWall(eb, er); break;
            case 14: AddTri(tl, eb, el); AddTri(tr, eb, tl); AddTri(br, eb, tr); AddExtrusionWall(el, eb); break;
            case 15: AddTri(bl, br, tr); AddTri(tl, bl, tr); break;
        }
    }
}
