# DefaultAzureIdentity and its various credential types

Modern applications consist of lots of independent components. Microservice architecture brings great benefits but it also has its downsides. Developers must take care of communication between various parts of the system and make it secure and authenticated. One of the preferred ways is to give your component identity from Azure Active Directory (AAD) and utilize use of AAD tokens. This demo shows various ways how to retrieve identity from application context using single line of code and get sample secret from Azure Key Vault. This all is done with the help of [DefaultAzureCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet) class from [Azure.Identity NuGet package](https://www.nuget.org/packages/Azure.Identity).

### Prerequisites

We will create instance of Azure Key vault. As second step, we insert value `supersecurevalue` as secret with key `mylittlesecret`.  This all is done with the help of [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/get-started-with-azure-cli).

```
az keyvault create --location westeurope --name azureidentityvault --resource-group identitytest
az keyvault secret set --name mylittlesecret --value supersecurevalue --vault-name azureidentityvault
```

### DefaultAzureCredential

There are various identities we want use for our application during different stages of development cycle. For example, one for development, one for integration testing and one for production. For sure we don’t want to have separate code section for each environment. Azure.Identity NuGet package makes retrieving identity unified. Following example retrieves our secret from created Key vault (uses C# 9 and [top level statements](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9#top-level-statements) feature).

```csharp
using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

const string keyvaultName = "azureidentityvault";
const string secretKey = "mylittlesecret";

var credential = new DefaultAzureCredential();

var client = new SecretClient(new Uri($"https://{keyvaultName}.vault.azure.net/"), credential);
KeyVaultSecret secret = client.GetSecret(secretKey);
Console.WriteLine(secret.Value);
```

**[DefaultAzureCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet) combines some classes, that are used to retrieve AAD identity. It tries to initialize them on by one (in this order). The first successfully initialized credential is used.**

1. [EnvironmentCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.environmentcredential?view=azure-dotnet)
2. [ManagedIdentityCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.managedidentitycredential?view=azure-dotnet)
3. [SharedTokenCacheCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.sharedtokencachecredential?view=azure-dotnet)
4. [VisualStudioCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.visualstudiocredential?view=azure-dotnet)
5. [VisualStudioCodeCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.visualstudiocodecredential?view=azure-dotnet)
6. [AzureCliCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.azureclicredential?view=azure-dotnet)
7. [InteractiveBrowserCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.interactivebrowsercredential?view=azure-dotnet)

When your application runs in production environment your identity will be probably retrieved with one of first three classes. When you debug your application locally on the other hand, managed identity or environment variables could not be available. We will talk about each of these types of credentials from bottom to the top in following sections.

### InteractiveBrowserCredential

This type of credentials opens default browser and lets the user to do interactive sign in. If you enter credentials of account, that created key vault, you should see the secret.  Retrieve credentials using this code: 

```csharp
var credential = new InteractiveBrowserCredential();
```

Keep in mind, that [DefaultAzureCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet) exclude interactive login by default. If you want to use it, you have to initialize it with includeInteractiveCredentials option set to true.

```csharp
var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
```

### AzureCliCredential

```csharp
var credential = new AzureCliCredential();
```

If you are in terminal environment, you can log to Azure CLI using *az login* command. Application running in the same terminal will use identity provided during login.

### VisualStudioCodeCredential

```csharp
var credential = new new VisualStudioCodeCredential();
```

Azure Visual studio extensions required you to be logged in to show your Azure resources. You need [Azure account extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.azure-account) for this purpose. It providers various commands how to perform sign in. Just hit F1 and start typing "Azure Sign In". VisualStudioCodeCredential takes this identity and uses it for identity of our application during runtime.

![visual studio code identity](https://github.com/mirecad/Azure.Identity.Demo/blob/master/Images/identityCode.jpg?raw=true)


### VisualStudioCredential

```csharp
var credential = new new VisualStudioCredential();
```

This option is very similar to the previous one. It differs only in the IDE and the way of providing credentials to it. In the "big" Visual studio you find the login form in Tools > Options > Azure service authentication.

![visual studio identity](https://github.com/mirecad/Azure.Identity.Demo/blob/master/Images/identityStudio.jpg?raw=true)


### SharedTokenCacheCredential

```csharp
var credential = new new SharedTokenCacheCredential();
```

Many Microsoft applications use Azure single sign on. This class uses identity, that was already stored in local cache by one of them.

### ManagedIdentityCredential

```csharp
var credential = new ManagedIdentityCredential();
```
Managed identity is great way how to secure your service in production. Application will receive identity managed by Azure itself. You do not even have access to credentials. In this case administrators can use role-based access control to set up permissions for other resources.

We can demonstrate this by creating simple HTTP based azure function. First create storage and the function app itself.

```
az storage account create --name identityfunctionstorage --resource-group identitytest
az functionapp create --name identityfunctiondemo --resource-group identitytest --storage-account identityfunctionstorage --consumption-plan-location westeurope
```

Then we command Azure to assign managed identity for our Azure function (response is just for illustration).

```
az functionapp identity assign --name identityfunctiondemo --resource-group mirotest

{
  "principalId": "3fedf722-7c5d-426f-9d35-d985d3eb59bc",
  "tenantId": "8d099a24-312e-4bb5-8fe4-aed67b7c4921",
  "type": "SystemAssigned",
  "userAssignedIdentities": null
}
```

Our application now has Azure identity with ID *3fedf722-7c5d-426f-9d35-d985d3eb59bc*. Last configuration step is to add permission for our newly created application to be able to retrieve secret from our Key vault.

```
az keyvault set-policy --name azureidentityvault --object-id 3fedf722-7c5d-426f-9d35-d985d3eb59bc --secret-permission get
```

Code of function app is in folder Azure.Identity.Demo.Function of this repository. After successful deployment you will see the Invoke url. Enter it in browser and you will see value of the secret as a response.

```
cd Azure.Identity.Demo.Fumction
func azure functionapp publish identityfunctiondemo

Microsoft (R) Build Engine version 16.8.0+126527ff1 for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  All projects are up-to-date for restore.
  Azure_Identity_Demo_Function -> D:\working\Azure.Identity.Demo.Function\bin\publish\bin\Azure_Identity_Demo_Function.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.22


Getting site publishing info...
Creating archive for current directory...
Uploading 4,06 MB [###############################################################################]
Upload completed successfully.
Deployment completed successfully.
Syncing triggers...
Functions in identityfunctiondemo:
    IdentityHttpFunction - [httpTrigger]
        Invoke url: https://identityfunctiondemo.azurewebsites.net/api/identityhttpfunction?code=QOLVCOC0FNtMIgN5bRur4sQSoEXkGraUovGmcsnULKPBiHuJXVKQwg==
```

### EnvironmentCredential

```csharp
var credential = new EnvironmentCredential();
```

The most universal way of providing Azure identity for your application is to use system environment variables. You can configure them in your virtual machine, in your build server, in your cloud hosting, pass them into your docker image and many other places. You can choose between using regular user account or application credential secured by secret or certificate.

ENVIRONMENT VARIABLE | description
------------ | -------------
AZURE_TENANT_ID | The Azure Active Directory tenant(directory) ID.
AZURE_CLIENT_ID | The client(application) ID of an App Registration in the tenant.
AZURE_CLIENT_SECRET | A client secret that was generated for the App Registration.
AZURE_CLIENT_CERTIFICATE_PATH | A path to certificate and private key pair in PEM or PFX format, which can authenticate the App Registration.
AZURE_USERNAME | The username, also known as upn, of an Azure Active Directory user account.
AZURE_PASSWORD | The password of the Azure Active Directory user account. Note this does not support accounts with MFA enabled.

### Conclusion

DefaultAzureCredential class make everyday life of developers much easier. By typing single line of code, we can provide unified solution for providing identity. It adapts well to various environments starting from local debugging in IDE, continuing with build runners and ending up in production cloud hosting.