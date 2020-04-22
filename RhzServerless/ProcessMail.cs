// The 'From' and 'To' fields are automatically populated with the values specified by the binding settings.
//
// You can also optionally configure the default From/To addresses globally via host.config, e.g.:
//
// {
//   "sendGrid": {
//      "to": "user@host.com",
//      "from": "Azure Functions <samples@functions.com>"
//   }
// }
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Logging;
using Rhz.Domains.Models;

namespace RhzServerless
{
    public static class ProcessMail
    {
        [FunctionName("ProcessMail")]
        [return: SendGrid(ApiKey = "SendGridKey", To = "r.a.hernandez@outlook.com", From = "noreply@rahernandez.azurewebsites.net")]
        public static SendGridMessage Run([QueueTrigger("newContactMessage", Connection = "AzureWebJobsStorage")]ContactMessage contactMessage, ILogger log)
        {
            // We will have to find a way to get SendGrid to use Azure Key Vault, this template won't work.
            log.LogInformation($"C# Queue trigger function processed order: {contactMessage.Name}");
            

            SendGridMessage message = new SendGridMessage()
            {
                Subject = $"Contact from my Website!"
            };
            log.LogInformation($"C# Key: {message.Headers}");
            message.AddContent("text/plain", $"From: {contactMessage.Name} {Environment.NewLine}Email: {contactMessage.Email} {Environment.NewLine}Subject: {contactMessage.Subject} {Environment.NewLine}Comment: {contactMessage.Comment}");
            return message;
        }
    }
    
}
