using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.Windows.WebCam;
using System;
using System.Linq;
using System.IO;
using System.Collections;

public class FotoOnActivation : MonoBehaviour
{
    private PhotoCapture photoCaptureObject = null;

    private int cameraResolutionWidth;
    private int cameraResolutionHeight;

    void Start()
    {
    }

    void OnDestroy()
    {
    }


    public void TakePhoto()
    {
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending(
            (res) => res.width * res.height).First();

        cameraResolutionWidth = cameraResolution.width;
        cameraResolutionHeight = cameraResolution.height;

        PhotoCapture.CreateAsync(false, (captureObject) =>
        {
            photoCaptureObject = captureObject;

            CameraParameters cameraParameters = new CameraParameters
            {
                hologramOpacity = 0.0f,
                cameraResolutionWidth = cameraResolutionWidth,
                cameraResolutionHeight = cameraResolutionHeight,
                pixelFormat = CapturePixelFormat.BGRA32
            };

            photoCaptureObject.StartPhotoModeAsync(cameraParameters, OnPhotoModeStarted);
        });
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
        }
    }

    private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            Texture2D targetTexture = new Texture2D(cameraResolutionWidth, cameraResolutionHeight);

            photoCaptureFrame.UploadImageDataToTexture(targetTexture);

            byte[] imageData = targetTexture.EncodeToPNG();

            string filename = $"CapturedImage_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            #if WINDOWS_UWP
            string filePath = Path.Combine(Windows.Storage.KnownFolders.CameraRoll.Path, filename);               
            System.IO.File.WriteAllBytes(filePath, imageData);

            Debug.Log($"Photo saved to: {filePath}");
            #endif
        }
        else
        {
            Debug.LogError("Failed to capture photo.");
        }

        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    private void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
}
