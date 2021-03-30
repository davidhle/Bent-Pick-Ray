using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class User1 : MonoBehaviour
{
    private GameObject rightHandController;
    private LineRenderer rightRayRenderer;
    private GameObject rightRayIntersectionSphere;
    private GameObject selectables;
    private RaycastHit rightHit;
    public LayerMask myLayerMask;
    public GameObject selectedObject = null;
    private GameObject scene = null;
    private XRController rightXRController;
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
        rightHandController = GameObject.Find("RightHand Controller");
        scene = GameObject.Find("Scene");
        selectables = GameObject.Find("Selectables");
        cameraOffset = GameObject.Find("Camera Offset");
        XRRig = GameObject.Find("XR Rig");
        bending = false;
        mainCamera = GameObject.Find("Main Camera");

        if (rightHandController != null) // guard
        {
            rightXRController = rightHandController.GetComponent<XRController>();

            rightRayRenderer = rightHandController.GetComponent<LineRenderer>();
            if (rightRayRenderer == null) rightRayRenderer = rightHandController.AddComponent<LineRenderer>() as LineRenderer;
            //rightRayRenderer.name = "Right Ray Renderer";
            rightRayRenderer.startWidth = 0.01f;
            rightRayRenderer.positionCount = 2; // two points (one line segment)
            rightRayRenderer.enabled = true;

            // geometry for intersection visualization
            rightRayIntersectionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //rightRayIntersectionSphere.transform.parent = this.gameObject.transform;
            rightRayIntersectionSphere.name = "Right Ray Intersection Sphere";
            rightRayIntersectionSphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            rightRayIntersectionSphere.GetComponent<MeshRenderer>().material.color = Color.yellow;
            rightRayIntersectionSphere.GetComponent<SphereCollider>().enabled = false; // disable for picking ?!
            rightRayIntersectionSphere.SetActive(false); // hide

        }
    }

    // Update is called once per frame
    void Update()
    {
        if(selectedObject == null)
        {
            rightRayRenderer.positionCount = 2;
            if (Physics.Raycast(rightHandController.transform.position, rightHandController.transform.TransformDirection(Vector3.forward), out rightHit))
            {
                //Debug.Log("Did Hit");
                // update ray visualization
                rightRayRenderer.SetPosition(0, rightHandController.transform.position);
                rightRayRenderer.SetPosition(1, rightHit.point);


                // update intersection sphere visualization
                rightRayIntersectionSphere.SetActive(true); // show
                rightRayIntersectionSphere.transform.position = rightHit.point;
            }
            else // ray does not intersect with objects
            {
                // update ray visualization
                rightRayRenderer.SetPosition(0, rightHandController.transform.position);
                rightRayRenderer.SetPosition(1, rightHandController.transform.position + rightHandController.transform.TransformDirection(Vector3.forward) * 1000);

                // update intersection sphere visualization
                rightRayIntersectionSphere.SetActive(false); // hide
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
            float r = Vector3.Distance(m, rightHandController.transform.position); // radius of circle that makes arc
            // Debug.Log("m: " + m);
            Vector3 handle = rightHandController.transform.position + rightHandController.transform.forward * r; // radius + controller position in controller direction
            // DrawQuadraticBezierCurve(rightHandController.transform.position, rightHandController.transform.position + rightHandController.transform.TransformDirection(Vector3.forward)* 0.5f, finalPos);
            DrawQuadraticBezierCurve(rightHandController.transform.position, handle, finalPos);
        }
        // Debug.Log("bending: " + bending);
        Dragging();
    }

    private void Dragging()
    {
        // mapping: grip button (middle finger)
        bool gripButtonRight = false;
        rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripButtonRight);
        //Debug.Log("middle finger rocker: " + gripButton);

        if (gripButtonRight != gripButtonLF) // state changed
        {
            if (gripButtonRight) // up (false->true)
            {
                if (rightHit.collider != null && selectedObject == null && rightHit.collider.gameObject.transform.parent.gameObject == selectables)
                {
                    SelectObject(rightHit.collider.gameObject);
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


        gripButtonLF = gripButtonRight;
    }

    private void SelectObject(GameObject go)
    {
        selectedObject = go;
        hitPosition = Matrix4x4.TRS(rightHit.point, Quaternion.Euler(new Vector3(0, 0, 0)), new Vector3(0, 0, 0));
        // selectedObjectRight.transform.SetParent(rightHandController.transform, false); // worldPositionStays = true
        AssignTransformationMatrices();
        oPrime = hC.inverse * CO.inverse * XRR.inverse * S * O;
        hitPositionLocal = selectedObject.transform.localToWorldMatrix.inverse * hitPosition;
        s = ScalingFactor();
        
        // SetTransformByMatrix(selectedObject, oPrime);
    }

    private void DeselectObject()
    {
        // selectedObjectRight.transform.SetParent(selectables.transform, true); // worldPositionStays = true
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
        hC = Matrix4x4.TRS(rightHandController.transform.localPosition, rightHandController.transform.localRotation, rightHandController.transform.localScale);
        S = Matrix4x4.TRS(selectables.transform.localPosition, selectables.transform.localRotation, selectables.transform.localScale);
        CO = Matrix4x4.TRS(cameraOffset.transform.localPosition, cameraOffset.transform.localRotation, cameraOffset.transform.localScale);
        XRR = Matrix4x4.TRS(XRRig.transform.localPosition, XRRig.transform.localRotation, XRRig.transform.localScale);
    }

    private void BendRays()
    {
        v1 = (selectedObject.transform.position - rightHandController.transform.position).normalized;
        v2 = new Vector3(selectedObjectMatrix[0, 3], selectedObjectMatrix[1, 3], selectedObjectMatrix[2, 3]) - rightHandController.transform.position;
        alpha = Vector3.Angle(v1, v2);
        a = ((v2 * Mathf.Cos(alpha) * v1.magnitude)/ v2.magnitude) - v1;
        m = rightHandController.transform.position - (v1.magnitude / (2 * Mathf.Cos(Mathf.PI/2 - alpha))) * a.normalized;
    }

    void DrawQuadraticBezierCurve(Vector3 point0, Vector3 point1, Vector3 point2)
    {
        rightRayRenderer.positionCount = 200;
        float t = 0f;
        Vector3 B = new Vector3(0, 0, 0);
        for (int i = 0; i < rightRayRenderer.positionCount; i++)
        {
            B = (1 - t) * (1 - t) * point0 + 2 * (1 - t) * t * point1 + t * t * point2;
            rightRayRenderer.SetPosition(i, B);
            t += (1 / (float)rightRayRenderer.positionCount);
        }

        rightRayIntersectionSphere.transform.position = point2;
    }

    private float ScalingFactor() {
        float objDistanceToArm = Vector3.Distance(rightHit.point, rightHandController.transform.position);
        armLength = Vector3.Distance(rightHandController.transform.position, mainCamera.transform.position);
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
        float currentArmLength = Vector3.Distance(rightHandController.transform.position, mainCamera.transform.position);
        float armchange = currentArmLength - armLength;
        float fScale = s * armchange;
        Vector3 fscaleDir = fScale * rightHandController.transform.TransformDirection(Vector3.forward);
        return (pos + fscaleDir);
    }
}
