using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class PastebinTextLoader : MonoBehaviour
{
    [Header("Assign your TextMeshPro (3D) component")]
    public TextMeshPro textDisplay;

    [Header("Paste the RAW Pastebin link here")]
    public string pastebinRawUrl;

    void Start()
    {
        if (textDisplay != null && !string.IsNullOrEmpty(pastebinRawUrl))
        {
            StartCoroutine(LoadTextFromPastebin());
        }
        else
        {
            Debug.LogWarning("TextMeshPro or Pastebin URL not assigned!");
        }
    }

    IEnumerator LoadTextFromPastebin()
    {
        UnityWebRequest request = UnityWebRequest.Get(pastebinRawUrl);
        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            Debug.LogError("Failed to load text: " + request.error);
        }
        else
        {
            textDisplay.text = request.downloadHandler.text;
        }
    }
}
