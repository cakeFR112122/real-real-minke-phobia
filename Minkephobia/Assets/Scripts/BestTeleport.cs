using System.Collections;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;

public class BestTeleport : MonoBehaviour
{
    [Header("Credits: TheCoder")]

    [Header("Gorilla Player")]
    public GameObject gorillaPlayer;

    [Header("Location")]
    public GameObject teleportLocation;

    [Header("Tag(s)")]
    public string rHand = "HandTag";
    public string lHand = "HandTag";
    [Header("Set this to whatever tag your GorillaPlayer has!")]
    public string body = "Body";

    private Collider[] objectsWithColliders;

    void Start()
    {
        objectsWithColliders = FindObjectsOfType<Collider>();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(rHand) || other.CompareTag(lHand) || other.CompareTag(body))
        {
            StartCoroutine(Teleport());
        }
    }

    IEnumerator Teleport()
    {
        gorillaPlayer.GetComponent<Rigidbody>().isKinematic = true;
        foreach (Collider collider in objectsWithColliders)
        {
            collider.enabled = false;
        }
        gorillaPlayer.transform.position = teleportLocation.transform.position;
        yield return new WaitForSeconds(1.0f);
        foreach (Collider collider in objectsWithColliders)
        {
            collider.enabled = true;
        }
        gorillaPlayer.GetComponent<Rigidbody>().isKinematic = false;
    }
}
