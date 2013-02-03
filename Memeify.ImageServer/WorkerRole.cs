using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace Memeify.ImageServer {

    public class WorkerRole : RoleEntryPoint {

        private const string BlobContainer = "images";
        private const string InputQueue = "memeinput";

        private static string _connectionString;
        private static CloudStorageAccount _storageAccount;
        private static CloudBlobClient _blobClient;
        private static CloudBlobContainer _container;

        // ReSharper disable FunctionNeverReturns
        public override void Run() {
            Trace.WriteLine("iGruut.Sockets.ControlServerRole entry point called", "Information");
            while (true) {
                Thread.Sleep(10000);
            }
        }
        // ReSharper restore FunctionNeverReturns

        public override bool OnStart() {
            ServicePointManager.DefaultConnectionLimit = 12;

            _connectionString = CloudConfigurationManager.GetSetting("StorageConnectionString");
            _storageAccount = CloudStorageAccount.Parse(_connectionString);
            _blobClient = _storageAccount.CreateCloudBlobClient();
            _container = _blobClient.GetContainerReference(BlobContainer);

            RunMeme();

            return base.OnStart();
        }

        // ReSharper disable FunctionNeverReturns
        private void RunMeme() {
            var connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var client = QueueClient.CreateFromConnectionString(connectionString, InputQueue);
            (new Thread(() => {
                while (true) {
                    var message = client.Receive();
                    if (message == null) continue;
                    try {
                        var image = message.Properties["image"].ToString();
                        var title = message.Properties["title"].ToString();
                        var description = message.Properties["description"].ToString();

                        CreateMeme(image, title, description);

                        message.Complete();
                    } catch (Exception ex) {
                        message.Abandon();
                    }
                }
            })).Start();
        }
        // ReSharper restore FunctionNeverReturns

        private static void CreateMeme(string imagename, string title, string description) {

            // (1) Get the original image from blob storage
            var img = GetImage(imagename);

            // (2) Add the background, frame and text using GDI
            var w = img.Width + 100;
            var h = img.Height + 200;
            var meme = new Bitmap(w, h);
            using (var g = Graphics.FromImage(meme)) {
                using (var b = new SolidBrush(Color.Black)) {
                    // (2a) Black background
                    g.FillRectangle(b, 0, 0, w, h);
                }
                // (2b) The image
                g.DrawImage(img, 50, 50, img.Width, img.Height);
                using (var p = new Pen(Color.White, 2)) {
                    // (2c) White frame around image
                    g.DrawRectangle(p, 47, 47, img.Width + 6, img.Height + 6);
                }
                using (var b = new SolidBrush(Color.White)) {
                    Single th;
                    using (var f = new Font("Arial", 28, FontStyle.Bold)) {
                        var ts = g.MeasureString(title, f);
                        var tw = ts.Width;
                        th = ts.Height;
                        // (2d) Title text
                        g.DrawString(title, f, b, (int)(w / 2) - (tw / 2), img.Height + 60);
                    }
                    using (var f = new Font("Arial", 20)) {
                        var ts = g.MeasureString(description, f);
                        var tw = ts.Width;
                        // (2e) Description text
                        g.DrawString(description, f, b, (int)(w / 2) - (tw / 2), img.Height + th + 70);
                    }
                }
                g.Flush();
            }

            // (3) Save the resulting image to blob storage
            SaveImage(meme, "result-" + imagename);
            
            meme.Dispose();
        }
        
        private static Image GetImage(string image) {
            Image img;
            var blockBlob = _container.GetBlockBlobReference(image);
            using (var stream = new MemoryStream()) {
                blockBlob.DownloadToStream(stream);
                img = Image.FromStream(stream);
            }
            return img;
        }

        private static void SaveImage(Image meme, string name) {
            var blockBlob = _container.GetBlockBlobReference(name);
            using (var stream = new MemoryStream()) {
                meme.Save(stream, ImageFormat.Jpeg);
                // CAUTION: forgetting the following line may cause headaches
                stream.Seek(0, SeekOrigin.Begin);
                blockBlob.UploadFromStream(stream);
            }
        }

    }

}
