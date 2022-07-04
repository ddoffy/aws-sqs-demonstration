using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Runtime.CredentialManagement;
using Amazon;

namespace Producer
{
    class Program
    {
        private const int MaxMessages = 1;
        private const int WaitTime = 2;
        private const string QueueUrl = "queue url";
        private const string AWSId = "key";
        private const string AWSSecret = "secret";
        private const string ProfileName = "my_profile";
        static async Task Main(string[] args)
        {

            // For illustrative purposes only--do not include credentials in your code.
            WriteProfile(ProfileName, AWSId, AWSSecret);

            AddRegion(ProfileName, RegionEndpoint.APSoutheast1);


            CredentialProfile profile = null;
            SharedCredentialsFile sharedCredentialsFile = new SharedCredentialsFile();

            if (sharedCredentialsFile.TryGetProfile(ProfileName, out profile))
            {
                var sqsClient = new AmazonSQSClient(profile.GetAWSCredentials(sharedCredentialsFile), profile.Region);

                await ShowQueues(sqsClient);

            }

        }



        public static void AddRegion(string profileName, RegionEndpoint region)
        {
            var sharedFile = new SharedCredentialsFile();
            CredentialProfile profile;
            if (sharedFile.TryGetProfile(profileName, out profile))
            {
                profile.Region = region;
                sharedFile.RegisterProfile(profile);
            }
        }

        private static void WriteProfile(string profileName, string keyId, string secret)
        {
            Console.WriteLine($"Create the [{profileName}] profile...");
            var options = new CredentialProfileOptions
            {
                AccessKey = keyId,
                SecretKey = secret
            };
            var profile = new CredentialProfile(profileName, options);
            var sharedFile = new SharedCredentialsFile();
            sharedFile.RegisterProfile(profile);
        }


        // Method to show a list of the existing queues
        private static async Task ShowQueues(IAmazonSQS sqsClient)
        {
            ListQueuesResponse responseList = await sqsClient.ListQueuesAsync("");
            Console.WriteLine();
            foreach (string qUrl in responseList.QueueUrls)
            {
                // Get and show all attributes. Could also get a subset.
                await ShowAllAttributes(sqsClient, qUrl);
            }
        }

        // Method to show all attributes of a queue
        private static async Task ShowAllAttributes(IAmazonSQS sqsClient, string qUrl)
        {
            var attributes = new List<string> { QueueAttributeName.All };
            GetQueueAttributesResponse responseGetAtt =
              await sqsClient.GetQueueAttributesAsync(qUrl, attributes);
            Console.WriteLine($"Queue: {qUrl}");
            foreach (var att in responseGetAtt.Attributes)
                Console.WriteLine($"\t{att.Key}: {att.Value}");
        }

        //
        // Method to read a message from the given queue
        // In this example, it gets one message at a time
        private static async Task<ReceiveMessageResponse> GetMessage(
          IAmazonSQS sqsClient, string qUrl, int waitTime = 0)
        {
            return await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = qUrl,
                MaxNumberOfMessages = MaxMessages,
                WaitTimeSeconds = waitTime
                // (Could also request attributes, set visibility timeout, etc.)
            });
        }

        //
        // Method to get the ARN of a queue
        private static async Task<string> GetQueueArn(IAmazonSQS sqsClient, string qUrl)
        {
            GetQueueAttributesResponse responseGetAtt = await sqsClient.GetQueueAttributesAsync(
              qUrl, new List<string> { QueueAttributeName.QueueArn });
            return responseGetAtt.QueueARN;
        }


        //
        // Method to check the name of the attribute
        private static bool ValidAttribute(string attribute)
        {
            var attOk = false;
            var qAttNameType = typeof(QueueAttributeName);
            List<string> qAttNamefields = new List<string>();
            foreach (var field in qAttNameType.GetFields())
                qAttNamefields.Add(field.Name);
            foreach (var name in qAttNamefields)
                if (attribute == name) { attOk = true; break; }
            return attOk;
        }

        //
        // Method to update a queue attribute
        private static async Task UpdateAttribute(
          IAmazonSQS sqsClient, string qUrl, string attribute, string value)
        {
            await sqsClient.SetQueueAttributesAsync(qUrl,
              new Dictionary<string, string> { { attribute, value } });
        }


        //
        // Method to delete an SQS queue
        private static async Task DeleteQueue(IAmazonSQS sqsClient, string qUrl)
        {
            Console.WriteLine($"Deleting queue {qUrl}...");
            await sqsClient.DeleteQueueAsync(qUrl);
            Console.WriteLine($"Queue {qUrl} has been deleted.");
        }

        //
        // Method to wait up to a given number of seconds
        private static async Task Wait(
          IAmazonSQS sqsClient, int numSeconds, string qUrl)
        {
            Console.WriteLine($"Waiting for up to {numSeconds} seconds.");
            Console.WriteLine("Press any key to stop waiting. (Response might be slightly delayed.)");
            for (int i = 0; i < numSeconds; i++)
            {
                Console.Write(".");
                Thread.Sleep(1000);
                if (Console.KeyAvailable) break;

                // Check to see if the queue is gone yet
                var found = false;
                ListQueuesResponse responseList = await sqsClient.ListQueuesAsync("");
                foreach (var url in responseList.QueueUrls)
                {
                    if (url == qUrl)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) break;
            }
        }

        //
        // Method to show a list of the existing queues
        private static async Task ListQueues(IAmazonSQS sqsClient)
        {
            ListQueuesResponse responseList = await sqsClient.ListQueuesAsync("");
            Console.WriteLine("\nList of queues:");
            foreach (var qUrl in responseList.QueueUrls)
                Console.WriteLine($"- {qUrl}");
        }

        //
        // Method to put a message on a queue
        // Could be expanded to include message attributes, etc., in a SendMessageRequest
        private static async Task SendMessage(
          IAmazonSQS sqsClient, string qUrl, string messageBody)
        {
            SendMessageResponse responseSendMsg =
              await sqsClient.SendMessageAsync(qUrl, messageBody);
            Console.WriteLine($"Message added to queue\n  {qUrl}");
            Console.WriteLine($"HttpStatusCode: {responseSendMsg.HttpStatusCode}");
        }

        //
        // Method to put a batch of messages on a queue
        // Could be expanded to include message attributes, etc.,
        // in the SendMessageBatchRequestEntry objects
        private static async Task SendMessageBatch(
          IAmazonSQS sqsClient, string qUrl, List<SendMessageBatchRequestEntry> messages)
        {
            Console.WriteLine($"\nSending a batch of messages to queue\n  {qUrl}");
            SendMessageBatchResponse responseSendBatch =
              await sqsClient.SendMessageBatchAsync(qUrl, messages);
            // Could test responseSendBatch.Failed here
            foreach (SendMessageBatchResultEntry entry in responseSendBatch.Successful)
                Console.WriteLine($"Message {entry.Id} successfully queued.");
        }

        //
        // Method to delete all messages from the queue
        private static async Task DeleteAllMessages(IAmazonSQS sqsClient, string qUrl)
        {
            Console.WriteLine($"\nPurging messages from queue\n  {qUrl}...");
            PurgeQueueResponse responsePurge = await sqsClient.PurgeQueueAsync(qUrl);
            Console.WriteLine($"HttpStatusCode: {responsePurge.HttpStatusCode}");
        }

        //
        // Method to delete a message from a queue
        private static async Task DeleteMessage(
          IAmazonSQS sqsClient, Message message, string qUrl)
        {
            Console.WriteLine($"\nDeleting message {message.MessageId} from queue...");
            await sqsClient.DeleteMessageAsync(qUrl, message.ReceiptHandle);
        }






    }
}
