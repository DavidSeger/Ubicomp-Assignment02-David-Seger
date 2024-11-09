// server.ts

import express from 'express';
import bodyParser from 'body-parser';
import { createDpopHeader, generateDpopKeyPair } from '@inrupt/solid-client-authn-core';
import fetch from 'cross-fetch';
import * as fs from 'fs';
import { Buffer } from 'buffer';

// Import the Comunica query engine
import { QueryEngine } from '@comunica/query-sparql';

const app = express();
app.use(bodyParser.json());

// Constants and URLs
const podUrl = "https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/";
const podUrlKai = "https://wiser-solid-xi.interactions.ics.unisg.ch/kai_ubicomp24/";
const podUrl_secondAccount = "https://wiser-solid-xi.interactions.ics.unisg.ch/Kirk/";
const accountApiUrl = 'https://wiser-solid-xi.interactions.ics.unisg.ch/.account/';
const tokenUrl = 'https://wiser-solid-xi.interactions.ics.unisg.ch/.oidc/token';

// User credentials (Replace with your actual credentials or use environment variables)
const userCredentials = {
    email: 'david.seger@adon.li',
    password: 'UbiComp24',
    webId: 'https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/profile/card#me',
};

const secondUserCredentials = {
    email: 'david.seger@student.unisg.ch',
    password: 'WAS2023',
    webId: 'https://wiser-solid-xi.interactions.ics.unisg.ch/Kirk/profile/card#me',
};

// Authentication Functions
const authenticate = async (email: string, password: string): Promise<string> => {
    const indexResponse = await fetch(accountApiUrl);
    const { controls } = await indexResponse.json();

    const response = await fetch(controls.password.login, {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ email, password }),
    });

    const { authorization } = await response.json();
    return authorization;
};

const getAuthorizationToken = async (authorization: string, webId: string): Promise<{ id: string; secret: string; resource: string }> => {
    const indexResponse = await fetch(accountApiUrl, {
        headers: { authorization: `CSS-Account-Token ${authorization}` },
    });
    const { controls } = await indexResponse.json();

    const response = await fetch(controls.account.clientCredentials, {
        method: 'POST',
        headers: { authorization: `CSS-Account-Token ${authorization}`, 'content-type': 'application/json' },
        body: JSON.stringify({ name: 'my-token', webId }),
    });

    const { id, secret, resource } = await response.json();
    return { id, secret, resource };
};

const getTokenUsage = async (id: string, secret: string): Promise<{ accessToken: string; dpopKey: any }> => {
    const dpopKey = await generateDpopKeyPair();
    const authString = `${encodeURIComponent(id)}:${encodeURIComponent(secret)}`;

    const response = await fetch(tokenUrl, {
        method: 'POST',
        headers: {
            authorization: `Basic ${Buffer.from(authString).toString('base64')}`,
            'content-type': 'application/x-www-form-urlencoded',
            dpop: await createDpopHeader(tokenUrl, 'POST', dpopKey),
        },
        body: 'grant_type=client_credentials&scope=webid',
    });

    const { access_token: accessToken } = await response.json();
    return { accessToken, dpopKey };
};

// Solid Pod Interaction Functions
const createContainer = async (podUrl: string, container: string, credentials: any): Promise<void> => {
    const authorization = await authenticate(credentials.email, credentials.password);
    const tokenAuth = await getAuthorizationToken(authorization, credentials.webId);
    const { accessToken, dpopKey } = await getTokenUsage(tokenAuth.id, tokenAuth.secret);
    const url = `${podUrl}${container}/`;

    const response = await fetch(url, {
        method: 'PUT',
        headers: {
            authorization: `DPoP ${accessToken}`,
            dpop: await createDpopHeader(url, 'PUT', dpopKey),
        },
    });

    if (!response.ok) {
        throw new Error(`Failed to create container: ${response.statusText}`);
    }
};

const addResourceToPod = async (
    podUrl: string,
    container: string,
    fileName: string,
    resource: any,
    fileType: string,
    credentials: any
): Promise<void> => {
    const authorization = await authenticate(credentials.email, credentials.password);
    const tokenAuth = await getAuthorizationToken(authorization, credentials.webId);
    const { accessToken, dpopKey } = await getTokenUsage(tokenAuth.id, tokenAuth.secret);
    const url = `${podUrl}${container}/${fileName}`;

    const response = await fetch(url, {
        method: 'PUT',
        headers: {
            authorization: `DPoP ${accessToken}`,
            dpop: await createDpopHeader(url, 'PUT', dpopKey),
            'content-type': fileType,
        },
        body: resource,
    });

    if (!response.ok) {
        throw new Error(`Failed to add resource: ${response.statusText}`);
    }
};

const getResource = async (podUrl: string, containerName: string, resourceName: string, credentials: any): Promise<any> => {
    const authorization = await authenticate(credentials.email, credentials.password);
    const tokenAuth = await getAuthorizationToken(authorization, credentials.webId);
    const { accessToken, dpopKey } = await getTokenUsage(tokenAuth.id, tokenAuth.secret);

    const url = `${podUrl}${containerName}/${resourceName}`;

    const response = await fetch(url, {
        method: 'GET',
        headers: {
            authorization: `DPoP ${accessToken}`,
            dpop: await createDpopHeader(url, 'GET', dpopKey),
        },
    });

    if (!response.ok) {
        throw new Error(`Failed to fetch resource: ${response.statusText}`);
    }

    const resource = await response.text();
    return resource;
};

const queryRobot = async (action: string) => {
    const QueryEngine = require('@comunica/query-sparql').QueryEngine;

    const myEngine = new QueryEngine();

    const bindingsStream = await myEngine.queryBindings(`
        PREFIX assg3: <https://ics.unisg.ch#>
        PREFIX schema: <https://schema.org/>
        PREFIX : <https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/profile/card#>
        PREFIX dbo: <https://dbpedia.org/ontology/>
        PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#> 
        PREFIX prov: <http://www.w3.org/ns/prov#> 
        
        SELECT ?comment WHERE {
             :me assg3:hasOccupation ?occupation .
             ?occupation assg3:performs ` + action +` .
             ` + action +` assg3:supportMaterial ?supportMaterial .
             ?supportMaterial rdfs:comment ?comment .
             FILTER (lang(?comment) = 'en')
        }
  `, {
        sources: ['https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/profile/card#me',
            'https://wiser-solid-xi.interactions.ics.unisg.ch/robotSG/operations/classifiedActivitiesMaterial.ttl',
            'https://dbpedia.org/sparql']
    });

    return new Promise((resolve, reject) => {
        const results = [];
        bindingsStream.on('data', (binding) => {
            results.push(binding.get('comment').value);
        });

        bindingsStream.on('end', () => {
            resolve(results);
        });

        bindingsStream.on('error', (error) => {
            reject(error);
        });
    });
};

const addAclAuthorizationTogaze = async (username: string) => {
    try {
        const authorization = await authenticate(userCredentials.email, userCredentials.password);
        const tokenAuth = await getAuthorizationToken(authorization, userCredentials.webId);
        const { accessToken, dpopKey } = await getTokenUsage(tokenAuth.id, tokenAuth.secret);

        const aclUrl = podUrl + "gazeData/" + ".acl";
        const sparqlUpdate = `
     PREFIX acl: <http://www.w3.org/ns/auth/acl#>

      INSERT DATA {
      <#auth2DavidsPoDKaiV3>
  a acl:Authorization;
  acl:accessTo <https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/gazeData/currentActivity.ttl>;
  acl:mode
    acl:Read;
  acl:agent <https://wiser-solid-xi.interactions.ics.unisg.ch/` + username +`/profile/card#me>;
  acl:default <https://wiser-solid-xi.interactions.ics.unisg.ch/Davids-Pod/gazeData/>.
      }`

        const response = await fetch(aclUrl, {
            method: 'PATCH',
            headers: {
                authorization: `DPoP ${accessToken}`,
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


app.post('/create-container', async (req, res) => {
    try {
        const { containerName } = req.body;
        await createContainer(podUrl, containerName, userCredentials);
        res.status(201).send(`Container "${containerName}" created successfully.`);
    } catch (error) {
        res.status(500).send((error as Error).message);
    }
});

app.post('/add-resource', async (req, res) => {
    try {
        const { container, fileName, resourceContent, fileType } = req.body;
        await addResourceToPod(podUrl, container, fileName, resourceContent, fileType, userCredentials);
        res.status(201).send(`Resource "${fileName}" added to container "${container}" successfully.`);
    } catch (error) {
        res.status(500).send((error as Error).message);
    }
});

app.post('/authorize-user-gaze-data', async (req, res) => {
    try {
        const { username } = req.query;
        await addAclAuthorizationTogaze(username);
        res.status(201).send(`success`);
    } catch (error) {
        res.status(500).send((error as Error).message);
    }
});

app.get('/get-resource', async (req, res) => {
    try {
        const { containerName, resourceName } = req.query;
        const resource = await getResource(podUrl, containerName as string, resourceName as string, userCredentials);
        res.status(200).send(resource);
    } catch (error) {
        res.status(500).send((error as Error).message);
    }
});

app.get('/get-resource-kai', async (req, res) => {
    try {
        const { containerName, resourceName } = req.query;
        const resource = await getResource(podUrlKai, containerName as string, resourceName as string, userCredentials);
        res.status(200).send(resource);
    } catch (error) {
        res.status(500).send((error as Error).message);
    }
});

app.get('/query-robot', async (req, res) => {
    try {
        let { action } = req.query;
        switch (action) {
            case "READ":
                action = "<https://ics.unisg.ch#readTechnician>"
                break;
            case "INSPECT":
                action = "<https://ics.unisg.ch#inspectTechnician>"
        }
        const result = await queryRobot(action);
        res.status(200).json({"comments" : result});
    } catch (error) {
        console.error('Error executing query:', error);
        res.status(500).send((error as Error).message);
    }
});


const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`REST server is running on port ${PORT}`);
});
