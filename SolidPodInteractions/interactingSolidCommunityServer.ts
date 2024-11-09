import { createDpopHeader, generateDpopKeyPair } from '@inrupt/solid-client-authn-core';
import fetch from 'cross-fetch';
import * as fs from "node:fs";

const podUrl = "https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/";
const podUrl_secondAccount = "https://wiser-solid-xi.interactions.ics.unisg.ch/Kirk/";

// First we request the account API controls to find out where we can log in
const authenticate = async (): Promise<any> => {

  const indexResponse = await fetch('https://wiser-solid-xi.interactions.ics.unisg.ch/.account/');
  const { controls } = await indexResponse.json();

  //console.log ("**** Index response: ", indexResponse.json());
  console.log ("**** Controls: ", controls);

  // And then we log in to the account API
  const response = await fetch(controls.password.login, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ email: 'david.seger@adon.li', password: 'UbiComp24' }),
  });
  // This authorization value will be used to authenticate in the next step
  const { authorization } = await response.json();
  return authorization;
}

const getAuthorizationToken = async (authorization: any): Promise<any> => {
  
  console.log("This is the authorization that I get: ", authorization)
  // Now that we are logged in, we need to request the updated controls from the server.
  // These will now have more values than in the previous example.
  const indexResponse = await fetch('https://wiser-solid-xi.interactions.ics.unisg.ch/.account/', {
    headers: { authorization: `CSS-Account-Token ${authorization}` }
  });
  const{controls} = await indexResponse.json();

  // Here we request the server to generate a token on our account
  const response= await fetch(controls.account.clientCredentials, {
    method: 'POST',
    headers: { authorization: `CSS-Account-Token ${authorization}`, 'content-type': 'application/json' },
    // The name field will be used when generating the ID of your token.
    // The WebID field determines which WebID you will identify as when using the token.
    // Only WebIDs linked to your account can be used.
    body: JSON.stringify({ name: 'my-token', webId: 'https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/profile/card#me' }),
});

  // These are the identifier and secret of your token.
  // Store the secret somewhere safe as there is no way to request it again from the server!
  // The `resource` value can be used to delete the token at a later point in time.
  
  const { id, secret, resource } = await response.json();
  return [id, secret, resource]
}

const getTokenUsage = async(id: any, secret: any): Promise<any> => {
  // A key pair is needed for encryption.
  // This function from `solid-client-authn` generates such a pair for you.
  const dpopKey = await generateDpopKeyPair();

  // These are the ID and secret generated in the previous step.
  // Both the ID and the secret need to be form-encoded.
  const authString = `${encodeURIComponent(id)}:${encodeURIComponent(secret)}`;
  // This URL can be found by looking at the "token_endpoint" field at
  const tokenUrl = 'https://wiser-solid-xi.interactions.ics.unisg.ch/.oidc/token';
  const response = await fetch(tokenUrl, {
    method: 'POST',
    headers: {
      // The header needs to be in base64 encoding.
      authorization: `Basic ${Buffer.from(authString).toString('base64')}`,
      'content-type': 'application/x-www-form-urlencoded',
      dpop: await createDpopHeader(tokenUrl, 'POST', dpopKey),
    },
    body: 'grant_type=client_credentials&scope=webid',
  });

  // This is the Access token that will be used to do an authenticated request to the server.
  // The JSON also contains an "expires_in" field in seconds,
  // which you can use to know when you need request a new Access token.
  //const  access_token = await response.json();
  const { access_token: accessToken } = await response.json();

 return [accessToken, dpopKey];

}

const authenticate_secondAccount = async (): Promise<any> => {

  const indexResponse = await fetch('https://wiser-solid-xi.interactions.ics.unisg.ch/.account/');
  const { controls } = await indexResponse.json();

  //console.log ("**** Index response: ", indexResponse.json());
  console.log ("**** Controls: ", controls);

  // And then we log in to the account API
  const response = await fetch(controls.password.login, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ email: 'david.seger@student.unisg.ch', password: 'WAS2023' }),
  });
  // This authorization value will be used to authenticate in the next step
  const { authorization } = await response.json();
  return authorization;
}

const getAuthorizationToken_secondAccount = async (authorization: any): Promise<any> => {

  console.log("This is the authorization that I get: ", authorization)
  // Now that we are logged in, we need to request the updated controls from the server.
  // These will now have more values than in the previous example.
  const indexResponse = await fetch('https://wiser-solid-xi.interactions.ics.unisg.ch/.account/', {
    headers: { authorization: `CSS-Account-Token ${authorization}` }
  });
  const{controls} = await indexResponse.json();

  // Here we request the server to generate a token on our account
  const response= await fetch(controls.account.clientCredentials, {
    method: 'POST',
    headers: { authorization: `CSS-Account-Token ${authorization}`, 'content-type': 'application/json' },
    // The name field will be used when generating the ID of your token.
    // The WebID field determines which WebID you will identify as when using the token.
    // Only WebIDs linked to your account can be used.
    body: JSON.stringify({ name: 'my-token', webId: 'https://wiser-solid-xi.interactions.ics.unisg.ch/Kirk/profile/card#me' }),
  });

  // These are the identifier and secret of your token.
  // Store the secret somewhere safe as there is no way to request it again from the server!
  // The `resource` value can be used to delete the token at a later point in time.

  const { id, secret, resource } = await response.json();
  return [id, secret, resource]
}

const getTokenUsage_secondAccount = async(id: any, secret: any): Promise<any> => {
  // A key pair is needed for encryption.
  // This function from `solid-client-authn` generates such a pair for you.
  const dpopKey = await generateDpopKeyPair();

  // These are the ID and secret generated in the previous step.
  // Both the ID and the secret need to be form-encoded.
  const authString = `${encodeURIComponent(id)}:${encodeURIComponent(secret)}`;
  // This URL can be found by looking at the "token_endpoint" field at
  const tokenUrl = 'https://wiser-solid-xi.interactions.ics.unisg.ch/.oidc/token';
  const response = await fetch(tokenUrl, {
    method: 'POST',
    headers: {
      // The header needs to be in base64 encoding.
      authorization: `Basic ${Buffer.from(authString).toString('base64')}`,
      'content-type': 'application/x-www-form-urlencoded',
      dpop: await createDpopHeader(tokenUrl, 'POST', dpopKey),
    },
    body: 'grant_type=client_credentials&scope=webid',
  });

  // This is the Access token that will be used to do an authenticated request to the server.
  // The JSON also contains an "expires_in" field in seconds,
  // which you can use to know when you need request a new Access token.
  //const  access_token = await response.json();
  const { access_token: accessToken } = await response.json();

  return [accessToken, dpopKey];

}

const runAsyncFunctions = async () => {

    const idInfo = await authenticate();
    const tokenAuth = await getAuthorizationToken(idInfo);
    
    const [token,dpopKey] = await getTokenUsage(tokenAuth[0],tokenAuth[1]);
    
    console.log("Token usage ", token);
    
    console.log("dpopKey here: ", dpopKey);


  }
 
  //runAsyncFunctions()

const getAcl = async () => {
  // Authenticate and retrieve the access token and dpopKey for authorization
  const idInfo = await authenticate();
  const tokenAuth = await getAuthorizationToken(idInfo);
  const [token, dpopKey] = await getTokenUsage(tokenAuth[0], tokenAuth[1]);

  const aclUrl = "https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/.acl";

  try {
    const response = await fetch(aclUrl, {
      method: 'GET',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(aclUrl, 'GET', dpopKey),
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch ACL: ${response.status} ${response.statusText}`);
    }

    // Parse and log the ACL response if successful
    const aclData = await response.text();
    console.log("ACL data:", aclData);
    return aclData;
  } catch (error) {
    console.error("Error fetching ACL:", error);
  }
};

//getAcl()

const createContainer = async (container: string) => {
  const idInfo = await authenticate();
  const tokenAuth = await getAuthorizationToken(idInfo);
  const [token, dpopKey] = await getTokenUsage(tokenAuth[0], tokenAuth[1]);
  const url = podUrl + container + "/";
  try {
    const response = await fetch(url, {
      method: 'PUT',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(url, 'PUT', dpopKey),
      },
    });

    if (!response.ok) {
      console.error(response.statusText);
    }
  } catch (error) {
    console.error("failed to create container " + container);
  }
};

const createContainer_secondAccount = async (container: string) => {
  const idInfo = await authenticate_secondAccount();
  const tokenAuth = await getAuthorizationToken_secondAccount(idInfo);
  const [token, dpopKey] = await getTokenUsage_secondAccount(tokenAuth[0], tokenAuth[1]);
  const url = podUrl_secondAccount + container + "/";
  try {
    const response = await fetch(url, {
      method: 'PUT',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(url, 'PUT', dpopKey),
      },
    });

    if (!response.ok) {
      console.error(response.statusText);
    }
  } catch (error) {
    console.error("failed to create container " + container);
  }
};


const addResourceToPod = async (container: string, fileName: string, resource: any, filetype: string) => {
  // Authenticate and retrieve the access token and dpopKey for authorization
  const idInfo = await authenticate();
  const tokenAuth = await getAuthorizationToken(idInfo);
  const [token, dpopKey] = await getTokenUsage(tokenAuth[0], tokenAuth[1]);
  const url = podUrl + container + "/" + fileName;

  try {
    const response = await fetch(url, {
      method: 'PUT',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(url, 'PUT', dpopKey),
        'content-type': filetype,
      },
      body: resource
    });

    if (!response.ok) {
      console.error("failed to add Resource " + response.statusText);
    }
  } catch (error) {
    console.error("failed to add Resource ");
  }
};

const addResourceToPod_secondAccount = async (container: string, fileName: string, resource: any, filetype: string) => {
  // Authenticate and retrieve the access token and dpopKey for authorization
  const idInfo = await authenticate_secondAccount();
  const tokenAuth = await getAuthorizationToken_secondAccount(idInfo);
  const [token, dpopKey] = await getTokenUsage_secondAccount(tokenAuth[0], tokenAuth[1]);
  const url = podUrl_secondAccount + container + "/" + fileName;

  try {
    const response = await fetch(url, {
      method: 'PUT',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(url, 'PUT', dpopKey),
        'content-type': filetype,
      },
      body: resource
    });

    if (!response.ok) {
      console.error("failed to add Resource " + response.statusText);
    }
  } catch (error) {
    console.error("failed to add Resource ");
  }
};

const getResource = async (containerName: string, resourceName: string) => {
  // Authenticate and retrieve the access token and dpopKey for authorization
  const idInfo = await authenticate();
  const tokenAuth = await getAuthorizationToken(idInfo);
  const [token, dpopKey] = await getTokenUsage(tokenAuth[0], tokenAuth[1]);

  const url = podUrl + containerName + "/" + resourceName;

  try {
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(url, 'GET', dpopKey),
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch ACL: ${response.status} ${response.statusText}`);
    }

    // Parse and log the ACL response if successful
    const resource = await response.text();
    console.log("resource data:", resource);
    return resource;
  } catch (error) {
    console.error("Error fetching resource:", error);
  }
};

const getResourceFromSecondAccount = async (containerName: string, resourceName: string) => {
  // Authenticate and retrieve the access token and dpopKey for authorization
  const idInfo = await authenticate_secondAccount();
  const tokenAuth = await getAuthorizationToken_secondAccount(idInfo);
  const [token, dpopKey] = await getTokenUsage_secondAccount(tokenAuth[0], tokenAuth[1]);

  const url = podUrl + containerName + "/" + resourceName;

  try {
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(url, 'GET', dpopKey),
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch ACL: ${response.status} ${response.statusText}`);
    }

    // Parse and log the ACL response if successful
    const resource = await response.text();
    console.log("resource data:", resource);
    return resource;
  } catch (error) {
    console.error("Error fetching resource:", error);
  }
};

const addAclAuthorizationToTest = async () => {
  try {
    const idInfo = await authenticate();
    const tokenAuth = await getAuthorizationToken(idInfo);
    const [token, dpopKey] = await getTokenUsage(tokenAuth[0], tokenAuth[1]);

    const aclUrl = podUrl + "test/" + ".acl";
    const sparqlUpdate = `
     PREFIX acl: <http://www.w3.org/ns/auth/acl#>

      INSERT DATA {
      <#auth2DavidsPosRaffael>
  a acl:Authorization;
  acl:accessTo <https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/test/myhobbies.txt>;
  acl:mode
    acl:Read, acl:write;
  acl:agent <https://wiser-solid-xi.interactions.ics.unisg.ch/raffael_ubicomp24/profile/card#me>,
            <mailto:raffael.rot@student.unisg.ch>;
  acl:default <https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/test/>.
      }`

    const response = await fetch(aclUrl, {
      method: 'PATCH',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(aclUrl, 'PATCH', dpopKey),
        'Content-Type': 'application/sparql-update',
      },
      body: sparqlUpdate
    });

    if (!response.ok) {
      const errorBody = await response.text();
      throw new Error(`Failed to patch ACL: ${response.status} ${response.statusText} - ${errorBody}`);
    }

    console.log(`Successfully added ACL rule to ${aclUrl}`);
  } catch (error) {
    console.error(`Failed to add ACL rule: ${(error as Error).message}`);
  }
};

const updateProfileCardToAddOccupation = async () => {
  try {
    const idInfo = await authenticate();
    const tokenAuth = await getAuthorizationToken(idInfo);
    const [token, dpopKey] = await getTokenUsage(tokenAuth[0], tokenAuth[1]);

    const profileCardUrl = podUrl + "profile/card";

    const sparqlUpdate = `
      PREFIX : <https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/profile/card#>
      PREFIX unisg: <https://ics.unisg.ch#>

      INSERT DATA {
        :me unisg:hasOccupation unisg:technician .
      }`;

    const response = await fetch(profileCardUrl, {
      method: 'PATCH',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(profileCardUrl, 'PATCH', dpopKey),
        'Content-Type': 'application/sparql-update',
      },
      body: sparqlUpdate,
    });

    if (!response.ok) {
      const errorBody = await response.text();
      throw new Error(`Failed to update profile card: ${response.status} ${response.statusText} - ${errorBody}`);
    }
  } catch (error) {
    console.error(`Failed to update profile card: ${(error as Error).message}`);
  }
};


const addRawGazeData = async (gazeDataFileName: string, resource: any) => {
  // Authenticate and retrieve the access token and dpopKey for authorization
  const idInfo = await authenticate();
  const tokenAuth = await getAuthorizationToken(idInfo);
  const [token, dpopKey] = await getTokenUsage(tokenAuth[0], tokenAuth[1]);
  const url = podUrl + "gazeData/" + gazeDataFileName;

  try {
    const response = await fetch(url, {
      method: 'PUT',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(url, 'PUT', dpopKey),
        'content-type': "text/csv",
      },
      body: resource
    });

    if (!response.ok) {
      console.error("failed to add Resource " + response.statusText);
    }
  } catch (error) {
    console.error("failed to add Resource ");
  }
};

const addRawGazeData_secondAccount = async (gazeDataFileName: string, resource: any) => {
  // Authenticate and retrieve the access token and dpopKey for authorization
  const idInfo = await authenticate_secondAccount();
  const tokenAuth = await getAuthorizationToken_secondAccount(idInfo);
  const [token, dpopKey] = await getTokenUsage_secondAccount(tokenAuth[0], tokenAuth[1]);
  const url = podUrl_secondAccount + "gazeData/" + gazeDataFileName;

  try {
    const response = await fetch(url, {
      method: 'PUT',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(url, 'PUT', dpopKey),
        'content-type': "text/csv",
      },
      body: resource
    });

    if (!response.ok) {
      console.error("failed to add Resource " + response.statusText);
    }
  } catch (error) {
    console.error("failed to add Resource ");
  }
};

const addCurrentActivity = async (activity: string, probability: number, endTime: Date, rawData: string) => {
  // Authenticate and retrieve the access token and dpopKey for authorization
  const idInfo = await authenticate();
  const tokenAuth = await getAuthorizationToken(idInfo);
  const [token, dpopKey] = await getTokenUsage(tokenAuth[0], tokenAuth[1]);
  const url = podUrl + "gazeData/currentActivity.ttl";

  const body = '@prefix xsd:  <http://www.w3.org/2001/XMLSchema#> .\n' +
      '@prefix foaf: <http://xmlns.com/foaf/0.1/> .\n' +
      '@prefix prov: <http://www.w3.org/ns/prov#> .\n' +
      '@prefix schema: <https://schema.org/> .\n' +
      '@prefix bm: <http://bimerr.iot.linkeddata.es/def/occupancy-profile#> .\n' +
      '\n' +
      '<https://solid.interactions.ics.unisg.ch/Davids-Pod/gazeData/currentActivity.ttl> a prov:Activity, schema:'+ activity + ';\n' +
      '                                                                              schema:name "' + activity +'"^^xsd:string;\n' +
      '                                                                              prov:wasAssociatedWith <https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/profile/card#me>;\n' +
      '                                                                              prov:used <https://solid.interactions.ics.unisg.ch/Davids-Pod/gazeData/' + rawData + '>;\n' +
      '                                                                              prov:endedAtTime "' + endTime.toISOString() + '"^^xsd:dateTime;\n' +
      '                                                                              bm:probability  "'+ probability + '"^^xsd:float.\n' +
      '<https://solid.interactions.ics.unisg.ch/Davids-Pod/profile/card#me> a foaf:Person, prov:Agent;\n' +
      '                                                                 foaf:name "David Seger";\n' +
      '                                                                 foaf:mbox <mailto:david.seger@student.unisg.ch>.'
  try {
    const response = await fetch(url, {
      method: 'PUT',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(url, 'PUT', dpopKey),
        'content-type': "text/turtle",
      },
      body: body
    });

    if (!response.ok) {
      console.error("failed to add Resource " + response.statusText);
    }
  } catch (error) {
    console.error("failed to add Resource ");
  }
};

const addCurrentActivity_SecondAccount = async (activity: string, probability: number, endTime: Date, rawData: string) => {
  // Authenticate and retrieve the access token and dpopKey for authorization
  const idInfo = await authenticate_secondAccount();
  const tokenAuth = await getAuthorizationToken_secondAccount(idInfo);
  const [token, dpopKey] = await getTokenUsage_secondAccount(tokenAuth[0], tokenAuth[1]);
  const url = podUrl_secondAccount + "gazeData/currentActivity.ttl";

  const body = '@prefix xsd:  <http://www.w3.org/2001/XMLSchema#> .\n' +
      '@prefix foaf: <http://xmlns.com/foaf/0.1/> .\n' +
      '@prefix prov: <http://www.w3.org/ns/prov#> .\n' +
      '@prefix schema: <https://schema.org/> .\n' +
      '@prefix bm: <http://bimerr.iot.linkeddata.es/def/occupancy-profile#> .\n' +
      '\n' +
      '<https://solid.interactions.ics.unisg.ch/Kirk/gazeData/currentActivity.ttl> a prov:Activity, schema:'+ activity + ';\n' +
      '                                                                              schema:name "' + activity +'"^^xsd:string;\n' +
      '                                                                              prov:wasAssociatedWith <https://wiser-solid-xi.interactions.ics.unisg.ch/Kirk/profile/card#me>;\n' +
      '                                                                              prov:used <https://solid.interactions.ics.unisg.ch/Kirk/gazeData/' + rawData + '>;\n' +
      '                                                                              prov:endedAtTime "' + endTime.toISOString() + '"^^xsd:dateTime;\n' +
      '                                                                              bm:probability  "'+ probability + '"^^xsd:float.\n' +
      '<https://solid.interactions.ics.unisg.ch/Kirk/profile/card#me> a foaf:Person, prov:Agent;\n' +
      '                                                                 foaf:name "James T. Kirk";\n' +
      '                                                                 foaf:mbox <mailto:JamesT.Kirk@starfleet.uss>.'
  try {
    const response = await fetch(url, {
      method: 'PUT',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(url, 'PUT', dpopKey),
        'content-type': "text/turtle",
      },
      body: body
    });

    if (!response.ok) {
      console.error("failed to add Resource " + response.statusText);
    }
  } catch (error) {
    console.error("failed to add Resource ");
  }
};

const queryRobot = async () => {
  const QueryEngine = require('@comunica/query-sparql').QueryEngine;

  const myEngine = new QueryEngine();

  const bindingsStream = await myEngine.queryBindings(`
        PREFIX assg3: <https://ics.unisg.ch#>
        PREFIX schema: <https://schema.org/>
        PREFIX : <https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/profile/card#>
        PREFIX dbo: <https://dbpedia.org/ontology/>
        PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#> 
        PREFIX prov: <http://www.w3.org/ns/prov#> 
        
        SELECT ?action WHERE {
             :me assg3:hasOccupation ?occupation .
             ?occupation assg3:performs ?action .
             ?action assg3:supportMaterial ?supportMaterial .
             ?supportMaterial rdfs:comment ?comment .
        }
  `, {
    sources: ['https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/profile/card#me',
              'https://wiser-solid-xi.interactions.ics.unisg.ch/robotSG/operations/classifiedActivitiesMaterial.ttl',
              'https://dbpedia.org/sparql']
  });

  bindingsStream.on('data', (binding) => {
    console.log(binding.get("action").value);
  });
};

const addAclAuthorizationTogaze = async () => {
  try {
    const idInfo = await authenticate();
    const tokenAuth = await getAuthorizationToken(idInfo);
    const [token, dpopKey] = await getTokenUsage(tokenAuth[0], tokenAuth[1]);

    const aclUrl = podUrl + "gazeData/" + ".acl";
    const sparqlUpdate = `
     PREFIX acl: <http://www.w3.org/ns/auth/acl#>

      INSERT DATA {
      <#auth2DavidsPoDKaiV4>
  a acl:Authorization;
  acl:accessTo <https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/gazeData/currentActivity.ttl>;
  acl:mode
    acl:Read;
  acl:agent <https://wiser-solid-xi.interactions.ics.unisg.ch/kai2_ubicomp24/profile/card#me>,
            <mailto:kai.schultz@student.unisg.ch>;
  acl:default <https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/gazeData/>.
      }`

    const response = await fetch(aclUrl, {
      method: 'PATCH',
      headers: {
        authorization: `DPoP ${token}`,
        dpop: await createDpopHeader(aclUrl, 'PATCH', dpopKey),
        'Content-Type': 'application/sparql-update',
      },
      body: sparqlUpdate
    });

    if (!response.ok) {
      const errorBody = await response.text();
      throw new Error(`Failed to patch ACL: ${response.status} ${response.statusText} - ${errorBody}`);
    }

    console.log(`Successfully added ACL rule to ${aclUrl}`);
  } catch (error) {
    console.error(`Failed to add ACL rule: ${(error as Error).message}`);
  }
};


/*
createContainer("test")
createContainer("gazeData")
fs.readFile('myhobbies.txt', 'utf8', function (err, data) {
  addResourceToPod("test", "myhobbies.txt", data, 'text/plain')
});
getResource("test","myhobbies.txt");
fs.readFile('.acl', 'utf8', function (err, data) {
  addResourceToPod("test", ".acl", data, 'text/turtle')
});
fs.readFile('.acl', 'utf8', function (err, data) {
  addResourceToPod("gazeData", ".acl", data, 'text/turtle')
});

getResource("gazeData",".acl");
addAclAuthorizationToTest()
getResource("test",".acl");

fs.readFile('myFamilyInfo.txt', 'utf8', function (err, data) {
  addResourceToPod("", "myFamilyInfo.txt", data, 'text/plain')
});


fs.readFile('myFriendsInfo.txt', 'utf8', function (err, data) {
  addResourceToPod("test", "myFriendsInfo.txt", data, 'text/plain')
});


getResourceFromSecondAccount("test","myhobbies.txt");
getResourceFromSecondAccount("test","myFriendsInfo.txt");
getResourceFromSecondAccount("","myFamilyInfo.txt");


fs.readFile('01_Inspection.csv', 'utf8', function (err, data) {
  addRawGazeData("01_Inspection.csv", data)
});

addCurrentActivity("CheckAction", 0.8, new Date(Date.now()), "01_Inspection.csv")

createContainer_secondAccount("gazeData")

fs.readFile('01_reading.csv', 'utf8', function (err, data) {
  addRawGazeData_secondAccount("01_reading.csv", data)
});

addCurrentActivity("CheckAction", 0.8, new Date(Date.now()), "01_Inspection.csv")
addCurrentActivity_SecondAccount("ReadAction", 0.74, new Date(Date.now()), "01_reading.csv")

fs.readFile('.acl', 'utf8', function (err, data) {
  addResourceToPod_secondAccount("gazeData", ".acl", data, 'text/turtle')
});

updateProfileCardToAddOccupation()
*/
//queryRobot()
addAclAuthorizationTogaze()

//getResource("gazeData", "currentActivity.ttl")

