using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Rhz.Domains.Models;

namespace RhzServerless
{
    public static class IndexData
    {
        [FunctionName("IndexData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "index")] HttpRequest req,
            [Table(RhzStorageTools.siteDisplayName, RhzStorageTools.heroPk, RhzStorageTools.heroRk, Connection = "AzureWebJobsStorage")] DisplayContent heroDisplay,
            [Table(RhzStorageTools.siteDisplayName, RhzStorageTools.skillPk, RhzStorageTools.skillRk, Connection = "AzureWebJobsStorage")] DisplayContent skillsDisplay,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var hvm = new BasicContentViewModel();
            hvm.RequestPath = req?.Path.Value;

            var client = RhzStorageTools.GetBlobClient();
            var heroText = string.Empty;
            var skillText = string.Empty;
            if (heroDisplay != null && heroDisplay.Published)
            {
                heroText = await RhzStorageTools.GetTextFromBlob(client, RhzStorageTools.siteCopyName, heroDisplay.BlobName, heroDisplay.HtmlContent).ConfigureAwait(false);
            }
            if (skillsDisplay != null && skillsDisplay.Published)
            {
                skillText = await RhzStorageTools.GetTextFromBlob(client, RhzStorageTools.siteCopyName, skillsDisplay.BlobName, skillsDisplay.HtmlContent).ConfigureAwait(false);
            }

            hvm.Content.Add(RhzStorageTools.heroPk, heroText);
            hvm.Content.Add(RhzStorageTools.skillPk, skillText);
            return (heroDisplay != null || skillsDisplay != null)
               ? (ActionResult)new OkObjectResult(hvm)
               : new NotFoundResult();
        }
    }
}
