using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

public class XRTimedTriggerWithTeleport_NoFreeze : MonoBehaviour
{
    [Header("Only this XR rig object can trigger")]
    public GameObject allowedColliderObject;

    [Header("Objects to disable temporarily")]
    public List<GameObject> objectsToDisable;
    public float objectsDisableDuration = 5f;

    [Header("Object to enable temporarily")]
    public GameObject objectToEnable;
    public float objectEnableDuration = 5f;

    [Header("Audio to play temporarily")]
    public AudioSource audioToPlay;
    public float audioPlayDuration = 3f;

    [Header("Teleport player to this position on trigger")]
    public Transform teleportTarget;

    [Header("Only trigger once?")]
    public bool oneTimeUse = true;

    [Header("Reference to XR Origin")]
    public XROrigin xrOrigin;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered by: " + other.name);

        if (hasTriggered && oneTimeUse)
        {
            Debug.Log("Already triggered. Ignoring.");
            return;
        }

        if (other.gameObject == allowedColliderObject)
        {
            Debug.Log("Correct object detected. Triggering event.");

            hasTriggered = true;

            if (objectsToDisable.Count > 0)
                StartCoroutine(HandleObjectDisabling());

            if (objectToEnable != null)
                StartCoroutine(HandleObjectEnabling());

            if (audioToPlay != null)
                StartCoroutine(HandleAudio());

            if (teleportTarget != null && xrOrigin != null)
                StartCoroutine(HandleTeleport());
        }
    }

    private IEnumerator HandleObjectDisabling()
    {
        foreach (var obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        yield return new WaitForSeconds(objectsDisableDuration);

        foreach (var obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    private IEnumerator HandleObjectEnabling()
    {
        objectToEnable.SetActive(true);
        yield return new WaitForSeconds(objectEnableDuration);
        objectToEnable.SetActive(false);
    }

    private IEnumerator HandleAudio()
    {
        audioToPlay.Play();
        yield return new WaitForSeconds(audioPlayDuration);
        audioToPlay.Stop();
    }

    private IEnumerator HandleTeleport()
    {
        Debug.Log("Teleporting player...");

        xrOrigin.MoveCameraToWorldLocation(teleportTarget.position);

        yield return null; // one frame delay (optional)

        Debug.Log("Teleport complete.");
    }
}
