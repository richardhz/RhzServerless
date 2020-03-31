using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using Rhz.Domains.Models;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RhzServerless
{
    public static class RhzApi
    {
        private static CloudStorageAccount _storageAccount;
        private const string siteDisplayName = "sitedisplays";
        private const string siteCopyName = "site-copy";
        public const string techDocs = "tech-documents";
        public const string postsName = "posts";

        private const string testPostPk = "Unknown";

        private const string heroPk = "hero";
        private const string heroRk = "index";
        private const string skillPk = "skills";
        private const string skillRk = "modal";

        private const string interestingLinksPk = "interestinglinks";
        private const string dotNetLinksPk = "dotnetlinks";
        private const string aboutPk = "about";
        private const string aboutRk = "me";


        private static TableQuery<T> GenerateContentQuery<T>(string partitionKey, string rowKey = null) where T : TableEntity, new()
        {
            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                var allRecords = new TableQuery<T>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, partitionKey));
                return allRecords;
            }

            if (rowKey == null)
            {
                var rangeQuery = new TableQuery<T>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
                return rangeQuery;
            }
            else
            {
                var specificQuery = new TableQuery<T>()
                    .Where(
                       TableQuery.CombineFilters(
                         TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                         TableOperators.And,
                         TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey)
                       )
                    );
                return specificQuery;
            }
        }

        public static async Task<string> GetTextFromBlob(CloudBlobClient client, string container, string blobName, string defaultContent = null)
        {
            string htmlText = string.Empty;

            if (!string.IsNullOrWhiteSpace(blobName) && (blobName.ToUpper(CultureInfo.InvariantCulture) != "EMPTY-STRING"))
            {
                var blobContainer = client.GetContainerReference(container);
                await blobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);

                var line = blobContainer.GetBlockBlobReference(blobName);

                using (var memStream = new MemoryStream())
                {
                    await line.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                    return System.Text.Encoding.UTF8.GetString(memStream.ToArray());
                }

            }
            else
            {
                htmlText = defaultContent ?? string.Empty;   
            }

            return htmlText;
        }


        public static CloudBlobClient GetBlobClient()
        {
            _storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            return _storageAccount.CreateCloudBlobClient();
        }


        [FunctionName("GetHero")]
        public static async Task<IActionResult> GetHero(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "index1")] HttpRequest req,
            [Table(siteDisplayName, heroPk, heroRk, Connection = "AzureWebJobsStorage")] DisplayContent heroDisplay,
            [Table(siteDisplayName, skillPk, skillRk, Connection = "AzureWebJobsStorage")] DisplayContent skillsDisplay
            )
        {
            var hvm = new BasicContentViewModel();
            hvm.RequestPath = req?.Path.Value;

            var client = GetBlobClient();
            var heroText = string.Empty;
            var skillText = string.Empty;
            if (heroDisplay != null && heroDisplay.Published)
            {
                heroText = await GetTextFromBlob(client, siteCopyName, heroDisplay.BlobName,heroDisplay.HtmlContent).ConfigureAwait(false);
            }
            if (skillsDisplay != null && skillsDisplay.Published)
            {
                skillText = await GetTextFromBlob(client, siteCopyName, skillsDisplay.BlobName,skillsDisplay.HtmlContent).ConfigureAwait(false);
            }

            hvm.Content.Add(heroPk, heroText);
            hvm.Content.Add(skillPk, skillText);
            return (heroDisplay != null || skillsDisplay != null)
               ? (ActionResult)new OkObjectResult(hvm)
               : new NotFoundResult();
        }


        [FunctionName("GetAbout")]
        public static async Task<IActionResult> GetAbout(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "about1")] HttpRequest req,
            [Table(siteDisplayName, Connection = "AzureWebJobsStorage")] CloudTable siteDisplayTable
            )
        {
            var interestingLinksQuery = GenerateContentQuery<LinkContent>(interestingLinksPk);
            var dotnetLinksQuery = GenerateContentQuery<LinkContent>(dotNetLinksPk);

            var getAboutOp = TableOperation.Retrieve<DisplayContent>(aboutPk, aboutRk);

            var iLinkSegment = await siteDisplayTable.ExecuteQuerySegmentedAsync(interestingLinksQuery, null).ConfigureAwait(false);
            var dLinkSegment = await siteDisplayTable.ExecuteQuerySegmentedAsync(dotnetLinksQuery, null).ConfigureAwait(false);
            var aboutData = await siteDisplayTable.ExecuteAsync(getAboutOp).ConfigureAwait(false);

            if (aboutData.Result == null)
            {
                return new NotFoundResult();
            }
            var aboutDisplay = (DisplayContent)aboutData.Result;


            var hvm = new BasicContentViewModel();
            hvm.RequestPath = req?.Path.Value;

            var client = GetBlobClient();

            var aboutText = string.Empty;
            if (aboutDisplay != null && aboutDisplay.Published)
            {
                aboutText = await GetTextFromBlob(client, siteCopyName, aboutDisplay.BlobName,aboutDisplay.HtmlContent).ConfigureAwait(false);
            }

            hvm.Lists.Add(interestingLinksPk, iLinkSegment.Where(lc => lc.Published).Select(lc =>
            new LinkContentDto 
            { 
                Caption = lc.Caption, 
                Target = lc.Target,
                Url = lc.Url 
            }));
            hvm.Lists.Add(dotNetLinksPk, dLinkSegment.Where(lc => lc.Published).Select(lc => 
            new LinkContentDto 
            { 
                Caption = lc.Caption, 
                Target = lc.Target, 
                Url = lc.Url 
            }));
            hvm.Content.Add(aboutPk, aboutText);

            return (aboutDisplay != null)
               ? (ActionResult)new OkObjectResult(hvm)
               : new NotFoundResult();

        }


        [FunctionName("GetPostList")]
        public static async Task<IActionResult> GetPostList(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documentList1")] HttpRequest req,
            [Table(postsName, Connection = "AzureWebJobsStorage")] CloudTable postsTable,
            [Table(siteDisplayName, Connection = "AzureWebJobsStorage")] CloudTable siteDisplayTable
            )
        {
            var getPostsQuery = GenerateContentQuery<PostContent>(testPostPk);
            

            var interestingLinksQuery = GenerateContentQuery<LinkContent>(interestingLinksPk);
            var dotnetLinksQuery = GenerateContentQuery<LinkContent>(dotNetLinksPk);

            var postListSegment = await postsTable.ExecuteQuerySegmentedAsync(getPostsQuery, null).ConfigureAwait(false);
            var iLinkSegment = await siteDisplayTable.ExecuteQuerySegmentedAsync(interestingLinksQuery, null).ConfigureAwait(false);
            var dLinkSegment = await siteDisplayTable.ExecuteQuerySegmentedAsync(dotnetLinksQuery, null).ConfigureAwait(false);


            var lists = postListSegment.Where(pl => pl.Published).Select(pc => 
            new PostContentDto 
            { 
                Key = pc.RowKey,
                Caption = pc.Caption, 
                Preview = pc.Preview, 
                BlobName = pc.BlobName, 
                Published = pc.Published,
                PublishedOn = pc.PublishedOn,
                UpdatedOn = pc.UpdatedOn
                
            });

            var hvm = new BasicContentViewModel();
            hvm.RequestPath = req?.Path.Value;

            hvm.Lists.Add(interestingLinksPk, iLinkSegment.Where(lc => lc.Published).Select(lc =>
            new LinkContentDto
            {
                Caption = lc.Caption,
                Target = lc.Target,
                Url = lc.Url
            }));
            hvm.Lists.Add(dotNetLinksPk, dLinkSegment.Where(lc => lc.Published).Select(lc =>
            new LinkContentDto
            {
                Caption = lc.Caption,
                Target = lc.Target,
                Url = lc.Url
            }));
            hvm.Documents = lists;


            return new OkObjectResult(hvm);
        }

        [FunctionName("GetPost")]
        public static async Task<IActionResult> GetPost(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "document1/{key}")] HttpRequest req, string key,
           //[Table(postsName, testPostPk, "010286c659974974bf064fed7d7a055c", Connection = "AzureWebJobsStorage")] PostContent documentDisplay
           [Table(postsName, Connection = "AzureWebJobsStorage")] CloudTable postsTable
           )
        {
            var hvm = new BasicContentViewModel();
            hvm.RequestPath = req?.Path.Value;

            var getDocOp = TableOperation.Retrieve<PostContent>(testPostPk, key);

            var postData = await postsTable.ExecuteAsync(getDocOp).ConfigureAwait(false);

            //if (postData.Result == null)
            //{
            //    return new NotFoundResult();
            //}
            //var documentDisplay = (PostContent)postData.Result;

            //-- Alternative start
            if(postData.HttpStatusCode == 200)
            {
                var documentDisplay = (PostContent)postData.Result;
                var client = GetBlobClient();
                var documentText = string.Empty;
                if (documentDisplay != null && documentDisplay.Published)
                {
                    documentText = await GetTextFromBlob(client, techDocs, documentDisplay.BlobName).ConfigureAwait(false);
                    hvm.Content.Add("document", documentText);
                    return new OkObjectResult(hvm);
                }

            }
            return new NotFoundResult();
            //-- Alternative end

            //var client = GetBlobClient();
            //var documentText = string.Empty;
            //if(documentDisplay != null && documentDisplay.Published)
            //{
            //    documentText = await GetTextFromBlob(client, techDocs, documentDisplay.BlobName).ConfigureAwait(false);
            //}


            //hvm.Content.Add("document", documentText);
            //return (documentDisplay != null)
            //   ? (ActionResult)new OkObjectResult(hvm)
            //   : new NotFoundResult();
        }

    }
}
