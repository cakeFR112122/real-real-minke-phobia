using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class KeosGlassBridge : MonoBehaviour, IPunObservable
{
    [Header("Sync")]
    public PhotonView PTView;

    [Header("Main")]
    public List<GlassPiece> GlassPieces;
    public GameObject GlassBreakEffect;
    public float DespawnTime;
    public string[] BreakTags = new string[0];

    [Header("Triggers")]
    public UnityEvent OnGlassBreak;

    [Header("Helper")]

    public Transform LeftLane;
    public Transform RightLane;

    bool GotTriggerd = false;

    [System.Serializable]
    public class GlassPiece
    {
        [Header("Partner Piece")]
        public KeosGlass PieceOne;
        public KeosGlass PieceTwo;
    }

    [ContextMenu("Random")]

    private void Awake()
    {
        foreach (var piece in GlassPieces)
        {
            piece.PieceOne.DespawnTime = DespawnTime;
            piece.PieceOne.GlassBreakEffect = GlassBreakEffect;
            piece.PieceOne.BreakTags = BreakTags;
            piece.PieceOne.manager = this;

            piece.PieceTwo.DespawnTime = DespawnTime;
            piece.PieceTwo.GlassBreakEffect = GlassBreakEffect;
            piece.PieceTwo.BreakTags = BreakTags;
            piece.PieceTwo.manager = this;
        }
    }
    public void UpdateGlasses()
    {
        for (int index = 0; index < GlassPieces.Count; index++)
        {
            int val = Random.Range(0, 2);
            PTView.RPC(nameof(UpdateGlass), RpcTarget.AllBuffered, index, val);
        }
    }

    private void Update()
    {
        if (!GotTriggerd && PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            print("yep");
            UpdateGlasses();
            PTView.RPC(nameof(GotTriggerdSync), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    public void GotTriggerdSync()
    {
        GotTriggerd = true;
    }

    [PunRPC]
    private void UpdateGlass(int index, int val)
    {
        if (index >= 0 && index < GlassPieces.Count)
        {
            GlassPieces[index].PieceOne.state = val == 0 ? KeosGlass.GlassState.Breakable : KeosGlass.GlassState.Stable;
            GlassPieces[index].PieceTwo.state = val == 0 ? KeosGlass.GlassState.Stable : KeosGlass.GlassState.Breakable;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

    }

    public void SetUpHelper()
    {
        List<GlassPiece> setUp = new List<GlassPiece>();

        for (int i = 0; i < LeftLane.childCount; i++)
        {
            LeftLane.GetChild(i).TryGetComponent<KeosGlass>(out KeosGlass gl);
            if (setUp.Count <= i)
            {
                setUp.Add(new GlassPiece());
            }
            setUp[i].PieceOne = gl;
        }

        for (int i = 0; i < RightLane.childCount; i++)
        {
            RightLane.GetChild(i).TryGetComponent<KeosGlass>(out KeosGlass gl);
            if (setUp.Count <= i)
            {
                setUp.Add(new GlassPiece());
            }
            setUp[i].PieceTwo = gl;
        }

        GlassPieces = setUp;
    }

}

#if UNITY_EDITOR

[CustomEditor(typeof(KeosGlassBridge))]
public class KeosGlassBridgeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        KeosGlassBridge script = (KeosGlassBridge)target;
        if (GUILayout.Button("Set Up"))
        {
            script.SetUpHelper();
        }
    }
}

#endif