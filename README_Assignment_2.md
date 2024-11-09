# README - Assignment 2

## Architecture

The project contains 3 different major parts: 
- The Unity application (called "Assignment_2_Reloaded")
- A Python based server, which has the random forest classifier trained and has endpoints to add raw gaze data (called once per gaze data event by the holoLens), and an endpoint to get the classification of the last 5 seconds of activity

The communication is REST based

## Setup

To get everything running, you need to follow these steps:

1. Open the project featureClassifier, install the needed python packages and run the main method of FeatureClassifier.py
2. Open the file "ActivityClassifier.cs" from the unity project (inside the assets folder), at line 27, set the variable "url" to the address your server is running on + /classify/, e.g. "http://yourIp:8000/classify"
3. Do the same inside the file "GazeDataFromHL2.cs", set the variable "serverAddress" on line 16 to the ip your server is running on, e.g. "http://yourIp:8000".
4. Deploy the app on HoloLens

## Code

I wrote all scripts in the assets Folder, soemtimes using tutorials or templates on how to do certain Things (f.e. activating voice commands, taking photos with the HoloLens etc.). I didnt really use libraries outside of MRTK and the required libraries for developing on the HoloLens. The Server code was written using the provided python classifier as a Basis, i mostly just added teh FastAPI endpoints to it. 

## Functions

- Searching: The HoloLens plays music to help you concentrate while you search for something
- Reading: Say any word out loud to get its definition displays!
- Inspecting: The HoloLens automatically takes a picture of what you are inspecting, so you can Access the file from your Computer and use it for a reverse Image search

Please note, that the Video of the hand-in cuts off when the inspection help is triggered, since it takes a photo and that stops the recording. 
