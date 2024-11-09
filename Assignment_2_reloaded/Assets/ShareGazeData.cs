using SolidInteractionLibrary;
using System;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class ShareGazeData : MonoBehaviour
{

    public TextMeshProUGUI Notification;
    private readonly string server = "http://192.168.1.42:3000";

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void share()
    {
        StartCoroutine(shareWithKai());
    }

    IEnumerator shareWithKai()
    {

        string url = $"{server}/authorize-user-gaze-data?username=kai2_ubicomp24";

        UnityWebRequest www = UnityWebRequest.Post(url, "","");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"An error occurred while reading current activity: {www.error}");
            yield break;
        }
        Notification.text = "Kai now has access to your gaze data!";
    }
}
