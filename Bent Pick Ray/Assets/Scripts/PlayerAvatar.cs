using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

using Photon.Pun;

public class PlayerAvatar : MonoBehaviourPun
{

    public GameObject leftHand;
    public GameObject rightHand;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(this.photonView.IsMine){
            rightHand.SetActive(false);
            leftHand.SetActive(false);
            MapPosition(leftHand, XRNode.LeftHand);
            MapPosition(rightHand, XRNode.RightHand);
        }
    }

    void MapPosition(GameObject target, XRNode node){

        InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
        InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);

        target.transform.position = position;
        target.transform.rotation = rotation;
    }
}
