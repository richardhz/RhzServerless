using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace RhzServerless
{
    public static class RhzStorageTools
    {
        public static CloudStorageAccount _storageAccount;
        public const string siteDisplayName = "sitedisplays";
        public const string siteCopyName = "site-copy";
        public const string techDocs = "tech-documents";
        public const string postsName = "posts";

        public const string techPostPk = "Unknown";

        public const string heroPk = "hero";
        public const string heroRk = "index";
        public const string skillPk = "skills";
        public const string skillRk = "modal";

        public const string interestingLinksPk = "interestinglinks";
        public const string dotNetLinksPk = "dotnetlinks";
        public const string aboutPk = "about";
        public const string aboutRk = "me";



        public static TableQuery<T> GenerateContentQuery<T>(string partitionKey, string rowKey = null) where T : TableEntity, new()
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
    }
}
