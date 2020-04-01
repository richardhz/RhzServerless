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
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "document/{key}")] HttpRequest req, string key,
            [Table(RhzStorageTools.postsName, Connection = "AzureWebJobsStorage")] CloudTable postsTable,
            IBinder binder,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var hvm = new BasicContentViewModel
            {
                RequestPath = req?.Path.Value
            };

            var getDocOp = TableOperation.Retrieve<PostContent>(RhzStorageTools.techPostPk, key);

            var postData = await postsTable.ExecuteAsync(getDocOp).ConfigureAwait(false);

            if (postData.HttpStatusCode == 200)
            {
                var documentDisplay = (PostContent)postData.Result;
                if (documentDisplay != null && documentDisplay.Published)
                {
                    string documentText = await RhzStorageTools.ReadTextFromBlob(binder, RhzStorageTools.techDocs, documentDisplay.BlobName, "AzureWebJobsStorage").ConfigureAwait(false);
                    hvm.Content.Add("document", documentText);
                    return new OkObjectResult(hvm);
                }

            }
            return new NotFoundResult();
        }
    }
}
