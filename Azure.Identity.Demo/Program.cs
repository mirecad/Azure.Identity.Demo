using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

const string KeyVaultName = "azureidentityvault";
const string SecretKey = "mylittlesecret";

//var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
var credential = new DefaultAzureCredential();

var client = new SecretClient(new Uri($"https://{KeyVaultName}.vault.azure.net/"), credential);
KeyVaultSecret secret = client.GetSecret("mylittlesecret");
Console.WriteLine(secret.Value);