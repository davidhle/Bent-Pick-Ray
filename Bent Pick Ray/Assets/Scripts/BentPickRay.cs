using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class BentPickRay : MonoBehaviour
{
    private GameObject rightHandController;
    private GameObject leftHandController;
    private GameObject selectables;
    private GameObject scene = null;
    private XRController rightXRController;
    private XRController leftXRController;
    public GameObject selectedObject = null;
    public Matrix4x4 oPrime;

    private bool gripButtonLFRight = false;
    private bool gripButtonLFLeft = false;
    private Matrix4x4 oPrimeRight, oPrimeLeft;
    private GameObject XRRig;
    private GameObject cameraOffset;
    private Matrix4x4 O, RHC,LHC, XRR, S, CO, worldTransform;
    private Vector3 t1,t2,translation;

    private User1 user1;
    private User2 user2;


    // Start is called before the first frame update
    void Awake()
    {

        rightHandController = GameObject.Find("RightHand Controller");
        leftHandController = GameObject.Find("LeftHand Controller");
        scene = GameObject.Find("Scene");
        selectables = GameObject.Find("Selectables");
        cameraOffset = GameObject.Find("Camera Offset");
        XRRig = GameObject.Find("XR Rig");

        user1 = (User1) this.GetComponent(typeof(User1));
        user2 = (User2) this.GetComponent(typeof(User2));

        MeshCollider[] colliders = FindObjectsOfType<MeshCollider>();
        for(int i=0; i<colliders.Length; i++){
          GameObject.Destroy(colliders[i]);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if(user1.selectedObject == user2.selectedObject && user1.selectedObject != null){
            selectedObject = user1.selectedObject;
            UpdatePosition();
        }
        else{
            selectedObject = null;
            if(user2.selectedObject != null){
                user2.UpdatePosition();
            }

            if(user1.selectedObject != null){
                user1.UpdatePosition();
            }
        }

    }

    private void UpdatePosition(){
        AssignTransformationMatrices();

        Matrix4x4 m1 = S.inverse * XRR * CO * RHC * user1.oPrime;
        Matrix4x4 m2 = S.inverse * XRR * CO * LHC * user2.oPrime;

        // Debug.Log(user1.selectedobjetMatrix);
        // Debug.Log(m1);

        t1 = new Vector3(m1[0, 3], m1[1, 3], m1[2, 3]);
        t2 = new Vector3(m2[0, 3], m2[1, 3], m2[2, 3]);
        translation  = (t1 + t2)/2;
        
        Quaternion r1 = m1.rotation;
        Quaternion r2 = m2.rotation;
        
        Quaternion total = r1 * r2;
        //Quaternion total = Quaternion.Slerp(r1, r2, 1/2f);
        //Debug.Log("r1: " + r1);
        //Debug.Log("r2: " + r2);
        //Debug.Log("total: " + total);
        //Debug.Log("r1 * r2: " + r1 * r2);

        selectedObject.transform.localPosition = translation;
        selectedObject.transform.localRotation = total;
        
        BendRays();
    }

    private void AssignTransformationMatrices()
    {
        O = Matrix4x4.TRS(selectedObject.transform.localPosition, selectedObject.transform.localRotation, selectedObject.transform.localScale);
        RHC = Matrix4x4.TRS(rightHandController.transform.localPosition, rightHandController.transform.localRotation, rightHandController.transform.localScale);
        LHC = Matrix4x4.TRS(leftHandController.transform.localPosition, leftHandController.transform.localRotation, leftHandController.transform.localScale);
        S = Matrix4x4.TRS(selectables.transform.localPosition, selectables.transform.localRotation, selectables.transform.localScale);
        CO = Matrix4x4.TRS(cameraOffset.transform.localPosition, cameraOffset.transform.localRotation, cameraOffset.transform.localScale);
        XRR = Matrix4x4.TRS(XRRig.transform.localPosition, XRRig.transform.localRotation, XRRig.transform.localScale);
    }

    private void BendRays(){
        Vector3 v1 = translation - rightHandController.transform.position;
        Vector3 v2 = t1 - rightHandController.transform.position;
        float cosAlpha = Vector3.Dot(v1,v2)/ Vector3.magnitude(v1) * Vector3.magnitude(v2);
        float alpha = acos(cosAlpha);

        Vector3 a = (v2 * cosAlpha * Vector3.magnitude(v1))/ Vector3.magnitude(v2) - v1;

        Vector3 m = rightHandController.transform.position - ((Vector3.magnitude(v1)/2*cosAlpha(90-alpha)) * (a/Vector3.magnitude(a)));
    }
    
}
