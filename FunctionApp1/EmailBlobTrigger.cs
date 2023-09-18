using System;
using System.Collections.Generic;
using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using SendGrid;
using Azure.Storage;

namespace FunctionApp1
{
    public class EmailBlobTrigger
    {
        private readonly IConfiguration _configuration;
        private readonly string pathName = "files";
        public EmailBlobTrigger(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("EmailBlobTrigger")]
        public void Run(
                [BlobTrigger("files/{name}", Connection = "task")] Stream myBlob,
                string name,
                IDictionary<string, string> metadata,
                ILogger log)
        {
            log.LogInformation($"Blob trigger function processed blob\n Name:{name}");

            string sasToken = GenerateSasToken(pathName, name);
            
            SendEmail(metadata, name, sasToken, log);
            
        }

        /// <summary>
        /// Generates a SAS token with a validity of one hour
        /// </summary>
        /// <param name="containerName">Blob container name</param>
        /// <param name="blobName">Name of uploaded .docx file</param>
        /// <returns>SAS link</returns>
        private string GenerateSasToken(string containerName, string blobName)
        {
            var storageAccountName = _configuration["StorageAccountName"];
            var storageAccountKey = _configuration["StorageAccountKey"];

            var blobServiceClient = new BlobServiceClient($"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageAccountKey};EndpointSuffix=core.windows.net");

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            BlobSasBuilder blobSasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                ExpiresOn = DateTime.UtcNow.AddHours(1),
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sasToken = blobSasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(storageAccountName,
                storageAccountKey)).ToString();

            var sasUrl = blobClient.Uri.AbsoluteUri + "?" + sasToken;

            return sasUrl;
        }
        /// <summary>
        /// Sends an email about blob upload using SendGrid API 
        /// </summary>
        /// <param name="metadata">Email that has been entered in the upload form</param>
        /// <param name="name">Name of uploaded .docx file</param>
        /// <param name="sasToken">SAS token Url</param>
        /// <param name="log">Action Logging</param>
        private void SendEmail(IDictionary<string, string> metadata, string name, string sasToken, ILogger log)
        {
            var client = new SendGridClient(_configuration["SendGridApiKey"]);

            var msg = new SendGridMessage();
            msg.SetFrom(new EmailAddress(_configuration["Email"], "Blob managment"));
            msg.AddTo(new EmailAddress(metadata["Email"]));
            msg.SetSubject("File Uploaded Successfully");
            msg.AddContent(MimeType.Html, $"<p>Your file '{name}' has been uploaded successfully.</p><p>Download link: <a href=\"{sasToken}\">Download</a></p>");

            var response = client.SendEmailAsync(msg).GetAwaiter().GetResult();
            log.LogInformation($"Email sent with status code: {response.StatusCode}");
        }
    }
}
