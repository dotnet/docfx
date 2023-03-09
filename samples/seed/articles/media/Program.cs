using System;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;

class Program
{
    static void Main(string[] args)
    {
        // Define the connection string for the storage account
        string connectionString = "DefaultEndpointsProtocol=https;AccountName=<your-account-name>;AccountKey=<your-account-key>;EndpointSuffix=core.windows.net";

        // Create a new BlobServiceClient using the connection string
        var blobServiceClient = new BlobServiceClient(connectionString);

        // Create a new container
        var container = blobServiceClient.CreateBlobContainer("mycontainer");

        // Upload a file to the container
        using (var fileStream = File.OpenRead("path/to/file.txt"))
        {
            container.UploadBlob("file.txt", fileStream);
        }

        // Download the file from the container
        var downloadedBlob = container.GetBlobClient("file.txt").Download();
        using (var fileStream = File.OpenWrite("path/to/downloaded-file.txt"))
        {
            downloadedBlob.Value.Content.CopyTo(fileStream);
        }
    }
}
