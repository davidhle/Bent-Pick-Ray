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
            // rightHand.SetActive(false);
            // leftHand.SetActive(false);
            MapPosition(leftHand, GameObject.Find("LeftHand Controller"));
            MapPosition(rightHand, GameObject.Find("RightHand Controller"));
        }
    }

    void MapPosition(GameObject target, GameObject XRnode){

        // InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
        // InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);

        target.transform.position = XRnode.transform.position;
        target.transform.rotation = XRnode.transform.rotation;
    }

    // public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    // {
    //     if (stream.IsWriting)
    //     {
    //         stream.SendNext(rightHand.transform.position);
    //         stream.SendNext(leftHand.transform.position);
    //     }
    //     else
    //     {
    //         rightHand.transform.position = (Vector3)stream.ReceiveNext();

    //         leftHand.transform.position = (Vector3)stream.ReceiveNext();

    //     }
    // }
}
