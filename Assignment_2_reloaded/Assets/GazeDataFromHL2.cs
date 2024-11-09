using ARETT;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GazeDataFromHL2 : MonoBehaviour
{

    // connect the DtatProvider-Prefab from ARETT in the Unity Editor
    public DataProvider DataProvider;
    public GameObject ReadingHelp;
    public GameObject SearchingHelp;
    public GameObject InspectionHelp;
    public GameObject WordDefinition;
    private ConcurrentQueue<Action> _mainThreadWorkQueue = new ConcurrentQueue<Action>();

    private static string serverAddress = "http://192.168.1.42:8000";
    private List<string> gazeDataList = new List<string>();
    private float dataCollectionInterval = 5f;
    public string currentlyActive = "nothing";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Check if there is something to process
        if (!_mainThreadWorkQueue.IsEmpty)
        {
            // Process all commands which are waiting to be processed
            // Note: This isn't 100% thread save as we could end in a loop when there is still new data coming in.
            //       However, data is added slowly enough so we shouldn't run into issues.
            while (_mainThreadWorkQueue.TryDequeue(out Action action))
            {
                // Invoke the waiting action
                action.Invoke();
            }
        }
    }

    /// <summary>
    /// Starts the Coroutine to get Eye tracking data on the HL2 from ARETT.
    /// </summary>
    public void StartArettData()
    {
        StartCoroutine(SubscribeToARETTData());
        StartCoroutine(SendDataAndClassifyPeriodically());
    }

    /// <summary>
    /// Subscribes to newDataEvent from ARETT.
    /// </summary>
    /// <returns></returns>
    private IEnumerator SubscribeToARETTData()
    {
        //*
        _mainThreadWorkQueue.Enqueue(() =>
        {
            DataProvider.NewDataEvent += HandleDataFromARETT;
        });
        //*/

        print("subscribed to ARETT events");
        yield return null;

    }

    /// <summary>
    /// Unsubscribes from NewDataEvent from ARETT.
    /// </summary>
    public void UnsubscribeFromARETTData()
    {
        _mainThreadWorkQueue.Enqueue(() =>
        {
            DataProvider.NewDataEvent -= HandleDataFromARETT;
        });

    }




    /// <summary>
    /// Handles gaze data from ARETT and allows you to do something with it
    /// </summary>
    /// <param name="gd"></param>
    /// <returns></retuns>
    public void HandleDataFromARETT(GazeData gd)
    {
        string serialized = SerializeToCsv(gd);
        gazeDataList.Add(serialized);
    }

    private IEnumerator SendDataAndClassifyPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(dataCollectionInterval);

            if (gazeDataList.Count > 0)
            {
                    string csvData = string.Join("\n", gazeDataList);

                    // Send to server and get classification
                    yield return StartCoroutine(SendDataAndGetClassification(csvData));

                    // Clear the list after sending
                    gazeDataList.Clear();
            }
        }
    }

    private IEnumerator SendDataAndGetClassification(string csvData)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(csvData);

        UnityWebRequest request = new UnityWebRequest(serverAddress + "/classify/", "POST")
        {
            uploadHandler = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader("Content-Type", "text/csv");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Data successfully sent to the server.");
            Debug.Log("Response: " + request.downloadHandler.text);

            PredictionResponse predictionResponse = JsonUtility.FromJson<PredictionResponse>(request.downloadHandler.text);
            HandlePrediction(predictionResponse.prediction);
        }
        else
        {
            Debug.LogError("Error sending data: " + request.error);
        }
    }

    [Serializable]
    private class PredictionResponse
    {
        public string prediction;
    }

    private void HandlePrediction(string prediction)
    {
        switch (prediction)
        {
            case "Reading":
                OnReading();
                break;
            case "Inspection":
                OnInspection();
                break;
            case "Search":
                OnSearch();
                break;
            default:
                break;
        }
    }

    public static string SerializeToCsv(GazeData data)
    {
        StringBuilder csvBuilder = new StringBuilder();

        // Helper function to format nullable Vector3 fields
        string FormatVector3(Vector3? vector)
        {
            if (vector.HasValue)
            {
                return $"{vector.Value.x},{vector.Value.y},{vector.Value.z}";
            }
            else
            {
                return ",,";
            }
        }

        // Build CSV line
        csvBuilder.Append($"{data.EyeDataTimestamp},");                  // eyeDataTimestamp
        csvBuilder.Append($"{data.EyeDataRelativeTimestamp},");         // eyeDataRelativeTimestamp
        csvBuilder.Append($"{data.FrameTimestamp},");                   // frameTimestamp
        csvBuilder.Append($"{data.IsCalibrationValid},");               // isCalibrationValid
        csvBuilder.Append($"{data.GazeHasValue},");                     // gazeHasValue

        // Gaze Origin
        csvBuilder.Append($"{data.GazeOrigin.x},{data.GazeOrigin.y},{data.GazeOrigin.z},"); // gazeOrigin_x,y,z

        // Gaze Direction
        csvBuilder.Append($"{data.GazeDirection.x},{data.GazeDirection.y},{data.GazeDirection.z},"); // gazeDirection_x,y,z

        // Gaze Point Data
        csvBuilder.Append($"{data.GazePointHit},");                     // gazePointHit
        csvBuilder.Append($"{data.GazePoint.x},{data.GazePoint.y},{data.GazePoint.z},"); // gazePoint_x,y,z
        csvBuilder.Append($"\"{data.GazePointName}\",");                // gazePoint_target_name

        // Gaze Point Target Data
        csvBuilder.Append($"{data.GazePointOnHit.x},{data.GazePointOnHit.y},{data.GazePointOnHit.z},"); // gazePoint_target_x,y,z
        csvBuilder.Append($"{data.GazePointHitPosition.x},{data.GazePointHitPosition.y},{data.GazePointHitPosition.z},"); // gazePoint_target_pos_x,y,z
        csvBuilder.Append($"{data.GazePointHitRotation.x},{data.GazePointHitRotation.y},{data.GazePointHitRotation.z},"); // gazePoint_target_rot_x,y,z
        csvBuilder.Append($"{data.GazePointHitScale.x},{data.GazePointHitScale.y},{data.GazePointHitScale.z},"); // gazePoint_target_scale_x,y,z

        // Gaze Point Left Screen
        csvBuilder.Append(FormatVector3(data.GazePointLeftDisplay));
        csvBuilder.Append(",");

        // Gaze Point Right Screen
        csvBuilder.Append(FormatVector3(data.GazePointRightDisplay));
        csvBuilder.Append(",");

        // Gaze Point Mono Screen
        csvBuilder.Append($"{data.GazePointMonoDisplay.x},{data.GazePointMonoDisplay.y},{data.GazePointMonoDisplay.z},"); // gazePointMonoScreen_x,y,z

        // Gaze Point Webcam
        csvBuilder.Append($"{data.GazePointWebcam.x},{data.GazePointWebcam.y},{data.GazePointWebcam.z},"); // GazePointWebcam_x,y,z

        // Gaze Point AOI Data
        csvBuilder.Append($"{data.GazePointAOIHit},");                  // gazePointAOIHit
        csvBuilder.Append($"{data.GazePointAOI.x},{data.GazePointAOI.y},{data.GazePointAOI.z},"); // gazePointAOI_x,y,z
        csvBuilder.Append($"\"{data.GazePointAOIName}\",");             // gazePointAOI_name

        // Gaze Point AOI Target Data
        csvBuilder.Append($"{data.GazePointAOIOnHit.x},{data.GazePointAOIOnHit.y},{data.GazePointAOIOnHit.z},"); // gazePointAOI_target_x,y,z
        csvBuilder.Append($"{data.GazePointAOIHitPosition.x},{data.GazePointAOIHitPosition.y},{data.GazePointAOIHitPosition.z},"); // gazePointAOI_target_pos_x,y,z
        csvBuilder.Append($"{data.GazePointAOIHitRotation.x},{data.GazePointAOIHitRotation.y},{data.GazePointAOIHitRotation.z},"); // gazePointAOI_target_rot_x,y,z
        csvBuilder.Append($"{data.GazePointAOIHitScale.x},{data.GazePointAOIHitScale.y},{data.GazePointAOIHitScale.z},"); // gazePointAOI_target_scale_x,y,z

        // Gaze Point AOI Webcam
        csvBuilder.Append($"{data.GazePointAOIWebcam.x},{data.GazePointAOIWebcam.y},{data.GazePointAOIWebcam.z},"); // GazePointAOIWebcam_x,y,z

        bool foundCamera = false;
        // Main Camera PositionInfo
        if (data.positionInfos != null)
        {
            foreach (var p in data.positionInfos)
            {
                if (p.gameObjectName == "Main Camera")
                {
                    if (p.positionValid)
                    {
                        // Position
                        csvBuilder.Append($"{p.xPosition},{p.yPosition},{p.zPosition},"); // GameObject_Main Camera_xPos,yPos,zPos

                        // Rotation
                        csvBuilder.Append($"{p.xRotation},{p.yRotation},{p.zRotation},"); // GameObject_Main Camera_xRot,yRot,zRot

                        // Scale
                        csvBuilder.Append($"{p.xScale},{p.yScale},{p.zScale},"); // GameObject_Main Camera_xScale,yScale,zScale

                        foundCamera = true;
                        break;
                    }
                }
            }
        }

        // Append Main Camera position, rotation, scale
        if (!foundCamera)
        {
            // Append empty fields for position
            csvBuilder.Append(",,,");

            // Append empty fields for rotation
            csvBuilder.Append(",,,");

            // Append empty fields for scale
            csvBuilder.Append("1,1,1,");

        }

        // Append 'info' field (empty or any additional info)
        csvBuilder.Append(""); // 'info' field

        return csvBuilder.ToString();
    }

    // Methods to handle each prediction value
    private void OnReading()
    {
        if (currentlyActive != "nothing")
        {
            switch (currentlyActive)
            {
                case "reading":
                    break;
                case "inspection":
                    InspectionHelp.SetActive(false);
                    break;
                case "searching":
                    SearchingHelp.SetActive(false);
                    SearchingHelp.GetComponent<MusicController>().DeactivateMusic();
                    break;
                default:
                    break;
            }
        }

        ReadingHelp.SetActive(true);
        ReadingHelp.GetComponent<DictionaryLookup>().ActivateSpeechRecognition();
        currentlyActive = "reading";
    }

    private void OnInspection()
    {
        if (currentlyActive != "nothing")
        {
            switch (currentlyActive)
            {
                case "reading":
                    ReadingHelp.SetActive(false);
                    ReadingHelp.GetComponent<DictionaryLookup>().DeactivateSpeechRecognition();
                    WordDefinition.SetActive(false);
                    break;
                case "inspection":
                    break;
                case "searching":
                    SearchingHelp.SetActive(false);
                    SearchingHelp.GetComponent<MusicController>().DeactivateMusic();
                    break;
                default:
                    break;
            }
        }

        currentlyActive = "inspection";
        InspectionHelp.SetActive(true);
        InspectionHelp.GetComponent<FotoOnActivation>().TakePhoto();
    }

    private void OnSearch()
    {
        if (currentlyActive != "nothing")
        {
            switch (currentlyActive)
            {
                case "reading":
                    ReadingHelp.SetActive(false);
                    ReadingHelp.GetComponent<DictionaryLookup>().DeactivateSpeechRecognition();
                    WordDefinition.SetActive(false);
                    break;
                case "inspection":
                    InspectionHelp.SetActive(false);
                    break;
                case "searching":
                    break;
                default:
                    break;
            }
        }

        SearchingHelp.SetActive(true);
        SearchingHelp.GetComponent<MusicController>().ActivateMusic();
        currentlyActive = "searching";
    }
}
