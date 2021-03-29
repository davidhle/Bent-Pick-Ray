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


    // Start is called before the first frame update
    void Start()
    {
        rightHandController = GameObject.Find("RightHand Controller");
        scene = GameObject.Find("Scene");
        selectables = GameObject.Find("Selectables");
        cameraOffset = GameObject.Find("Camera Offset");
        XRRig = GameObject.Find("XR Rig");
        bending = false;

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
        possitionDiff = new Vector3(0,0,0);
        rotationDiff = new Vector3(0,0,0);
        lastRotation = new Vector3(0,0,0);
        hitPosition = Matrix4x4.TRS(rightHit.point, Quaternion.Euler(new Vector3(0, 0, 0)), new Vector3(0, 0, 0));
        // selectedObjectRight.transform.SetParent(rightHandController.transform, false); // worldPositionStays = true

        AssignTransformationMatrices();
        oPrime = hC.inverse * CO.inverse * XRR.inverse * S * O;
        hitPositionLocal = selectedObject.transform.localToWorldMatrix.inverse * hitPosition;
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
        Vector3 translation = new Vector3(selectedObjectMatrix[0, 3], selectedObjectMatrix[1, 3], selectedObjectMatrix[2, 3]);
        
        if(possitionDiff.x > 0){
            if(translation.x - selectedObject.transform.localPosition.x > possitionDiff.x){
                translation.x = translation.x - possitionDiff.x;
            }
            else{
                possitionDiff.x = translation.x - selectedObject.transform.localPosition.x;
                translation.x = selectedObject.transform.localPosition.x;
            }
        }
        if(possitionDiff.x < 0){
            if(translation.x - selectedObject.transform.localPosition.x < possitionDiff.x){
                translation.x = translation.x - possitionDiff.x;
            }
            else{
                possitionDiff.x = translation.x - selectedObject.transform.localPosition.x;
                translation.x = selectedObject.transform.localPosition.x;
            }
        }
        
        if(possitionDiff.y > 0){
            if(translation.y - selectedObject.transform.localPosition.y > possitionDiff.y){
                translation.y = translation.y - possitionDiff.y;
            }
            else{
                possitionDiff.y = translation.y - selectedObject.transform.localPosition.y;
                translation.y = selectedObject.transform.localPosition.y;
            }
        }
        else{
            if(translation.y - selectedObject.transform.localPosition.y < possitionDiff.y){
                translation.y = translation.y - possitionDiff.y;
            }
            else{
                possitionDiff.y = translation.y - selectedObject.transform.localPosition.y;
                translation.y = selectedObject.transform.localPosition.y;
            }
        }
        
        if(possitionDiff.z > 0){
            if(translation.z - selectedObject.transform.localPosition.z > possitionDiff.z){
                translation.z = translation.z - possitionDiff.z;
            }
            else{
                possitionDiff.z = translation.z - selectedObject.transform.localPosition.z;
                translation.z = selectedObject.transform.localPosition.z;
            }
        }
        else{
            if(translation.z - selectedObject.transform.localPosition.z < possitionDiff.z){
                translation.z = translation.z - possitionDiff.z;
            }
            else{
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
}
