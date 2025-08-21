using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class KeosGlass : MonoBehaviour, IPunObservable
{
    [HideInInspector]
    public string[] BreakTags = new string[0];

    [HideInInspector]
    public GameObject GlassBreakEffect;

    [HideInInspector]
    public float DespawnTime;

    [HideInInspector]
    public KeosGlassBridge manager;

    private PhotonView PTView;

    [Header("NONO SPOT")]

    public GlassState state;
    private void Awake()
    {
        PTView = GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (state == GlassState.Breakable)
        {
            foreach (string s in BreakTags)
            {
                if (other.CompareTag(s))
                {
                    PTView.RPC(nameof(SpawnGlass), RpcTarget.AllBuffered);
                }
            }
        }
    }

    [PunRPC]
    public void SpawnGlass()
    {
        if (GlassBreakEffect)
        {
            Destroy(Instantiate(GlassBreakEffect, transform.position, Quaternion.identity), DespawnTime);
        }

        if (manager.OnGlassBreak != null)
        {
            manager.OnGlassBreak.Invoke();
        }

        gameObject.SetActive(false);
    }

    public enum GlassState
    {
        Stable,
        Breakable
    }

    public void OnPhotonSerializeView(PhotonStream s, PhotonMessageInfo i)
    {

    }
}
