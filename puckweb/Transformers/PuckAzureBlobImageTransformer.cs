using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using puck.core.Abstract;
using puck.core.Base;
using System.IO;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using puck.Models;
using System.Configuration;

namespace puck.Transformers
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class PuckAzureBlobImageTransformer : Attribute, I_Property_Transformer<PuckImage, PuckImage>
    {
        string accountName = ConfigurationManager.AppSettings["AzureImageTransformer_AccountName"];
        string accessKey = ConfigurationManager.AppSettings["AzureImageTransformer_AccessKey"];
        string containerName = ConfigurationManager.AppSettings["AzureImageTransformer_ContainerName"];

        public PuckImage Transform(BaseModel m,string propertyName,string ukey,PuckImage p)
        {
            try
            {
                if (p.File == null || string.IsNullOrEmpty(p.File.FileName))
                    return null;

                
                StorageCredentials creden = new StorageCredentials(accountName, accessKey);

                CloudStorageAccount acc = new CloudStorageAccount(creden, useHttps: false);

                CloudBlobClient client = acc.CreateCloudBlobClient();

                CloudBlobContainer cont = client.GetContainerReference(containerName);

                if(cont.CreateIfNotExists())
                    cont.SetPermissions(new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });

                string filepath = string.Concat(m.Id, "/", m.Variant, "/", ukey, "_", p.File.FileName);

                CloudBlockBlob cblob = cont.GetBlockBlobReference(filepath);
                cblob.UploadFromStream(p.File.InputStream);
                
                p.Path = $"https://{accountName}.blob.core.windows.net/{containerName}/{filepath}";
                p.Size = p.File.InputStream.Length.ToString();
                p.Extension=Path.GetExtension(p.File.FileName);
                var img = System.Drawing.Image.FromStream(p.File.InputStream);
                p.Width = img.Width;
                p.Height = img.Height;
            }catch(Exception ex){
                puck.core.State.PuckCache.PuckLog.Log(ex);
            }finally {
                p.File = null;
            }
            return p;
        }
    }    
}