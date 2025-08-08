using System.Collections;
using UnityEngine;

public class TeleportOnPlayerCollision : MonoBehaviour
{
    [Header("Teleport Settings")]
    public GameObject playerObject; // Object with Player script
    public GameObject teleportDestination; // Where to teleport the player
    public float teleportDelay = 0.5f;

    [Header("Map/Scene Control")]
    public GameObject[] objectsToDisable; // Objects to disable during teleport
    public GameObject temporaryEnableObject; // Object to enable temporarily
    public float enableDuration = 1f;

    private bool isTeleporting = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return;

        // Check if the collider belongs to the player object (or one of its children)
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

        // Disable maps or other objects
        foreach (GameObject go in objectsToDisable)
        {
            if (go != null) go.SetActive(false);
        }

        // Enable temporary object
        if (temporaryEnableObject != null)
            temporaryEnableObject.SetActive(true);

        // Wait for teleport delay
        yield return new WaitForSeconds(teleportDelay);

        // Teleport player
        if (playerObject != null && teleportDestination != null)
        {
            playerObject.transform.position = teleportDestination.transform.position;
            playerObject.transform.rotation = teleportDestination.transform.rotation;
        }

        // Wait for duration
        yield return new WaitForSeconds(enableDuration);

        // Re-enable maps or objects
        foreach (GameObject go in objectsToDisable)
        {
            if (go != null) go.SetActive(true);
        }

        // Disable temporary object
        if (temporaryEnableObject != null)
            temporaryEnableObject.SetActive(false);

        isTeleporting = false;
    }
}
