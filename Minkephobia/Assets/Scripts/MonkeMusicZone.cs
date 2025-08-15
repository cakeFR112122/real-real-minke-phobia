using UnityEngine;

public class MonkeMusicZone : MonoBehaviour
{
    public AudioClip[] musicClips;
    public AudioSource audioSource;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("HandTag"))
        {
            audioSource.clip = musicClips[Random.Range(0, musicClips.Length)];
            audioSource.Play();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("HandTag"))
        {
            audioSource.Stop();
        }
    }
}
