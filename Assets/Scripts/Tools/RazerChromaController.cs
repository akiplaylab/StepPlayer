using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public sealed class RazerChromaController : MonoBehaviour
{
    [SerializeField] string appTitle = "DanceDanceRevolution";
    [SerializeField] string appDescription = "Light up Razer Chroma devices on judgements.";
    [SerializeField] string authorName = "DanceDanceRevolution";
    [SerializeField] string authorContact = "";
    [SerializeField] string[] devices = { "keyboard", "mouse", "headset", "mousepad", "keypad", "chromalink" };

    string sessionUri;
    Coroutine registerRoutine;

    void OnEnable()
    {
        if (registerRoutine == null)
            registerRoutine = StartCoroutine(Register());
    }

    void OnDisable()
    {
        if (registerRoutine != null)
            StopCoroutine(registerRoutine);
        registerRoutine = null;

        if (!string.IsNullOrEmpty(sessionUri))
            StartCoroutine(Unregister());
    }

    public void TriggerJudgement(Judgement judgement, Color color)
    {
        if (judgement != Judgement.Perfect && judgement != Judgement.Great)
            return;

        if (string.IsNullOrEmpty(sessionUri))
        {
            Debug.LogWarning("Razer Chroma session is not ready. Unable to light devices.");
            return;
        }

        StartCoroutine(ApplyStaticColor(color));
    }

    IEnumerator Register()
    {
        var payload = new RegisterRequest
        {
            title = appTitle,
            description = appDescription,
            author = new Author { name = authorName, contact = authorContact },
            device_supported = devices,
            category = "application",
        };

        var json = JsonUtility.ToJson(payload);
        using var request = BuildRequest("http://localhost:54235/razer/chromasdk", json, UnityWebRequest.kHttpVerbPOST);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Razer Chroma registration failed: {request.error}");
            yield break;
        }

        var response = JsonUtility.FromJson<RegisterResponse>(request.downloadHandler.text);
        sessionUri = response?.uri;
        if (string.IsNullOrEmpty(sessionUri))
            Debug.LogWarning("Razer Chroma registration returned an empty session URI.");
    }

    IEnumerator Unregister()
    {
        using var request = new UnityWebRequest(sessionUri, UnityWebRequest.kHttpVerbDELETE);
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogWarning($"Razer Chroma unregistration failed: {request.error}");

        sessionUri = null;
    }

    IEnumerator ApplyStaticColor(Color color)
    {
        int packedColor = ToBgrInt(color);

        foreach (var device in devices)
        {
            var payload = new EffectRequest
            {
                effect = "CHROMA_STATIC",
                param = new ColorParam { color = packedColor },
            };

            var json = JsonUtility.ToJson(payload);
            using var request = BuildRequest($"{sessionUri}/{device}", json, UnityWebRequest.kHttpVerbPOST);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"Razer Chroma request for {device} failed: {request.error}");
        }
    }

    static UnityWebRequest BuildRequest(string url, string json, string method)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var request = new UnityWebRequest(url, method)
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer(),
        };
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    static int ToBgrInt(Color color)
    {
        int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
        int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
        int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
        return (b << 16) | (g << 8) | r;
    }

    [Serializable]
    class RegisterRequest
    {
        public string title;
        public string description;
        public Author author;
        public string[] device_supported;
        public string category;
    }

    [Serializable]
    class Author
    {
        public string name;
        public string contact;
    }

    [Serializable]
    class RegisterResponse
    {
        public int status;
        public string sessionid;
        public string uri;
    }

    [Serializable]
    class EffectRequest
    {
        public string effect;
        public ColorParam param;
    }

    [Serializable]
    class ColorParam
    {
        public int color;
    }
}
