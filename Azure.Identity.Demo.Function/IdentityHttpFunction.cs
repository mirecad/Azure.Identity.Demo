using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Azure.Identity.Demo.Function
{
    public static class IdentityHttpFunction
    {
        const string KeyVaultName = "azureidentityvault";
        const string SecretKey = "mylittlesecret";

        [FunctionName("IdentityHttpFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try 
            {
                var credential = new DefaultAzureCredential();
                
                var client = new SecretClient(new Uri($"https://{KeyVaultName}.vault.azure.net/"), credential);
                KeyVaultSecret secret = client.GetSecret("mylittlesecret");
                return (ActionResult)new OkObjectResult(secret.Value);
            }
            catch(Exception ex) 
            {
                return (ActionResult)new ExceptionResult(ex, true);
            }          
        }
    }
}
