﻿using System;
using System.IO;
using System.Xml.Serialization;

using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;


namespace ActivityReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Starting import {DateTime.Now}!");


            // https://jeo4cyberdemostorage.blob.core.windows.net/private2?sv=2019-12-12&si=private2-176F33B69E2&sr=c&sig=6VmOqynJf1gyH%2BO%2FTcDx3AXOi4sxESy5WpXlD%2Bccs5c%3D
            //var containerSAP = args[0];
            //var fileName = args[1];
            //var containerSAP = "https://jeo4cyberdemostorage.blob.core.windows.net/private2?sv=2019-12-12&si=private2-176F33B69E2&sr=c&sig=6VmOqynJf1gyH%2BO%2FTcDx3AXOi4sxESy5WpXlD%2Bccs5c%3D";
            var containerSAP = Environment.GetEnvironmentVariable("CONTAINER_SAP");
            var fileQSAS = Environment.GetEnvironmentVariable("IATI_FILE_SAS");
            string fileName = ExtractFileName(fileQSAS);
            Console.WriteLine($"Extracted filename was {fileName}");
            if (fileName==null || fileName.Length==0)
            {
                fileName = "DZ.xml";
            }

            Console.WriteLine($"SAP is now {containerSAP}");
            Console.WriteLine($"QSAP is now {Environment.GetEnvironmentVariable("QEUEU_SAP")}");
            Console.WriteLine($"FileName is {fileName}");

            try
            {
                var containerClient = new BlobContainerClient(new Uri(containerSAP), null);
                var blobclient = containerClient.GetBlobClient(fileName);


                //using (activityStream = File.OpenRead(args[0]))
                using (Stream activityStream = blobclient.OpenRead())
                {
                    ReadActivities(activityStream);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Something unexpected happened {ex.Message}!");
            }
            Console.WriteLine($"Finished import - SAP supplied through build Pipeline {DateTime.Now}!");
        }

        private static string ExtractFileName(string fileQSAS)
        {
            string blobName = null;
            try
            {
                QueueClient client = new QueueClient(new System.Uri(fileQSAS), new QueueClientOptions() { MessageEncoding = QueueMessageEncoding.Base64 });
                QueueMessage msg = client.ReceiveMessage();
                var blobUri = msg.Body.ToString();
                var blobUriSegments = new Uri(blobUri).Segments;
                blobName = blobUriSegments[^1];
                client.DeleteMessage(msg.MessageId, msg.PopReceipt);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Extract filename failed with message {e.ToString()}");
            }
            Console.WriteLine($"ExtractFileName returns {blobName}");
            return blobName;
        }

        private static void ReadActivities(Stream activityStream)
        {
            using System.IO.StreamReader stream = new System.IO.StreamReader(activityStream);
            var ser = new XmlSerializer(typeof(iatiactivities));
            var c = (iatiactivities)ser.Deserialize(stream);
            Console.WriteLine($"Processing {c.iatiactivity.Length} activityidentifiers");
            foreach (var activity in c.iatiactivity)
            {
                ProcessActivity(activity);
            }
        }

        private static void ProcessActivity(iatiactivity activity)
        {
            BaseActivity ba = new BaseActivity
            {
                Iatiidentifier = activity.iatiidentifier.Value,
                Title = activity.title.narrative != null && activity.title.narrative.Length > 0 ? GetNarrative(activity.title.narrative, false) : "no title",
                Description = GetDescription(activity.description, "1", false),
                Sector = ProcessSector(activity)
            };
            Console.WriteLine($"Acitivity: {ba.Iatiidentifier}");
            Console.WriteLine($"  Title: {ba.Title}");
            Console.WriteLine($"  Sector code={ba.Sector.Code}, name={ba.Sector.Name}");
            Console.WriteLine($"  Description: {ba.Description}");
            
            // Persist activity in data store.
            DataStore.Store(ba);
        }
        private static string GetDescription(iatiactivityDescription[] descriptions, string descriptionType, bool swedish)
        {
            foreach (var desc in descriptions)
            {
                if (desc.type != null && desc.type.Equals(descriptionType, StringComparison.OrdinalIgnoreCase))
                {
                    if (desc.narrative != null)
                    {
                        var val = GetNarrative(desc.narrative, swedish);
                        return val?.Length > 500 ? val.Substring(0, 499) : val;
                    }
                }
            }
            return null;
        }

        private static string GetNarrative(narrative[] narratives, bool swedish)
        {
            foreach (var nar in narratives)
            {
                if (nar.lang != null && nar.lang.Equals("sv", StringComparison.OrdinalIgnoreCase))
                {
                    if (swedish)
                    {
                        return nar.Value;
                    }
                }
                else
                {
                    if (!swedish)
                    {
                        return nar.Value;
                    }
                }

            }
            return null;
        }

        private static BaseSector ProcessSector(iatiactivity activity)
        {
            if (activity.sector == null || activity.sector.Length == 0) return null;
            var item = activity.sector[0];
            return new BaseSector()
            {
                Code = item.code,
                Name = item.narrative != null && item.narrative.Length > 0 ? item.narrative[0].Value : null
            };
        }

    }

}
