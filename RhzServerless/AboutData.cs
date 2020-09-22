using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;

using Rhz.Domains.Models;
using System.Linq;

namespace RhzServerless
{
    public static class AboutData
    {
        [FunctionName("AboutData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "about")] HttpRequest req,
            [Table(RhzStorageTools.siteDisplayName, Connection = "AzureWebJobsStorage")] CloudTable siteDisplayTable,
            IBinder binder,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var interestingLinksQuery = RhzStorageTools.GenerateContentQuery<LinkContent>(RhzStorageTools.interestingLinksPk);
            var dotnetLinksQuery = RhzStorageTools.GenerateContentQuery<LinkContent>(RhzStorageTools.dotNetLinksPk);
            var mailStatusQuery = RhzStorageTools.GenerateContentQuery<LinkContent>(RhzStorageTools.mailStatusPk);
            var getAboutOp = TableOperation.Retrieve<DisplayContent>(RhzStorageTools.aboutPk, RhzStorageTools.aboutRk);

            var iLinkSegment = await siteDisplayTable.ExecuteQuerySegmentedAsync(interestingLinksQuery, null).ConfigureAwait(false);
            var dLinkSegment = await siteDisplayTable.ExecuteQuerySegmentedAsync(dotnetLinksQuery, null).ConfigureAwait(false);
            var mailstatusSegment = await siteDisplayTable.ExecuteQuerySegmentedAsync(mailStatusQuery, null).ConfigureAwait(false);
            var aboutData = await siteDisplayTable.ExecuteAsync(getAboutOp).ConfigureAwait(false);

            if (aboutData.Result == null)
            {
                return new NotFoundResult();
            }
            var aboutDisplay = (DisplayContent)aboutData.Result;

            var hvm = new BasicContentViewModel
            {
                RequestPath = req?.Path.Value
            };

            var aboutText = string.Empty;
            if (aboutDisplay != null && aboutDisplay.Published)
            {
                aboutText = await RhzStorageTools.ReadTextFromBlob(binder, RhzStorageTools.siteCopyName, aboutDisplay.BlobName, "AzureWebJobsStorage").ConfigureAwait(false);
            }
            else
            {
                aboutText = aboutDisplay.HtmlContent ?? string.Empty;
            }

            hvm.Lists.Add(RhzStorageTools.interestingLinksPk, iLinkSegment.Where(lc => lc.Published).Select(lc =>
            new LinkContentDto
            {
                Caption = lc.Caption,
                Target = lc.Target,
                Url = lc.Url
            }));
            hvm.Lists.Add(RhzStorageTools.dotNetLinksPk, dLinkSegment.Where(lc => lc.Published).Select(lc =>
            new LinkContentDto
            {
                Caption = lc.Caption,
                Target = lc.Target,
                Url = lc.Url
            }));
            hvm.Lists.Add(RhzStorageTools.mailStatusPk, mailstatusSegment.Where(lc => lc.Published).Select(lc =>
            new LinkContentDto
            {
                Caption = lc.Caption,
                Target = lc.Target,
                Url = lc.Url
            }));
            hvm.Content.Add(RhzStorageTools.aboutPk, aboutText);

            return (aboutDisplay != null)
               ? (ActionResult)new OkObjectResult(hvm)
               : new NotFoundResult();

        }
    }
}
