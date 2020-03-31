using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Rhz.Domains.Models;

namespace RhzServerless
{
    public static class DocMonitor
    {
        [FunctionName("DocMonitor")]
        
        public static void Run(
            [BlobTrigger("tech-documents/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob, string name,
            [Queue("newDocs")] IAsyncCollector<DocumentNotify> newDocQueue,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            // when a new file is added to blob storage we detect this here
            // and add the blob name and date to the queue.
            var notify = new DocumentNotify { BlobName = name, PublishedOn = DateTime.UtcNow };
            newDocQueue.AddAsync(notify);  //we should maake this an async method
        }
    }
}
