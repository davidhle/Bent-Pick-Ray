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
    public Vector3 possitionDiff;
    public Vector3 rotationDiff;
    public Vector3 lastRotation;
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
            Debug.Log("left r: " + r);
            DrawQuadraticBezierCurve(leftHandController.transform.position, leftHandController.transform.position + leftHandController.transform.TransformDirection(Vector3.forward) * 0.5f, finalPos);
            // DrawQuadraticBezierCurve(leftHandController.transform.position, handle, finalPos);
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
        possitionDiff = new Vector3(0, 0, 0);
        rotationDiff = new Vector3(0, 0, 0);
        lastRotation = new Vector3(0, 0, 0);
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
        Vector3 translation = GrabScaling(pos);

        if (possitionDiff.x > 0)
        {
            if (translation.x - selectedObject.transform.localPosition.x > possitionDiff.x)
            {
                translation.x = translation.x - possitionDiff.x;
            }
            else
            {
                possitionDiff.x = translation.x - selectedObject.transform.localPosition.x;
                translation.x = selectedObject.transform.localPosition.x;
            }
        }
        if (possitionDiff.x < 0)
        {
            if (translation.x - selectedObject.transform.localPosition.x < possitionDiff.x)
            {
                translation.x = translation.x - possitionDiff.x;
            }
            else
            {
                possitionDiff.x = translation.x - selectedObject.transform.localPosition.x;
                translation.x = selectedObject.transform.localPosition.x;
            }
        }

        if (possitionDiff.y > 0)
        {
            if (translation.y - selectedObject.transform.localPosition.y > possitionDiff.y)
            {
                translation.y = translation.y - possitionDiff.y;
            }
            else
            {
                possitionDiff.y = translation.y - selectedObject.transform.localPosition.y;
                translation.y = selectedObject.transform.localPosition.y;
            }
        }
        else
        {
            if (translation.y - selectedObject.transform.localPosition.y < possitionDiff.y)
            {
                translation.y = translation.y - possitionDiff.y;
            }
            else
            {
                possitionDiff.y = translation.y - selectedObject.transform.localPosition.y;
                translation.y = selectedObject.transform.localPosition.y;
            }
        }

        if (possitionDiff.z > 0)
        {
            if (translation.z - selectedObject.transform.localPosition.z > possitionDiff.z)
            {
                translation.z = translation.z - possitionDiff.z;
            }
            else
            {
                possitionDiff.z = translation.z - selectedObject.transform.localPosition.z;
                translation.z = selectedObject.transform.localPosition.z;
            }
        }
        else
        {
            if (translation.z - selectedObject.transform.localPosition.z < possitionDiff.z)
            {
                translation.z = translation.z - possitionDiff.z;
            }
            else
            {
                possitionDiff.z = translation.z - selectedObject.transform.localPosition.z;
                translation.z = selectedObject.transform.localPosition.z;
            }
        }
        selectedObject.transform.localPosition = translation;

        Vector3 rotationVector = selectedObjectMatrix.rotation.eulerAngles;

        // get x rotation
        if (rotationDiff.x < 0) rotationDiff.x += 360;
        if (lastRotation.x < 0) lastRotation.x += 360;
        if (rotationVector.x < 0) rotationVector.x += 360;

        float left = (360 - lastRotation.x) + rotationDiff.x;
        float right = lastRotation.x - rotationDiff.x;

        if (lastRotation.x < rotationDiff.x)
        {
            if (rotationDiff.x > 0)
            {
                left = rotationDiff.x - lastRotation.x;
                right = (360 - rotationDiff.x) + lastRotation.x;
            }
            else
            {
                left = (360 - rotationDiff.x) + lastRotation.x;
                right = rotationDiff.x - lastRotation.x;
            }
        }

        float shortest = ((left <= right) ? left : (right * -1));
        Vector3 currentRotation = rotationVector - lastRotation;

        if ((currentRotation.x > 0 && shortest > 0) || (currentRotation.x < 0 && shortest < 0))
        {
            lastRotation.x += currentRotation.x;
            rotationVector.x = rotationDiff.x;
        }
        else
        {
            rotationDiff.x += currentRotation.x;
            lastRotation.x += currentRotation.x;
            rotationVector.x = rotationDiff.x;
        }

        //get y rotation
        if (rotationDiff.y < 0) rotationDiff.y += 360;
        if (lastRotation.y < 0) lastRotation.y += 360;
        if (rotationVector.y < 0) rotationVector.y += 360;

        left = (360 - lastRotation.y) + rotationDiff.y;
        right = lastRotation.y - rotationDiff.y;

        if (lastRotation.y < rotationDiff.y)
        {
            if (rotationDiff.y > 0)
            {
                left = rotationDiff.y - lastRotation.y;
                right = (360 - rotationDiff.y) + lastRotation.y;
            }
            else
            {
                left = (360 - rotationDiff.y) + lastRotation.y;
                right = rotationDiff.y - lastRotation.y;
            }
        }

        shortest = ((left <= right) ? left : (right * -1));
        currentRotation = rotationVector - lastRotation;

        if ((currentRotation.y > 0 && shortest > 0) || (currentRotation.y < 0 && shortest < 0))
        {
            lastRotation.y += currentRotation.y;
            rotationVector.y = rotationDiff.y;
        }
        else
        {
            rotationDiff.y += currentRotation.y;
            lastRotation.y += currentRotation.y;
            rotationVector.y = rotationDiff.y;
        }

        //get z rotation
        if (rotationDiff.z < 0) rotationDiff.z += 360;
        if (lastRotation.z < 0) lastRotation.z += 360;
        if (rotationVector.z < 0) rotationVector.z += 360;

        left = (360 - lastRotation.z) + rotationDiff.z;
        right = lastRotation.z - rotationDiff.z;

        if (lastRotation.z < rotationDiff.z)
        {
            if (rotationDiff.z > 0)
            {
                left = rotationDiff.z - lastRotation.z;
                right = (360 - rotationDiff.z) + lastRotation.z;
            }
            else
            {
                left = (360 - rotationDiff.z) + lastRotation.z;
                right = rotationDiff.z - lastRotation.z;
            }
        }

        shortest = ((left <= right) ? left : (right * -1));
        currentRotation = rotationVector - lastRotation;

        if ((currentRotation.z > 0 && shortest > 0) || (currentRotation.z < 0 && shortest < 0))
        {
            lastRotation.z += currentRotation.z;
            rotationVector.z = rotationDiff.z;
        }
        else
        {
            rotationDiff.z += currentRotation.z;
            lastRotation.z += currentRotation.z;
            rotationVector.z = rotationDiff.z;
        }

        //get final rotation
        Quaternion rotation = Quaternion.Euler(rotationVector);
        //Debug.Log(selectedObjectMatrix.rotation);

        selectedObject.transform.localRotation = rotation;
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
