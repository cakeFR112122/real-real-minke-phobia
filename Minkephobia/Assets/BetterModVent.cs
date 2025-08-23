using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BetterModVent : MonoBehaviour
{
    public float radius;
    public Transform gorillaPlayer;
    public Animator animator;
    private void Update()
    {
        if (Vector3.Distance(gorillaPlayer.position, transform.position) < radius)
        {
            animator.SetBool("IOPV", true);
        }
        else
        {
            animator.SetBool("IOPV", false);
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
