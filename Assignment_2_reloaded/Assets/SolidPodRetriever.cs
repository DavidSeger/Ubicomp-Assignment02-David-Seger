using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

public class SolidPodRetriever : MonoBehaviour
{
    public TextMeshProUGUI activityInfoField;
    public TextMeshProUGUI activityInfoFieldKirk;
    public TextMeshProUGUI additionalInfo;
    public TextMeshProUGUI additionalInfoKai;

    private readonly string restServerUrl = "http://192.168.1.42:3000"; 

    void Start()
    {
    }

    public void ReadActivity()
    {
        StartCoroutine(ReadCurrentActivityCoroutine());
    }

    public void ReadActivityFromKai()
    {
        StartCoroutine(ReadCurrentActivityOfKaiCoroutine());
    }

    IEnumerator ReadCurrentActivityCoroutine()
    {
        string fileContentResponse = null;

        string containerName = "gazeData";
        string resourceName = "currentActivity.ttl";

        string url = $"{restServerUrl}/get-resource?containerName={UnityWebRequest.EscapeURL(containerName)}&resourceName={UnityWebRequest.EscapeURL(resourceName)}";

        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"An error occurred while reading current activity: {www.error}");
            yield break;
        }

        fileContentResponse = www.downloadHandler.text;

        try
        {
            Graph g = new Graph();
            StringParser.Parse(g, fileContentResponse);

            NamespaceMapper ns = new NamespaceMapper();
            ns.AddNamespace("xsd", new Uri("http://www.w3.org/2001/XMLSchema#"));
            ns.AddNamespace("foaf", new Uri("http://xmlns.com/foaf/0.1/"));
            ns.AddNamespace("prov", new Uri("http://www.w3.org/ns/prov#"));
            ns.AddNamespace("schema", new Uri("https://schema.org/"));
            ns.AddNamespace("bm", new Uri("http://bimerr.iot.linkeddata.es/def/occupancy-profile#"));
            g.NamespaceMap.Import(ns);

            string sparqlQuery = @"
                    PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
                    PREFIX prov: <http://www.w3.org/ns/prov#>
                    PREFIX schema: <https://schema.org/>
                    PREFIX bm: <http://bimerr.iot.linkeddata.es/def/occupancy-profile#>
                    PREFIX foaf: <http://xmlns.com/foaf/0.1/>

                    SELECT ?activityName ?personName ?endTime ?probability
                    WHERE {
                        ?activity a prov:Activity ;
                                  schema:name ?activityName ;
                                  prov:wasAssociatedWith ?data ;
                                  prov:endedAtTime ?endTime ;
                                  bm:probability ?probability .
                        ?person a foaf:Person ;
                                foaf:name ?personName .
                    }";

            SparqlResultSet results = g.ExecuteQuery(sparqlQuery) as SparqlResultSet;

            string activity = "could not parse";
            string person = "could not parse";
            string endTime = "could not parse";
            string probability = "could not parse";
            foreach (SparqlResult result in results)
            {
                activity = result["activityName"]?.ToString() ?? "N/A";
                person = result["personName"]?.ToString() ?? "N/A";
                endTime = result["endTime"]?.ToString() ?? "N/A";
                probability = result["probability"]?.ToString() ?? "N/A";
            }

            activity = activity.Split('^')[0];
            person = person.Split('^')[0];
            endTime = endTime.Split('^')[0];
            probability = probability.Split('^')[0];

            activityInfoField.text = "<b>activity: </b>" + activity + "\n\n" +
                                     "<b>person: </b>" + person + "\n\n" +
                                     "<b>endTime: </b>" + endTime + "\n\n" +
                                     "<b>probability: </b>" + probability + "\n\n";
            switch (activity)
            {
                case "CheckAction":
                    StartCoroutine(QueryRobotCoroutine("INSPECT"));
                    break;
                case "ReadAction":
                    StartCoroutine(QueryRobotCoroutine("READ"));
                    break;
                default:
                    additionalInfo.text = "No support material available for this action";
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while processing activity data: {ex}");
        }
    }

    IEnumerator ReadCurrentActivityOfKaiCoroutine()
    {
        string fileContentResponse = null;

        string containerName = "gazeData";
        string resourceName = "currentActivity.ttl";

        string url = $"{restServerUrl}/get-resource-kai?containerName={UnityWebRequest.EscapeURL(containerName)}&resourceName={UnityWebRequest.EscapeURL(resourceName)}";

        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"An error occurred while reading current activity: {www.error}");
            yield break;
        }

        fileContentResponse = www.downloadHandler.text;

        try
        {
            Graph g = new Graph();
            StringParser.Parse(g, fileContentResponse);

            NamespaceMapper ns = new NamespaceMapper();
            ns.AddNamespace("xsd", new Uri("http://www.w3.org/2001/XMLSchema#"));
            ns.AddNamespace("foaf", new Uri("http://xmlns.com/foaf/0.1/"));
            ns.AddNamespace("prov", new Uri("http://www.w3.org/ns/prov#"));
            ns.AddNamespace("schema", new Uri("https://schema.org/"));
            ns.AddNamespace("bm", new Uri("http://bimerr.iot.linkeddata.es/def/occupancy-profile#"));
            g.NamespaceMap.Import(ns);

            string sparqlQuery = @"
                    PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
                    PREFIX prov: <http://www.w3.org/ns/prov#>
                    PREFIX schema: <https://schema.org/>
                    PREFIX bm: <http://bimerr.iot.linkeddata.es/def/occupancy-profile#>
                    PREFIX foaf: <http://xmlns.com/foaf/0.1/>

                    SELECT ?activityName ?personName ?endTime ?probability
                    WHERE {
                        ?activity a prov:Activity ;
                                  schema:name ?activityName ;
                                  prov:wasAssociatedWith ?data ;
                                  prov:endedAtTime ?endTime ;
                                  bm:probability ?probability .
                        ?person a foaf:Person ;
                                foaf:name ?personName .
                    }";

            SparqlResultSet results = g.ExecuteQuery(sparqlQuery) as SparqlResultSet;

            string activity = "could not parse";
            string person = "could not parse";
            string endTime = "could not parse";
            string probability = "could not parse";
            foreach (SparqlResult result in results)
            {
                activity = result["activityName"]?.ToString() ?? "N/A";
                person = result["personName"]?.ToString() ?? "N/A";
                endTime = result["endTime"]?.ToString() ?? "N/A";
                probability = result["probability"]?.ToString() ?? "N/A";
            }

            // Clean up the strings
            activity = activity.Split('^')[0];
            person = person.Split('^')[0];
            endTime = endTime.Split('^')[0];
            probability = probability.Split('^')[0];

            // Update the UI on the main thread
            activityInfoFieldKirk.text = "<b>activity (Kirk): </b>" + activity + "\n\n" +
                                         "<b>person (Kirk): </b>" + person + "\n\n" +
                                         "<b>endTime (Kirk): </b>" + endTime + "\n\n" +
                                         "<b>probability (Kirk): </b>" + probability + "\n\n";

            switch (activity)
            {
                case "CheckAction":
                case "Check Action":
                    StartCoroutine(QueryRobotCoroutine("INSPECT", true));
                    break;
                case "Read action":
                case "ReadAction":
                    StartCoroutine(QueryRobotCoroutine("READ", true));
                    break;
                default:
                    additionalInfoKai.text = "No support material available for this action";
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while processing activity data: {ex}");
        }
    }

    IEnumerator QueryRobotCoroutine(string action, bool forKai = false)
    {
        string comment = null;
        string url = $"{restServerUrl}/query-robot?action="+action;

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Accept", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"An error occurred while querying robot: {www.error}");
            yield break;
        }

        string responseText = www.downloadHandler.text;

        try
        {
            // Parse the responseText as JSON
            CommentsResponse jsonResponse = JsonUtility.FromJson<CommentsResponse>(responseText);
            if (jsonResponse.comments != null && jsonResponse.comments.Length > 0)
            {
                comment = jsonResponse.comments[0];
            }
            else
            {
                Debug.LogError("No comments found in the response");
                yield break;
            }

            // Display the comment
            Debug.Log("Comment: " + comment);
            if (forKai)
            {
                additionalInfoKai.text = "<b>Comment:</b> " + comment + "\n\n";
            } else {
                additionalInfo.text = "<b>Comment:</b> " + comment + "\n\n";
            }
           
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while processing robot query response: {ex}");
        }
    }

    [Serializable]
    public class CommentsResponse
    {
        public string[] comments;
    }
}
