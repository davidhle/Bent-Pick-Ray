using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

using Photon.Pun;
using Photon.Realtime;

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
    private bool multiUsers;

    private User1 user1;
    private User2 user2;


    // Start is called before the first frame update
    void Awake()
    {
        multiUsers = false;
        rightHandController = GameObject.Find("RightHand Controller");
        leftHandController = GameObject.Find("LeftHand Controller");
        scene = GameObject.Find("Scene");
        selectables = GameObject.Find("Selectables");
        cameraOffset = GameObject.Find("Camera Offset");
        XRRig = this.gameObject;

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
            if(!multiUsers){
                multiUsers = true;
                oPrime = Matrix4x4.TRS(selectedObject.transform.localPosition, selectedObject.transform.localRotation, selectedObject.transform.localScale);
            }
            UpdatePosition();
        }
        else{
            selectedObject = null;
            multiUsers = false;
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

        t1 = user1.GrabScaling(new Vector3(m1[0, 3], m1[1, 3], m1[2, 3]));
        t2 = user2.GrabScaling(new Vector3(m2[0, 3], m2[1, 3], m2[2, 3]));
        Vector3 originalPosition = new Vector3(oPrime[0, 3], oPrime[1, 3], oPrime[2, 3]);
        float l1 = Vector3.Distance(t1, originalPosition);
        float l2 = Vector3.Distance(t2, originalPosition);

        float w1 = 0.5f;
        float w2 = 0.5f;

        // if (l1 > l2) {
        //     // w1 = 0.25f * (((l1 - l2) / l2) + 0.5f);
        // }
        // else {
        //     // w1 = 0.25f * (((l2 - l1) / l1) + 0.5f);
        // }
        w1 = (l1 / (l1 + l2));
        w2 = 1f - w1;
        Debug.Log("w1: " + w1);
        Debug.Log("w2: " + w2);

        Vector3 t1Scaled = (t1-originalPosition)*w1;
        Vector3 t2Scaled = (t2-originalPosition)*w2;

        translation  = t1Scaled + t2Scaled;
        translation = originalPosition + translation;

        user1.possitionDiff = t1 - translation;
        user2.possitionDiff = t2 - translation;

        Quaternion r1 = m1.rotation;
        Quaternion r2 = m2.rotation;
        Quaternion total = Quaternion.Slerp(r1, r2, 1/2f);

        user1.rotationDiff = total.eulerAngles;
        user1.lastRotation = r1.eulerAngles;

        user2.rotationDiff = total.eulerAngles;
        user2.lastRotation = r2.eulerAngles;

        selectedObject.transform.localPosition = translation;
        selectedObject.transform.localRotation = total;
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

}
