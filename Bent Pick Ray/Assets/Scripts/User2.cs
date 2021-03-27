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

    private bool gripButtonLF = false;
    public Matrix4x4 oPrime;
    private GameObject XRRig;
    private GameObject cameraOffset;
    private Matrix4x4 O, hC, XRR, S, CO, worldTransform;
    private Matrix4x4 hitPosition, hitPositionLocal;

    // Start is called before the first frame update
    void Start()
    {
        leftHandController = GameObject.Find("LeftHand Controller");
        scene = GameObject.Find("Scene");
        selectables = GameObject.Find("Selectables");
        cameraOffset = GameObject.Find("Camera Offset");
        XRRig = GameObject.Find("XR Rig");

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
            Matrix4x4 finalPosM = selectedObject.transform.localToWorldMatrix * hitPositionLocal;
            Vector3 finalPos = new Vector3(finalPosM[0, 3], finalPosM[1, 3], finalPosM[2, 3]);
            DrawQuadraticBezierCurve(leftHandController.transform.position, leftHandController.transform.position + leftHandController.transform.TransformDirection(Vector3.forward) * 0.5f, finalPos);
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
        // SetTransformByMatrix(selectedObject, oPrime);
    }



    private void DeselectObject()
    {
        // selectedObjectLeft.transform.SetParent(selectables.transform, true); // worldPositionStays = true
        selectedObject = null;

    }

    public void UpdatePosition()
    {
        AssignTransformationMatrices();
        Matrix4x4 selectedobjetMatrix = S.inverse * XRR * CO * hC * oPrime;

        selectedObject.transform.localPosition = new Vector3(selectedobjetMatrix[0, 3], selectedobjetMatrix[1, 3], selectedobjetMatrix[2, 3]);
        selectedObject.transform.localRotation = selectedobjetMatrix.rotation;
    }

    private void AssignTransformationMatrices()
    {
        O = Matrix4x4.TRS(selectedObject.transform.localPosition, selectedObject.transform.localRotation, selectedObject.transform.localScale);
        hC = Matrix4x4.TRS(leftHandController.transform.localPosition, leftHandController.transform.localRotation, leftHandController.transform.localScale);
        S = Matrix4x4.TRS(selectables.transform.localPosition, selectables.transform.localRotation, selectables.transform.localScale);
        CO = Matrix4x4.TRS(cameraOffset.transform.localPosition, cameraOffset.transform.localRotation, cameraOffset.transform.localScale);
        XRR = Matrix4x4.TRS(XRRig.transform.localPosition, XRRig.transform.localRotation, XRRig.transform.localScale);
    }

    void DrawQuadraticBezierCurve(Vector3 point0, Vector3 point1, Vector3 point2)
    {
        leftRayRenderer.positionCount = 200;
        float t = 0f;
        Vector3 B = new Vector3(0, 0, 0);
        //Debug.Log(point0);
        //Debug.Log(point1);
        //Debug.Log(point2);
        for (int i = 0; i < leftRayRenderer.positionCount; i++)
        {
            B = (1 - t) * (1 - t) * point0 + 2 * (1 - t) * t * point1 + t * t * point2;
            leftRayRenderer.SetPosition(i, B);
            t += (1 / (float)leftRayRenderer.positionCount);
        }

        leftRayIntersectionSphere.transform.position = point2;
    }
}
