using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Rhz.Domains.Models;

namespace RhzServerless
{
    public static class Document
    {
        [FunctionName("Document")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "documents/{key}")] HttpRequest req, string key,
            IBinder binder,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var hvm = new BasicContentViewModel
            {
                RequestPath = req?.Path.Value
            };

            try
            {
                string documentText = await RhzStorageTools.ReadTextFromBlob(binder, RhzStorageTools.techDocs, key, "AzureWebJobsStorage").ConfigureAwait(false);
                if (documentText == null)
                {
                    throw new System.Exception($"Possible Document name missmatch request id {key} cannot be resolved.");
                }
                hvm.Content.Add("document", documentText);
                return new OkObjectResult(hvm);
            }
            catch (System.Exception ex)
            {
                log.LogInformation($"Data Error: {ex.Message}");
                throw;
            }
        }
    }
}
