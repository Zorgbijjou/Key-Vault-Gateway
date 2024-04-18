# # Azure Key Vault API for Nuts-Node

## Overview
This repository contains a lightweight API designed to interface between the [Nuts-Node](https://github.com/nuts-foundation/nuts-node) application and Azure Key Vault. The API ensures secure handling of credentials and secrets needed by Nuts-Node for its operations.

## Features
- **Secure Storage**: Leverage Azure Key Vault for secure storage and management of secrets.

## Prerequisites
Before you begin, ensure you have the following:
- An Azure account with an active subscription.
- Access to Azure Key Vault.
- Docker or dotnet

## Installation

Clone the repository:
```bash
git clone https://github.com/Zorgbijjou/Key-Vault-Gateway.git
```

Log in with azure credentials, this is required to connect to the Key Vault backend.
```bash
az login
```

Running locally with docker:
```bash
cd Key-Vault-Gateway
docker buildx build --tag vault --file KeyStoreApi/Dockerfile .
KeyVault__Uri=https://your.vault.azure.net/ docker run --rm -it vault
```

Running locally with dotnet:
``` bash
vim KeyStoreApi/appsettings.Development.json # Configure your kv in here
cd KeyStoreApi
ASPNET_ENVIRONMENT=Development dotnet run 
```

## Running in Azure
When running this software in Azure, keep in mind it has no authentication mechanism built-in. This means it absolutely cannot be exposed to the public internet.

We recommend running it as a sidecar for the Nuts node, using Azure Container Apps or Azure Kubernetes Service. Dockerfile in the repository exposes port 5080.

Bicep to deploy Azure Container Instances might look something like this. Our internal implementation is not directly exposed to the internet, instead, our Nuts node sits inside a private network behind an Azure Application Gateway ingress.

```bicep
resource containerApp 'Microsoft.App/containerapps@2023-08-01-preview' = {
  name: appName
  location: location
  tags: tagList
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${containerIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    environmentId: containerAppEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
        additionalPortMappings: [
          {
            external: true
            targetPort: 8081
          }
        ]
        allowInsecure: false
      }
      registries: [
        {
          identity: containerIdentity.id
          server: containerRegistry.properties.loginServer
        }
      ]
    }
    template: {
      initContainers: [
        {
          name: initContainer.name
          image: initContainer.image
          resources: {
            #disable-next-line BCP036
            cpu: initContainer.cpu
            memory: initContainer.memory
          }
          volumeMounts: nutsVolumes
        }
      ]
      containers: [
        {
          env: vaultEnv
          name: vaultContainer.name
          probes: [
            {
              failureThreshold: 3
              httpGet: {
                path: '/health'
                port: 5080
                scheme: 'HTTP'
              }
              initialDelaySeconds: 30
              timeoutSeconds: 5
              periodSeconds: 10
              successThreshold: 1
            }
          ]
          image: vaultContainer.image
          resources: {
            #disable-next-line BCP036
            cpu: vaultContainer.cpu
            memory: vaultContainer.memory
          }
        }
        {
          env: nutsEnv
          name: nodeContainer.name
          probes: [
            {
              failureThreshold: 3
              httpGet: {
                path: '/status'
                port: 8081
                scheme: 'HTTP'
              }
              initialDelaySeconds: 30
              timeoutSeconds: 5
              periodSeconds: 10
              successThreshold: 1
            }
          ]
          image: nodeContainer.image
          resources: {
            #disable-next-line BCP036
            cpu: nodeContainer.cpu
            memory: nodeContainer.memory
          }
          volumeMounts: nutsVolumes
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
      volumes: [
        {
          name: volumeName
          storageType: 'AzureFile'
          storageName: nutsEnvironment
        }
      ]
    }
  }
  dependsOn: [
    registryReaderRole
    containerIdentity
  ]
}
```