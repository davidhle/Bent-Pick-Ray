using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class User2 : MonoBehaviour
{
    private GameObject leftHandController;
    private LineRenderer leftRayRenderer;
    private GameObject leftRayIntersectionSphere;
    private GameObject selectables;
    private RaycastHit leftHit;
    public LayerMask myLayerMask;
    public GameObject selectedObject = null;
    private GameObject scene = null;
    private XRController leftXRController;
    public Matrix4x4 selectedObjectMatrix;
    private GameObject mainCamera;

    private bool gripButtonLF = false;
    public Matrix4x4 oPrime;
    private GameObject XRRig;
    private GameObject cameraOffset;
    private Matrix4x4 O, hC, XRR, S, CO, worldTransform;
    private Matrix4x4 hitPosition, hitPositionLocal;
    private Vector3 v1, v2, a, m;
    private float alpha;
    private bool bending;
    private float s;
    float armLength;
    // Start is called before the first frame update
    void Start()
    {
        leftHandController = GameObject.Find("LeftHand Controller");
        scene = GameObject.Find("Scene");
        selectables = GameObject.Find("Selectables");
        cameraOffset = GameObject.Find("Camera Offset");
        XRRig = GameObject.Find("XR Rig");
        bending = false;
        mainCamera = GameObject.Find("Main Camera");

        if (leftHandController != null) // guard
        {
            leftXRController = leftHandController.GetComponent<XRController>();

            leftRayRenderer = leftHandController.GetComponent<LineRenderer>();
            if (leftRayRenderer == null) leftRayRenderer = leftHandController.AddComponent<LineRenderer>() as LineRenderer;
            //leftRayRenderer.name = "left Ray Renderer";
            leftRayRenderer.startWidth = 0.01f;
            leftRayRenderer.positionCount = 2; // two points (one line segment)
            leftRayRenderer.enabled = true;

            // geometry for intersection visualization
            leftRayIntersectionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //leftRayIntersectionSphere.transform.parent = this.gameObject.transform;
            leftRayIntersectionSphere.name = "left Ray Intersection Sphere";
            leftRayIntersectionSphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            leftRayIntersectionSphere.GetComponent<MeshRenderer>().material.color = Color.yellow;
            leftRayIntersectionSphere.GetComponent<SphereCollider>().enabled = false; // disable for picking ?!
            leftRayIntersectionSphere.SetActive(false);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (selectedObject == null)
        {
            leftRayRenderer.positionCount = 2;
            if (Physics.Raycast(leftHandController.transform.position, leftHandController.transform.TransformDirection(Vector3.forward), out leftHit))
            {
                //Debug.Log("Did Hit");
                // update ray visualization
                leftRayRenderer.SetPosition(0, leftHandController.transform.position);
                leftRayRenderer.SetPosition(1, leftHit.point);

                // update intersection sphere visualization
                leftRayIntersectionSphere.SetActive(true); // show
                leftRayIntersectionSphere.transform.position = leftHit.point;
            }
            else // ray does not intersect with objects
            {
                // update ray visualization
                leftRayRenderer.SetPosition(0, leftHandController.transform.position);
                leftRayRenderer.SetPosition(1, leftHandController.transform.position + leftHandController.transform.TransformDirection(Vector3.forward) * 1000);

                // update intersection sphere visualization
                leftRayIntersectionSphere.SetActive(false); // hide
            }
        }
        else
        {
            if (!bending) {
                BendRays();
                bending = true;
            }
            Matrix4x4 finalPosM = selectedObject.transform.localToWorldMatrix * hitPositionLocal;
            Vector3 finalPos = new Vector3(finalPosM[0, 3], finalPosM[1, 3], finalPosM[2, 3]);
            float r = Vector3.Distance(m, leftHandController.transform.position); // radius of circle that makes arc
            Vector3 handle = leftHandController.transform.position + leftHandController.transform.forward * r; // radius + controller position in controller direction
            // DrawQuadraticBezierCurve(leftHandController.transform.position, leftHandController.transform.position + leftHandController.transform.TransformDirection(Vector3.forward) * 0.5f, finalPos);
            DrawQuadraticBezierCurve(leftHandController.transform.position, handle, finalPos);
        }

        Dragging();
    }

    private void Dragging()
    {
        // mapping: grip button (middle finger)
        bool gripButtonLeft = false;
        leftXRController.inputDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripButtonLeft);
        //Debug.Log("middle finger rocker: " + gripButton);

        if (gripButtonLeft != gripButtonLF) // state changed
        {
            if (gripButtonLeft) // up (false->true)
            {
                if (leftHit.collider != null && selectedObject == null && leftHit.collider.gameObject.transform.parent.gameObject == selectables)
                {
                    SelectObject(leftHit.collider.gameObject);
                }
            }
            else // down (true->false)
            {
                if (selectedObject != null)
                {
                    DeselectObject();
                }
            }
        }


        gripButtonLF = gripButtonLeft;
    }

    private void SelectObject(GameObject go)
    {
        selectedObject = go;
        hitPosition = Matrix4x4.TRS(leftHit.point, Quaternion.Euler(new Vector3(0, 0, 0)), new Vector3(0, 0, 0));
        // selectedObjectLeft.transform.SetParent(leftHandController.transform, false); // worldPositionStays = true

        AssignTransformationMatrices();
        oPrime = hC.inverse * CO.inverse * XRR.inverse * S * O;
        hitPositionLocal = selectedObject.transform.localToWorldMatrix.inverse * hitPosition;
        s = ScalingFactor();
        
        // SetTransformByMatrix(selectedObject, oPrime);
    }



    private void DeselectObject()
    {
        // selectedObjectLeft.transform.SetParent(selectables.transform, true); // worldPositionStays = true
        selectedObject = null;
        if (bending) {
          bending = false;
        }
    }

    public void UpdatePosition()
    {
        AssignTransformationMatrices();
        selectedObjectMatrix = S.inverse * XRR * CO * hC * oPrime;
        Vector3 pos = new Vector3(selectedObjectMatrix[0, 3], selectedObjectMatrix[1, 3], selectedObjectMatrix[2, 3]);
        selectedObject.transform.localPosition = GrabScaling(pos);

        selectedObject.transform.localRotation = selectedObjectMatrix.rotation;
    }

    private void AssignTransformationMatrices()
    {
        O = Matrix4x4.TRS(selectedObject.transform.localPosition, selectedObject.transform.localRotation, selectedObject.transform.localScale);
        hC = Matrix4x4.TRS(leftHandController.transform.localPosition, leftHandController.transform.localRotation, leftHandController.transform.localScale);
        S = Matrix4x4.TRS(selectables.transform.localPosition, selectables.transform.localRotation, selectables.transform.localScale);
        CO = Matrix4x4.TRS(cameraOffset.transform.localPosition, cameraOffset.transform.localRotation, cameraOffset.transform.localScale);
        XRR = Matrix4x4.TRS(XRRig.transform.localPosition, XRRig.transform.localRotation, XRRig.transform.localScale);
    }

    private void BendRays()
    {
        v1 = (selectedObject.transform.position - leftHandController.transform.position).normalized;
        v2 = new Vector3(selectedObjectMatrix[0, 3], selectedObjectMatrix[1, 3], selectedObjectMatrix[2, 3]) - leftHandController.transform.position;
        alpha = Vector3.Angle(v1, v2);
        a = ((v2 * Mathf.Cos(alpha) * v1.magnitude)/ v2.magnitude) - v1;
        m = leftHandController.transform.position - (v1.magnitude / (2 * Mathf.Cos(Mathf.PI/2 - alpha))) * a.normalized;
    }

    void DrawQuadraticBezierCurve(Vector3 point0, Vector3 point1, Vector3 point2)
    {
        leftRayRenderer.positionCount = 200;
        float t = 0f;
        Vector3 B = new Vector3(0, 0, 0);
        for (int i = 0; i < leftRayRenderer.positionCount; i++)
        {
            B = (1 - t) * (1 - t) * point0 + 2 * (1 - t) * t * point1 + t * t * point2;
            leftRayRenderer.SetPosition(i, B);
            t += (1 / (float)leftRayRenderer.positionCount);
        }

        leftRayIntersectionSphere.transform.position = point2;
    }

    private float ScalingFactor()
    {
        float objDistanceToArm = Vector3.Distance(leftHit.point, leftHandController.transform.position);
        armLength = Vector3.Distance(leftHandController.transform.position, mainCamera.transform.position);
        if (objDistanceToArm / armLength < 1)
        {
            return 0f;
        }
        else
        {
            return (objDistanceToArm / armLength);
        }
    }

    public Vector3 GrabScaling(Vector3 pos)
    {
        float currentArmLength = Vector3.Distance(leftHandController.transform.position, mainCamera.transform.position);
        float armchange = currentArmLength - armLength;
        float fScale = s * armchange;
        Vector3 fscaleDir = fScale * leftHandController.transform.TransformDirection(Vector3.forward);
        return (pos + fscaleDir);
    }
}
