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
    public Matrix4x4 selectedobjetMatrix;
    private GameObject mainCamera;

    private bool gripButtonLF = false;
    public Matrix4x4 oPrime;
    private GameObject XRRig;
    private GameObject cameraOffset;
    private Matrix4x4 O, hC, XRR, S, CO, worldTransform;
    private Vector3 lastControllerPosition;

    // Start is called before the first frame update
    void Start()
    {
        rightHandController = GameObject.Find("RightHand Controller");
        scene = GameObject.Find("Scene");
        selectables = GameObject.Find("Selectables");
        cameraOffset = GameObject.Find("Camera Offset");
        XRRig = GameObject.Find("XR Rig");
        mainCamera = GameObject.Find("Main Camera");
        lastControllerPosition = new Vector3(0,0,0);

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
        // Debug.Log(lastControllerPosition == rightHandController.transform.localPosition);
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

        Dragging();
        lastControllerPosition = rightHandController.transform.position;
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
        // selectedObjectRight.transform.SetParent(rightHandController.transform, false); // worldPositionStays = true

        AssignTransformationMatrices();
        oPrime = hC.inverse * CO.inverse * XRR.inverse * S * O;
        // SetTransformByMatrix(selectedObject, oPrime);
    }

    private void DeselectObject()
    {
        // selectedObjectRight.transform.SetParent(selectables.transform, true); // worldPositionStays = true
        selectedObject = null;

    }

    public void UpdatePosition()
    {
        AssignTransformationMatrices();
        selectedobjetMatrix = S.inverse * XRR * CO * hC * oPrime;
        Debug.Log("s = " + ScalingFactor());
        float s = ScalingFactor() * Time.deltaTime;
        Debug.Log("after multiplying by Time.deltaTime: " + s);
        Vector3 vProj = new Vector3(selectedobjetMatrix[0, 3], selectedobjetMatrix[1, 3], selectedobjetMatrix[2, 3]);
        selectedObject.transform.localPosition = s * vProj + lastControllerPosition;
        selectedObject.transform.localRotation = selectedobjetMatrix.rotation;
    }

    private void AssignTransformationMatrices()
    {
        O = Matrix4x4.TRS(selectedObject.transform.localPosition, selectedObject.transform.localRotation, selectedObject.transform.localScale);
        hC = Matrix4x4.TRS(rightHandController.transform.localPosition, rightHandController.transform.localRotation, rightHandController.transform.localScale);
        S = Matrix4x4.TRS(selectables.transform.localPosition, selectables.transform.localRotation, selectables.transform.localScale);
        CO = Matrix4x4.TRS(cameraOffset.transform.localPosition, cameraOffset.transform.localRotation, cameraOffset.transform.localScale);
        XRR = Matrix4x4.TRS(XRRig.transform.localPosition, XRRig.transform.localRotation, XRRig.transform.localScale);
    }

    private float ScalingFactor() {
        float objDistanceToArm = Vector3.Distance(selectedObject.transform.position, rightHandController.transform.position);
        float armLength = Vector3.Distance(rightHandController.transform.position, mainCamera.transform.position);
        if (objDistanceToArm / armLength < 1) {
          return 1.0f;
        } else {
          return objDistanceToArm / armLength;
        }
    }
}
