using Photon.Pun;
using UnityEngine;

public class RebuildGlassBrdige : MonoBehaviour, IPunObservable
{
    public string HandTag = "HandTag";
    public KeosGlassBridge manager;
    public PhotonView PTView;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(HandTag))
        {
            PTView.RPC(nameof(RebuildGlass), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    public void RebuildGlass()
    {
        foreach (KeosGlassBridge.GlassPiece g in manager.GlassPieces)
        {
            g.PieceOne.gameObject.SetActive(true);
            g.PieceTwo.gameObject.SetActive(true);
        }
    }

    public void OnPhotonSerializeView(PhotonStream s, PhotonMessageInfo i)
    {

    }
}
