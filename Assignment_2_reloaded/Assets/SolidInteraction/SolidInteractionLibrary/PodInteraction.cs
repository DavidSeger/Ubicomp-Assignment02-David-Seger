using System.Text;
using System.Text.Json;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Diagnostics;

namespace SolidInteractionLibrary
{
#nullable enable
  public partial class AuthenticatedPodClient
  {

    // get pods connected to the webId
    public async Task<List<string>> GetPods()
    {
      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Authorization = authHeader;

      // get urls
      var loggedinIndexResponse = await client.GetStringAsync(serverUrl + ".account/");
      JsonDocument loggedinIndexResponseJson = JsonDocument.Parse(loggedinIndexResponse);

      if (loggedinIndexResponseJson.RootElement.TryGetProperty("controls", out JsonElement loggedinControlsElement))
      {
        if (loggedinControlsElement.TryGetProperty("account", out JsonElement accountElement))
        {
          if (accountElement.TryGetProperty("pod", out JsonElement podsUrlElement))
          {
            string? podsUrl = podsUrlElement.GetString();
            // get pods info
            HttpResponseMessage podsResponse = await client.GetAsync(podsUrl);
            string podsResponseBody = await podsResponse.Content.ReadAsStringAsync();
            JsonDocument podsResponseJson = JsonDocument.Parse(podsResponseBody);
            List<string> podNames = new List<string>();
            if (podsResponseJson.RootElement.TryGetProperty("pods", out JsonElement podsElement))
            {
              // check if user is owner of pod and add to list if true
              foreach (var podInfo in podsElement.EnumerateObject())
              {
                string podName = podInfo.Name;

                string podResponse = await client.GetStringAsync(podInfo.Value.GetString());
                JsonDocument podResponseJson = JsonDocument.Parse(podResponse);
                // Console.WriteLine("Pod response: " + podResponse);
                if (podResponseJson.RootElement.TryGetProperty("owners", out JsonElement ownersElement))
                {
                  foreach (var owner in ownersElement.EnumerateArray())
                  {
                    if (owner.TryGetProperty("webId", out JsonElement webIdElement))
                    {
                      string? podWebId = webIdElement.GetString();
                      if (podWebId == webId)
                      {
                        podNames.Add(podName);
                        break;
                      }
                    }
                  }
                }
                else
                {
                  throw new Exception("owners not found in pod response");
                }
              }
              return podNames;
            }
            else
            {
              throw new Exception("items not found in pod response");
            }

          }
          else
          {
            throw new Exception("pod url not found in controls");
          }
        }
        else
        {
          throw new Exception("account controls not found");
        }
      }
      else
      {
        throw new Exception("controls not found");
      }
    }

    public async Task<string> SaveFileAsync(string url, string contentType, string fileContent)
    {
      await CreateOrRenewOIDCToken();
      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Add("Authorization", $"DPoP {oidcToken}");
      string customDPoP = BuildJwtForContent("PUT", url, privateKey, publicRSAKey);
      // Console.WriteLine("customDPoP2 " + customDPoP);
      client.DefaultRequestHeaders.Add("DPoP", customDPoP);

      HttpResponseMessage response = await client.PutAsync(url, new StringContent(fileContent, Encoding.UTF8, contentType));
      response.EnsureSuccessStatusCode();
      if (response.StatusCode == System.Net.HttpStatusCode.ResetContent)
      {
        Console.WriteLine("File updated at " + url);
        return url;
      }
      string? location = response.Headers.Location?.ToString();
      if (location != null)
      {
        Console.WriteLine($"File saved at {location}");
        return location;
      }
      else
      {
        throw new Exception("No location header found in response.");
      }
    }
    public async Task<string> GetFileAsync(string location)
    {
      await CreateOrRenewOIDCToken();
      HttpClient client = new HttpClient();
      string customDPoP = BuildJwtForContent("GET", location, privateKey, publicRSAKey);
      client.DefaultRequestHeaders.Add("Authorization", $"DPoP {oidcToken}");
      client.DefaultRequestHeaders.Add("DPoP", customDPoP);
      HttpResponseMessage response = await client.GetAsync(location);
      response.EnsureSuccessStatusCode();
      string responseBody = await response.Content.ReadAsStringAsync();
      return responseBody;
    }

    public async Task GrantAccessToPod(string podLocation, string webId, string accessType)
    {
      if (accessType != "Read" && accessType != "Write" && accessType != "ReadWrite")
      {
        throw new Exception("Invalid access type. Must be Read, Write or ReadWrite");
      }
      await CreateOrRenewOIDCToken();
      HttpClient client = new HttpClient();
      string customDPoP = BuildJwtForContent("GET", podLocation, privateKey, publicRSAKey);
      client.DefaultRequestHeaders.Add("Authorization", $"DPoP {oidcToken}");
      client.DefaultRequestHeaders.Add("DPoP", customDPoP);
      HttpResponseMessage response = await client.GetAsync(podLocation);
      response.EnsureSuccessStatusCode();
      // get "Link" header
      IEnumerable<string> linkHeaders = response.Headers.GetValues("Link");
      string? aclInfo = linkHeaders
          .SelectMany(header => header.Split(','))
          .Select(link => link.Trim())
          .FirstOrDefault(link => link.Contains("rel=\"acl\""));
      if (aclInfo != null)
      {
        int start = aclInfo.IndexOf('<') + 1;
        int end = aclInfo.IndexOf('>');
        string aclLink = aclInfo.Substring(start, end - start);

        string customDPoP2 = BuildJwtForContent("GET", aclLink, privateKey, publicRSAKey);
        client.DefaultRequestHeaders.Remove("DPoP");
        client.DefaultRequestHeaders.Add("DPoP", customDPoP2);
        HttpResponseMessage aclResponse = await client.GetAsync(aclLink);
        aclResponse.EnsureSuccessStatusCode();
        string aclResponseBody = await aclResponse.Content.ReadAsStringAsync();
        string newAcl = aclResponseBody + $"\n<#agent>\n    a acl:Authorization;\n    acl:agent <{webId}>;\n    acl:accessTo <./>;\n    acl:default <./>;\n    acl:mode\n        ";
        switch (accessType)
        {
          case "Read":
            newAcl += "acl:Read.";
            break;
          case "Write":
            newAcl += "acl:Write.";
            break;
          case "ReadWrite":
            newAcl += "acl:Read, acl:Write.";
            break;
        }
        string customDPoP3 = BuildJwtForContent("PUT", aclLink, privateKey, publicRSAKey);
        client.DefaultRequestHeaders.Remove("DPoP");
        client.DefaultRequestHeaders.Add("DPoP", customDPoP3);
        HttpResponseMessage aclPutResponse = await client.PutAsync(aclLink, new StringContent(newAcl, Encoding.UTF8, "text/turtle"));
        aclPutResponse.EnsureSuccessStatusCode();
        Console.WriteLine($"Access granted to {webId} in pod {podLocation}");

      }
      else
      {
        throw new Exception("No ACL url found in response.");
      }
    }

    public async IAsyncEnumerable<string> SubscribeToResource(string resourceLocation)
    {
      await CreateOrRenewOIDCToken();

      HttpClient client = new HttpClient();
      string webSocketUrl = "http://localhost:3000/.notifications/WebSocketChannel2023/";
      string customDPoP2 = BuildJwtForContent("POST", webSocketUrl, privateKey, publicRSAKey);
      client.DefaultRequestHeaders.Add("Authorization", $"DPoP {oidcToken}");
      client.DefaultRequestHeaders.Remove("DPoP");
      client.DefaultRequestHeaders.Add("DPoP", customDPoP2);

      string body = "{\n  \"@context\": [ \"https://www.w3.org/ns/solid/notification/v1\" ],\n  \"type\": \"http://www.w3.org/ns/solid/notifications#WebSocketChannel2023\",\n  \"topic\": \"" + resourceLocation + "\"\n}";

      HttpResponseMessage subscriptionPostResponse = await client.PostAsync(webSocketUrl, new StringContent(body, Encoding.UTF8, "application/ld+json"));
      string responseBody = await subscriptionPostResponse.Content.ReadAsStringAsync();
      // {"name":"ForbiddenHttpError","message":"","statusCode":403,"errorCode":"H403","details":{}}
      // if forbidden, throw error
      if (subscriptionPostResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
      {
        throw new Exception("webId does not have read access to this resource");
      }

      subscriptionPostResponse.EnsureSuccessStatusCode();
      Console.WriteLine($"Subscribed to resource {resourceLocation}");

      string receiveFrom = responseBody.Split("\"receiveFrom\":\"")[1].Split("\"")[0];

      using (var ws = new ClientWebSocket())
      {
        await ws.ConnectAsync(new Uri(receiveFrom), CancellationToken.None);
        Console.WriteLine("Connected to WebSocket server at " + receiveFrom);

        // await ReceiveMessages(ws);
        await foreach (var message in ReceiveMessages(ws))
        {
          yield return message; // Yield each message as it's received
        }
      }

    }
    //   else
    //   {
    //     throw new Exception("No subscription service found in response.");
    //   }
    // }

    static async IAsyncEnumerable<string> ReceiveMessages(ClientWebSocket ws)
    {
      var buffer = new byte[1024 * 4];  // 4 KB buffer for incoming messages

      while (ws.State == WebSocketState.Open)
      {
        WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Text)
        {
          string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
          yield return message; // Yield each message as it's received
        }
        else if (result.MessageType == WebSocketMessageType.Close)
        {
          Console.WriteLine("WebSocket closed by the server.");
          await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
      }
    }

    // public async Task GrantAccessToSubFolder(string podLocation, string subFolder, string webId, string accessType)
    // {
    //   if (accessType != "Read" && accessType != "Write" && accessType != "ReadWrite")
    //   {
    //     throw new Exception("Invalid access type. Must be Read, Write or ReadWrite");
    //   }
    //   await CreateOrRenewOIDCToken();
    //   HttpClient client = new HttpClient();
    //   string customDPoP = BuildJwtForContent("GET", $"{podLocation}{subFolder}/", privateKey, publicRSAKey);
    //   client.DefaultRequestHeaders.Add("Authorization", $"DPoP {oidcToken}");
    //   client.DefaultRequestHeaders.Add("DPoP", customDPoP);
    //   HttpResponseMessage response = await client.GetAsync($"{podLocation}{subFolder}/");
    //   response.EnsureSuccessStatusCode();

    //   foreach (var header in response.Headers)
    //   {
    //     Console.WriteLine(header.Key + ": " + string.Join(", ", header.Value));
    //   }
    //   // get "Link" header
    //   IEnumerable<string> linkHeaders = response.Headers.GetValues("Link");
    //   string? aclInfo = linkHeaders
    //       .SelectMany(header => header.Split(','))
    //       .Select(link => link.Trim())
    //       .FirstOrDefault(link => link.Contains("rel=\"acl\""));
    //   if (aclInfo != null)
    //   {
    //     int start = aclInfo.IndexOf('<') + 1;
    //     int end = aclInfo.IndexOf('>');
    //     string aclLink = aclInfo.Substring(start, end - start);

    //     string customDPoP2 = BuildJwtForContent("GET", aclLink, privateKey, publicRSAKey);
    //     client.DefaultRequestHeaders.Remove("DPoP");
    //     client.DefaultRequestHeaders.Add("DPoP", customDPoP2);
    //     HttpResponseMessage aclResponse = await client.GetAsync(aclLink);
    //     // aclResponse.EnsureSuccessStatusCode();

    //     string newAcl = "";
    //     if (aclResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
    //     {
    //       newAcl = $"<#owner>\n    a acl:Authorization;\n    acl:agent <{webId}>;\n    acl:accessTo <./{subFolder}>;\n    acl:default <./{subFolder}>;\n    acl:mode\n        ";
    //       switch (accessType)
    //       {
    //         case "Read":
    //           newAcl += "acl:Read.";
    //           break;
    //         case "Write":
    //           newAcl += "acl:Write.";
    //           break;
    //         case "ReadWrite":
    //           newAcl += "acl:Read, acl:Write.";
    //           break;
    //       }
    //     }
    //     else
    //     {
    //       string aclResponseBody = await aclResponse.Content.ReadAsStringAsync();
    //       newAcl = aclResponseBody + $"\n<#agent>\n    a acl:Authorization;\n    acl:agent <{webId}>;\n    acl:accessTo <./{subFolder}>;\n    acl:default <./{subFolder}>;\n    acl:mode\n        ";
    //       switch (accessType)
    //       {
    //         case "Read":
    //           newAcl += "acl:Read.";
    //           break;
    //         case "Write":
    //           newAcl += "acl:Write.";
    //           break;
    //         case "ReadWrite":
    //           newAcl += "acl:Read, acl:Write.";
    //           break;
    //       }
    //     }

    //     // var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:3000/my-pod/folder1/");
    //     // request.Headers.Add("Slug", ".acl");
    //     // var newAclContent = new StringContent(newAcl, Encoding.UTF8, "text/turtle");
    //     // newAclContent.Headers.ContentLength = newAcl.Length;
    //     // request.Content = newAclContent;
    //     // string customDPoP3 = BuildJwtForContent("GET", "http://localhost:3000/my-pod/folder1/", privateKey, publicRSAKey);
    //     // client.DefaultRequestHeaders.Remove("DPoP");
    //     // client.DefaultRequestHeaders.Add("DPoP", customDPoP3);
    //     // HttpResponseMessage aclGetResponse = await client.SendAsync(request);
    //     // string responseBody = await aclGetResponse.Content.ReadAsStringAsync();
    //     // Console.WriteLine(responseBody);

    //     string customDPoP3 = BuildJwtForContent("PUT", aclLink, privateKey, publicRSAKey);
    //     client.DefaultRequestHeaders.Remove("DPoP");
    //     client.DefaultRequestHeaders.Add("DPoP", customDPoP3);
    //     HttpResponseMessage aclPutResponse = await client.PutAsync(aclLink, new StringContent(newAcl, Encoding.UTF8, "text/turtle"));
    //     Console.WriteLine(aclPutResponse);
    //     aclPutResponse.EnsureSuccessStatusCode();
    //     Console.WriteLine($"Access granted to {webId} in pod {podLocation}{subFolder}");
    //   }
    // }
  }
}
