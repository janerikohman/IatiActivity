using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;

namespace ActivityReader
{
    class DataStore
    {
        public static void Store(BaseActivity activity)
        {
            Console.WriteLine($"Storing activity {activity.Iatiidentifier}");
            Store2(activity);
        }

        //https://jeo4cyberdemostorage.queue.core.windows.net/activity?sv=2019-02-02&st=2020-01-13T19%3A43%3A00Z&se=2022-01-14T19%3A43%3A00Z&sp=raup&sig=eyzGMrALnBB54V4IEbS9LT%2F331wPrjQtarR2CBk%2F6AM%3D
        public static void Store2(BaseActivity activity)
        {
            //string sasURI = "https://jeo4cyberdemostorage.queue.core.windows.net/activity?sv=2019-02-02&st=2020-01-13T19%3A43%3A00Z&se=2022-01-14T19%3A43%3A00Z&sp=raup&sig=eyzGMrALnBB54V4IEbS9LT%2F331wPrjQtarR2CBk%2F6AM%3D";
            string sasURI = Environment.GetEnvironmentVariable("QEUEU_SAP");
            QueueClient client = new QueueClient(new System.Uri(sasURI));
            var serilizedActivity = JsonSerializer.Serialize(activity);
            var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(serilizedActivity));
            client.SendMessage(base64String);
        }
    }
}
