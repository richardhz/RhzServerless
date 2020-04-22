using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rhz.Domains.Models;

namespace RhzServerless
{
    public static class QueueContactMessage
    {
        [FunctionName("QueueContactMessage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mail")] HttpRequest req,
            [Queue("newContactMessage")] IAsyncCollector<ContactMessage> newContactMessageQueue,
            ILogger log)
        {
            log.LogInformation("C# Queuing message.");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<ContactMessage>(requestBody);

            await newContactMessageQueue.AddAsync(data);

            return new NoContentResult();
        }
    }
}
