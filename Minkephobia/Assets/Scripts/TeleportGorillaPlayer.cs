using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportGorillaPlayer : MonoBehaviour
{
    public Transform GorillaPlayer;
    public GameObject[] ObjectsToDisable;
    public Transform TeleportLocation;
    public float WaitTime;
    public GameObject TeleportOverlay;
    public AudioSource TeleportSound;

    private LayerMask defaultLayers;
    public void Start()
    {
        defaultLayers = GorillaLocomotion.Player.Instance.locomotionEnabledLayers;
    }

    void OnTriggerEnter(Collider Other)
    {
        if (Other.gameObject.CompareTag("MainCamera"))
        {
            TeleportOverlay.SetActive(true);
            TeleportSound.Play();
            foreach (GameObject OTD in ObjectsToDisable)
            {
                OTD.SetActive(false);
            }
            StartCoroutine(TPWD());
        }
   
    }
IEnumerator TPWD()
{
    yield return new WaitForSeconds(WaitTime);
    // Store rigidbody reference
    Rigidbody playerRigidbody = GorillaPlayer.gameObject.GetComponent<Rigidbody>();

    // Disable collisions
    GorillaLocomotion.Player.Instance.locomotionEnabledLayers = default;
    GorillaLocomotion.Player.Instance.headCollider.enabled = false;
    GorillaLocomotion.Player.Instance.bodyCollider.enabled = false;

    // Set rigidbody to kinematic
    playerRigidbody.isKinematic = true;

    GorillaPlayer.position = TeleportLocation.position;
    playerRigidbody.velocity = Vector3.zero;

    yield return new WaitForSeconds(WaitTime);
    // Enable collisions again
    GorillaLocomotion.Player.Instance.locomotionEnabledLayers = defaultLayers;
    GorillaLocomotion.Player.Instance.headCollider.enabled = true;
    GorillaLocomotion.Player.Instance.bodyCollider.enabled = true;

    // Set rigidbody back to non-kinematic
    playerRigidbody.isKinematic = false;

    foreach (GameObject OTD in ObjectsToDisable)
    {
        OTD.SetActive(true);
    }
    TeleportOverlay.SetActive(false);
}

}
