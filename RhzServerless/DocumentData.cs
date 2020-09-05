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
    public static class DocumentData
    {
        [FunctionName("DocumentData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "documents")] HttpRequest req,
            [Table(RhzStorageTools.postsName, Connection = "AzureWebJobsStorage")] CloudTable postsTable,
            [Table(RhzStorageTools.siteDisplayName, Connection = "AzureWebJobsStorage")] CloudTable siteDisplayTable,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var getPostsQuery = RhzStorageTools.GenerateContentQuery<PostContent>(RhzStorageTools.techPostPk);


            var interestingLinksQuery = RhzStorageTools.GenerateContentQuery<LinkContent>(RhzStorageTools.interestingLinksPk);
            var dotnetLinksQuery = RhzStorageTools.GenerateContentQuery<LinkContent>(RhzStorageTools.dotNetLinksPk);

            var postListSegment = await postsTable.ExecuteQuerySegmentedAsync(getPostsQuery, null).ConfigureAwait(false);
            var iLinkSegment = await siteDisplayTable.ExecuteQuerySegmentedAsync(interestingLinksQuery, null).ConfigureAwait(false);
            var dLinkSegment = await siteDisplayTable.ExecuteQuerySegmentedAsync(dotnetLinksQuery, null).ConfigureAwait(false);


            var lists = postListSegment.Where(pl => pl.Published).Select(pc =>
            new PostContentDto
            {
                Caption = pc.Caption,
                Preview = pc.Preview,
                BlobName = pc.BlobName,
                Published = pc.Published,
                PublishedOn = pc.PublishedOn,
                UpdatedOn = pc.UpdatedOn

            });

            var hvm = new BasicContentViewModel
            {
                RequestPath = req?.Path.Value
            };

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
            hvm.Documents = lists;


            return new OkObjectResult(hvm);
        }
    }
}
