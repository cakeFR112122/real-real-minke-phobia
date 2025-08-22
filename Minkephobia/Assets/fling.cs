using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class fling : MonoBehaviour
{
    [Header("ignore this. :)")]
    public float amountup;
    [Header("dont ignore these!!1")]
    public float amountforce;
    public PhotonView ptView;
    public GameObject FlingCollider;


    void OnTriggerEnter(Collider other)
    {
        if (ptView.IsMine)
        {
            FlingCollider.SetActive(false);
        }

        Vector3 forceDirection = (other.transform.position - transform.position).normalized;

        Rigidbody[] allRigidbodies = FindObjectsOfType<Rigidbody>();

        foreach (Rigidbody rb in allRigidbodies)
        {
            rb.AddForce(Vector3.up * amountup);
            rb.AddForce(forceDirection * amountforce);
        }

    }
}
