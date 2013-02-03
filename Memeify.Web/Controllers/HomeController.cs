using System;
using System.Web.Mvc;
using Memeify.Web.Models;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Memeify.Web.Controllers {

    public class HomeController : Controller {

        private const string BlobContainer = "images";
        private const string InputQueue = "memeinput";

        public ActionResult Index() {
            return View();
        }

        [HttpPost]
        public ActionResult CreateMeme(MemeModel model) {

            // TODO: form validation

            var connectionString = CloudConfigurationManager.GetSetting("StorageConnectionString");
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(BlobContainer);

            container.CreateIfNotExist();
            container.SetPermissions(
                new BlobContainerPermissions {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                }
            );

            var ext = model.Image.FileName;
            ext = ext.Substring(ext.LastIndexOf('.'));
            var guid = Guid.NewGuid().ToString();
            var blobname = DateTime.UtcNow.Ticks + "-" + guid + ext;

            // TODO: check if blobname is unique - see http://blog.smarx.com/posts/testing-existence-of-a-windows-azure-blob

            var blockBlob = container.GetBlockBlobReference(blobname);
            blockBlob.UploadFromStream(model.Image.InputStream);

            connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");

            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            var qd = new QueueDescription(InputQueue);
            if (!namespaceManager.QueueExists(InputQueue)) {
                namespaceManager.CreateQueue(qd);
            }

            var queue = QueueClient.CreateFromConnectionString(connectionString, InputQueue);
            var msg = new BrokeredMessage();
            msg.Properties["image"] = blobname;
            msg.Properties["title"] = model.Title;
            msg.Properties["description"] = model.Description;

            queue.Send(msg);

            return RedirectToAction("Show", new { filename = "result-" + blobname });
        }

        public ActionResult Show(string filename) {
            ViewBag.Filename = filename;
            return View();
        }

    }

}
