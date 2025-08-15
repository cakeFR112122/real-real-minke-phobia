using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GorillaTeleporter : MonoBehaviour
{
    public GameObject player;
    public Transform teleportplace;
    public Rigidbody rb;
    public string playerTag = "Player";


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag(playerTag))
        {
            StartCoroutine(StartTeleport());
        }
    }

    IEnumerator StartTeleport()
    {
        rb.isKinematic = true;
        foreach(Collider i in UnityEngine.Resources.FindObjectsOfTypeAll<Collider>())
        {
            i.enabled = false;
        }
        player.transform.position = teleportplace.position;
        yield return new WaitForSeconds(2.25f);
        rb.isKinematic = false;
        foreach (Collider i in UnityEngine.Resources.FindObjectsOfTypeAll<Collider>())
        {
            i.enabled = true;
        }
    }
}
