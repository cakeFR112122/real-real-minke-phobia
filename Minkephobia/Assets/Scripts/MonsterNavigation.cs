using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public class MonsterNavigation : MonoBehaviour
{
    public float DetectionRange = 5f;
    public float MonsterSpeedWander = 5f;
    public float MonsterSpeedChase = 7.5f;
    public NavMeshAgent agent;
    public Transform[] points;
    public string tagString = "Player";

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = MonsterSpeedWander;
        Wander();
    }

    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            agent.enabled = true;
            GameObject[] players = GameObject.FindGameObjectsWithTag(tagString);

            GameObject target = null;

            // Set the target to the closest player
            if (players.Length > 0)
            {
                float minDistance = float.MaxValue;
                foreach (GameObject player in players)
                {
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    if (distance < DetectionRange && distance < minDistance)
                    {
                        minDistance = distance;
                        target = player;
                    }
                }


                if (target != null)
                {
                    agent.speed = MonsterSpeedChase;
                    Chase(target.transform);
                }

                else if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    agent.speed = MonsterSpeedWander;
                    Wander();
                }
            }

            // If we have a target, set the NavMeshAgent's destination to the target's position
            if (target != null)
            {
                agent.destination = target.transform.position;
            }
        }
        else
        {
            agent.enabled = false;

        }
    }

    void Chase(Transform target)
    {
        agent.destination = target.position;
    }

    void Wander()
    {
        if (points.Length == 0)
            return;

        int destPoint = Random.Range(0, points.Length);
        agent.destination = points[destPoint].position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);
    }
}