using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

using Photon.Pun;
using Photon.Realtime;

namespace Omar.Launcher
{
    public class PlayerAnimatorManager : MonoBehaviourPun
    {

        #region Private Fields
        [SerializeField]
        private float directionDampTime = 0.25f;
        #endregion

        #region MonoBehaviour Callbacks

        private Animator animator;

        // Use this for initialization
        void Start()
        {
            animator = GetComponent<Animator>();
            if (!animator)
            {
                Debug.LogError("PlayerAnimatorManager is Missing Animator Component", this);
            }
    
        }


        // Update is called once per frame
        void Update()
        {
            if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
            {
                return;
            }
            
            if (!animator)
            {
                return;
            }
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (v < 0)
            {
                v = 0;
            }
            animator.SetFloat("Direction", h, directionDampTime, Time.deltaTime);
            animator.SetFloat("Speed", h * h + v * v);
        }


        #endregion
    }
}
