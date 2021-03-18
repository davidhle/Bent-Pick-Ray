using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class BentPickRay : MonoBehaviour
{
    private GameObject rightHandController;
    private GameObject leftHandController;
    private LineRenderer rightRayRenderer;
    private LineRenderer leftRayRenderer;
    private GameObject rightRayIntersectionSphere;
    private GameObject leftRayIntersectionSphere;
    private GameObject selectables;
    private RaycastHit rightHit;
    private RaycastHit leftHit;
    public LayerMask myLayerMask;
    private GameObject selectedObjectRight = null;
    private GameObject selectedObjectLeft = null;
    private GameObject scene = null;
    private XRController rightXRController;
    private XRController leftXRController;

    private bool gripButtonLFRight = false;
    private bool gripButtonLFLeft = false;
    private Matrix4x4 oPrimeRight, oPrimeLeft;
    private GameObject XRRig;
    private GameObject cameraOffset;
    private Matrix4x4 O, RHC, XRR, S, CO, worldTransform;

    // Start is called before the first frame update
    void Awake()
    {
        rightHandController = GameObject.Find("RightHand Controller");
        leftHandController = GameObject.Find("LeftHand Controller");
        scene = GameObject.Find("Scene");
        selectables = GameObject.Find("Selectables");

        if (rightHandController != null) // guard
        {
            rightXRController = rightHandController.GetComponent<XRController>();

            //rightRayRenderer = gameObject.AddComponent<LineRenderer>();

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

        MeshCollider[] colliders = FindObjectsOfType<MeshCollider>();
        for(int i=0; i<colliders.Length; i++){
          GameObject.Destroy(colliders[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
      if (Physics.Raycast(rightHandController.transform.position, rightHandController.transform.TransformDirection(Vector3.forward), out rightHit))
      {
          //Debug.Log("Did Hit");
          Debug.Log(rightHit.collider.gameObject);
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


      if (Physics.Raycast(leftHandController.transform.position, leftHandController.transform.TransformDirection(Vector3.forward), out leftHit))
      {
          //Debug.Log("Did Hit");
          Debug.Log(leftHit.collider.gameObject);
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



      if(selectedObjectRight == selectedObjectLeft){
        DraggingMerge();
      }
      else
      {
        DraggingRight();
        DraggingLeft();
      }

    }

    private void DraggingRight()
    {
        // mapping: grip button (middle finger)
        bool gripButtonRight = false;
        rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripButtonRight);
        //Debug.Log("middle finger rocker: " + gripButton);

        if (gripButtonRight != gripButtonLFRight) // state changed
        {
            if (gripButtonRight) // up (false->true)
            {
                if (rightHit.collider != null && selectedObjectRight == null && rightHit.collider.gameObject.transform.parent.gameObject == selectables)
                {
                    SelectObjectRight(rightHit.collider.gameObject);
                }
            }
            else // down (true->false)
            {
                if (selectedObjectRight != null)
                {
                    DeselectObjectRight();
                }
            }
        }
        gripButtonLFRight = gripButtonRight;
    }

    private void DraggingLeft()
    {
        // mapping: grip button (middle finger)
        bool gripButtonLeft = false;
        leftXRController.inputDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripButtonLeft);
        //Debug.Log("middle finger rocker: " + gripButton);

        if (gripButtonLeft != gripButtonLFLeft) // state changed
        {
            if (gripButtonLeft) // up (false->true)
            {
                if (leftHit.collider != null && selectedObjectLeft == null && leftHit.collider.gameObject.transform.parent.gameObject == selectables)
                {
                    SelectObjectLeft(leftHit.collider.gameObject);
                }
            }
            else // down (true->false)
            {
                if (selectedObjectLeft != null)
                {
                    DeselectObjectLeft();
                }
            }
        }
        gripButtonLFLeft = gripButtonLeft;
    }

    private void SelectObjectRight(GameObject go)
    {
        selectedObjectRight = go;
        // selectedObjectRight.transform.SetParent(rightHandController.transform, false); // worldPositionStays = true

        AssignTransformationMatrices();
        oPrimeRight = RHC.inverse * CO.inverse * XRR.inverse * S * O;
        // SetTransformByMatrix(selectedObject, oPrime);
    }

    private void SelectObjectLeft(GameObject go)
    {
        selectedObjectLeft = go;
        // selectedObjectLeft.transform.SetParent(leftHandController.transform, false); // worldPositionStays = true

        AssignTransformationMatrices();
        oPrimeLeft = LHC.inverse * CO.inverse * XRR.inverse * S * O;
        // SetTransformByMatrix(selectedObject, oPrime);
    }

    private void DeselectObjectRight()
    {
        // selectedObjectRight.transform.SetParent(selectables.transform, true); // worldPositionStays = true
        selectedObjectRight = null;

    }

    private void DeselectObjectLeft()
    {
        // selectedObjectLeft.transform.SetParent(selectables.transform, true); // worldPositionStays = true
        selectedObjectLeft = null;

    }

    private void AssignTransformationMatrices() {
        O = Matrix4x4.TRS(selectedObject.transform.localPosition, selectedObject.transform.localRotation, selectedObject.transform.localScale);
        RHC = Matrix4x4.TRS(rightHandController.transform.localPosition, rightHandController.transform.localRotation, rightHandController.transform.localScale);
        LHC = Matrix4x4.TRS(leftHandController.transform.localPosition, leftHandController.transform.localRotation, leftHandController.transform.localScale);
        S = Matrix4x4.TRS(selectables.transform.localPosition, selectables.transform.localRotation, selectables.transform.localScale);
        CO = Matrix4x4.TRS(cameraOffset.transform.localPosition, cameraOffset.transform.localRotation, cameraOffset.transform.localScale);
        XRR = Matrix4x4.TRS(XRRig.transform.localPosition, XRRig.transform.localRotation, XRRig.transform.localScale);
    }
}
