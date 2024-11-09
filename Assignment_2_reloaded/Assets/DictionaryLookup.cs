using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;
using System;
using System.Collections.Generic;

public class DictionaryLookup : MonoBehaviour
{
    public TextMeshProUGUI definitionText;

    private DictationRecognizer dictationRecognizer;

    public GameObject definitionField;

    void Start()
    {
        
    }

    private void Awake()
    {
        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;
        dictationRecognizer.DictationHypothesis += DictationRecognizer_DictationHypothesis;
        dictationRecognizer.DictationComplete += DictationRecognizer_DictationComplete;
        dictationRecognizer.DictationError += DictationRecognizer_DictationError;
    }

    void OnDestroy()
    {
        if (dictationRecognizer != null)
        {
            dictationRecognizer.Dispose();
        }
    }

    public void ActivateSpeechRecognition()
    {
        StartCoroutine(StartDictationAfterPhraseRecognitionSystemHasStopped());
    }

    private IEnumerator StartDictationAfterPhraseRecognitionSystemHasStopped()
    {
        while (PhraseRecognitionSystem.Status != SpeechSystemStatus.Stopped)
        {
            yield return null;
        }

        if (dictationRecognizer.Status != SpeechSystemStatus.Running)
        {
            dictationRecognizer.Start();
            Debug.Log("Speech recognition activated.");
        }
    }

    public void DeactivateSpeechRecognition()
    {
        if (dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
            Debug.Log("Speech recognition deactivated.");
        }

    }

    private void DictationRecognizer_DictationResult(string text, ConfidenceLevel confidence)
    {
        Debug.Log($"Recognized text: {text}");
        string word = text.Trim();
        StartCoroutine(GetDefinition(word));
        DeactivateSpeechRecognition();
    }

    private void DictationRecognizer_DictationHypothesis(string text)
    {
        Debug.Log($"Hypothesized text: {text}");
    }
    private void DictationRecognizer_DictationComplete(DictationCompletionCause cause)
    {
        Debug.Log($"Dictation completed: {cause}");
        if (cause != DictationCompletionCause.Complete)
        {

            ActivateSpeechRecognition();
        }
    }

    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        Debug.LogError($"Dictation error: {error}; HResult = {hresult}");
        // Handle the error as needed
    }

    private IEnumerator GetDefinition(string word)
    {
        string url = "https://api.dictionaryapi.dev/api/v2/entries/en/" + word.ToLower();
        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching definition: " + request.error);
            definitionText.text = "Definition not found.";
        }
        else
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("API Response: " + responseText);

            try
            {
                List<RootObject> entries = JsonHelper.FromJson<RootObject>(responseText);

                if (entries != null && entries.Count > 0)
                {
                    RootObject entry = entries[0];

                    StringBuilder definitionsBuilder = new StringBuilder();
                        foreach (Meaning meaning in entry.meanings)
                        {
                            definitionsBuilder.AppendLine($"<b>{meaning.partOfSpeech}</b>");
                            foreach (Definition def in meaning.definitions)
                            {
                                definitionsBuilder.AppendLine($"- {def.definition}");
                            }
                            definitionsBuilder.AppendLine();
                        }

                        definitionField.SetActive(true);
                        definitionText.text = definitionsBuilder.ToString();
                    }
            }
            catch (Exception ex)
            {
                Debug.LogError("JSON Parsing Error: " + ex.Message);
                definitionText.text = "Error parsing definition.";
            }
        }
    }

    [Serializable]
    public class RootObject
    {
        public string word;
        public string phonetic;
        public Phonetic[] phonetics;
        public string origin;
        public Meaning[] meanings;
    }

    [Serializable]
    public class Phonetic
    {
        public string text;
        public string audio;
    }

    [Serializable]
    public class Meaning
    {
        public string partOfSpeech;
        public Definition[] definitions;
    }

    [Serializable]
    public class Definition
    {
        public string definition;
        public string example;
        public string[] synonyms;
        public string[] antonyms;
    }

    public static class JsonHelper
    {
        public static List<T> FromJson<T>(string json)
        {
            string newJson = "{ \"array\": " + json + " }";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return new List<T>(wrapper.array);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }
}
