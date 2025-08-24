using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Evrery body Dance now... *music*
public class KickHitBox : MonoBehaviour, IPunObservable
{
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo idk)
    {
        //Wow this is usefull cause cause cause... IPunObservable wants it not me ok?
    }

    [PunRPC]
    public void GetThisFallOutOfHere()
    {
        Application.Quit();
    }
}
