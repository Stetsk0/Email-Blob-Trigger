using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using SendGrid;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Email_Blob_Trigger
{
    public class Function1
    {

        [FunctionName("Function1")]
        public void Run([BlobTrigger("files/{name}", Connection = "test")]Stream myBlob, string name, IDictionary<string, string> metadata, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            string recipientEmail = metadata["Email"];
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("stonksplus52@gmail.com", "Blob managment");
            var to = new EmailAddress(recipientEmail);
            var subject = "New file Uploaded to your blob storage container";
            var body = $"The file '{name}' was uploaded to the blob storage container.";
            var message = MailHelper.CreateSingleEmail(from, to, subject, body, "");
            var response = client.SendEmailAsync(message).Result;

            log.LogInformation($"Sent email notification with status code {response.StatusCode}");
        }
    }
    
}
