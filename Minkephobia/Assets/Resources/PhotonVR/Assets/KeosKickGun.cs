using easyInputs;
using Photon.Pun;
using UnityEngine;
[RequireComponent(typeof(PhotonView))]
public class KeosKickGun : MonoBehaviour
{
    public PhotonView PTView;
    public EasyHand triggerhand;
    public Transform tip;
    public float range;
    [Header("Optional")]
    public LineRenderer lineRenderer;

    private void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(tip.position, tip.forward, out hit, range))
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, tip.position);
                lineRenderer.SetPosition(1, hit.point);
            }
            if (EasyInputs.GetTriggerButtonDown(triggerhand) || Input.GetMouseButtonDown(0))
            {
                KickHitBox idkwhattocallthisvariablelol = hit.collider.GetComponent<KickHitBox>();
                if (idkwhattocallthisvariablelol != null )
                {
                    PTView.RPC(nameof(idkwhattocallthisvariablelol.GetThisFallOutOfHere), RpcTarget.AllBuffered);
                }
            }
        }
        else
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
    }
}
