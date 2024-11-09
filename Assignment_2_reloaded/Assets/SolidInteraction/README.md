This project folder contains the C# library at /SolidInteractionLibrary to interact with solid Pods from a [Community Solid Server](https://communitysolidserver.github.io/CommunitySolidServer/latest/). The folder SolidInteractionConsoleApp contains an example program to showcase how to use the library.

The library allows users to authenticate and interact with their private pods without user input. It does not follow the usual flow of OIDC authentication, which includes redirecting to the identity provider in the browser. Instead it uses the client credentials flow described [here](https://communitysolidserver.github.io/CommunitySolidServer/latest/usage/client-credentials/) for JavaScript.

## Getting Started

start a Community Solid Server, for example locally on port 3000 with

```bash
$ npx @solid/community-server
```

We create an account

```csharp
AuthenticationHeaderValue authHeader = await SolidClient.CreateAccount(serverUrl, email, password);
```

or login to an existing account

```csharp
AuthenticationHeaderValue authHeader = await AuthenticatedPodClient.LoginAsync(serverUrl, email, password);
```

Then we create a pod if we didn't already create one.

```csharp
await SolidClient.CreatePod(serverUrl, podName, authHeader);
```

## Interaction with the Pod

First we make an authenticated client. It creates a credentials token connected to the given webId. Every time an authenticated request is made it checks if it has a valid OIDC token required for authenticated requests. If there is still none, or the token is about to expire, it renews the OIDC token.

```csharp
AuthenticatedPodClient authenticatedClient = await AuthenticatedPodClient.BuildAsync(serverUrl, webId, email, password);
```

We can then get a list of pod urls with the given webId as the owner and save a private file on any of these pods. We can then fetch and print the file content to verify it worked.

```csharp
List<string> podUrls = await authenticatedClient.GetPods();
string location = await authenticatedClient.SaveFileAsync($"{podUrls[0]}folder/hello.txt", contentType, fileContent);
string fileContentResponse = await authenticatedClient.GetFileAsync(location);
Console.WriteLine("file content: " + fileContentResponse);
```

We can grant "Read", "Write", or "ReadWrite" access to a pod to another WebId like this. The WebId

```csharp
await authenticatedClient.GrantAccessToPod("http://localhost:3000/my-pod/", "http://localhost:3000/podFromAnotherAccount/profile/card#me", "ReadWrite");
```

We can subscribe to a resource (file or folder) to receive e.g. create or update notifications like this.

```csharp
await foreach (var message in authenticatedClient.SubscribeToResource("http://localhost:3000/folder/hello.txt"))
{
  Console.WriteLine($"Received message: {message}");
}
```

## Adding the library to a Unity project

First, add [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) to your Unity proejct. Add packages System.Text.Json, System.IdentityModel.Tokens.Jwt, Microsoft.IdentityModel, Microsoft.IdentityModel.Tokens with NuGet.
Drag the SolidInteractionLibrary folder into the Assets folder in Unity. Make sure to delete any possible obj folder in SolidInteractionLibrary.

## Testing the library with Unity

Start a community solid server on localhost 3000 by running community-solid-server in the command line. Import the[TestScriptForUnity.cs](TestScriptForUnity.cs) script to Unity and add it to e.g. a Button. Finally, run the Unity project and click the button and check the Console.
