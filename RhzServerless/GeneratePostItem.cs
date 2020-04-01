using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Rhz.Domains.Models;

namespace RhzServerless
{
    public static class GeneratePostItem
    {
        [FunctionName("GeneratePostItem")]
        public static async Task Run(
            [QueueTrigger("newdocs", Connection = "AzureWebJobsStorage")]DocumentNotify myQueueItem,
            [Table(RhzStorageTools.postsName, Connection = "AzureWebJobsStorage")] CloudTable postsTable,
            //[Table(RhzStorageTools.postsName)] IAsyncCollector<PostContent> postsTable,  // this and the commented line below are alternatives .
            IBinder binder,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            string text = await RhzStorageTools.ReadTextFromBlob(binder, RhzStorageTools.techDocs, myQueueItem.BlobName, "AzureWebJobsStorage").ConfigureAwait(false);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(text);

            log.LogInformation($"Document Loaded length: {doc.ToString().Length}");

            // Check the first 2 lines of the document
            // This still needs a lot of work, not thought of all the possibilites yet, just get it functioning
            var linePosition = 1;
            try
            {
                var cap = doc.DocumentNode.SelectSingleNode($"/html[1]/body[1]/div[1]/p[{linePosition}]/span[1]");
                if (cap == null)
                {
                    linePosition++;
                    cap = doc.DocumentNode.SelectSingleNode($"/html[1]/body[1]/div[1]/p[{linePosition}]/span[1]");
                }

                log.LogInformation($"Preview Position: {linePosition}");

                var post = new PostContent
                {
                    PartitionKey = "Unknown",
                    BlobName = myQueueItem.BlobName,
                    Published = true,
                    PublishedOn = myQueueItem.PublishedOn,
                    UpdatedOn = myQueueItem.PublishedOn,
                    Caption = myQueueItem.BlobName.Replace("-", " "),
                    Preview = cap?.InnerText ?? "Preview not provided",
                    RowKey = Guid.NewGuid().ToString("N"),
                    ETag = "*"
                };

                var insertOp = TableOperation.Insert(post);
                var newRecord = await postsTable.ExecuteAsync(insertOp);
                //await postsTable.AddAsync(post);
                log.LogInformation($"New Post status: {newRecord.HttpStatusCode}");
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error {ex.Message}: {myQueueItem}");
            }
        }
    }
}
