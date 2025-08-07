using System.Collections;
using UnityEngine;

public class SimpleTeleportOnPlayerCollision : MonoBehaviour
{
    [Header("Teleport Settings")]
    public GameObject playerObject;               // Your XR rig or player root
    public GameObject teleportDestination;        // Where to teleport to
    public float teleportDelay = 0.5f;            // Delay before teleport happens

    [Header("Objects to Disable During Teleport")]
    public GameObject[] objectsToDisable;         // Maps, zones, UI, etc.
    public float reenableAfter = 1f;              // Delay before re-enabling them

    private bool isTeleporting = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return;

        if (IsPlayerObject(other.gameObject))
        {
            StartCoroutine(TeleportPlayerCoroutine());
        }
    }

    private bool IsPlayerObject(GameObject obj)
    {
        return obj == playerObject || obj.transform.IsChildOf(playerObject.transform);
    }

    private IEnumerator TeleportPlayerCoroutine()
    {
        isTeleporting = true;

        // Disable any assigned objects immediately
        foreach (GameObject go in objectsToDisable)
        {
            if (go != null) go.SetActive(false);
        }

        // Wait for teleport delay
        yield return new WaitForSeconds(teleportDelay);

        // Teleport the player
        if (playerObject != null && teleportDestination != null)
        {
            playerObject.transform.position = teleportDestination.transform.position;
            playerObject.transform.rotation = teleportDestination.transform.rotation;
        }

        // Wait before re-enabling objects
        if (reenableAfter > 0f)
            yield return new WaitForSeconds(reenableAfter);

        foreach (GameObject go in objectsToDisable)
        {
            if (go != null) go.SetActive(true);
        }

        isTeleporting = false;
    }
}
