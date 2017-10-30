using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Wikiled.Gmail.Analysis;

namespace Wikiled.Gmail
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            ProcessMessages(credential).Wait();
            Console.Read();
        }

        private static async Task ProcessMessages(UserCredential credential)
        {
            // Create Gmail API service.
            var service = new GmailService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

            var emailListRequest = service.Users.Messages.List("me");
            emailListRequest.Q = "-label:chats";
            emailListRequest.IncludeSpamTrash = false;
            emailListRequest.MaxResults = 100;

            do
            {
                //get our emails
                var emailListResponse = await emailListRequest.ExecuteAsync().ConfigureAwait(false);
                emailListRequest.PageToken = emailListResponse.NextPageToken;
                if (emailListResponse?.Messages != null)
                {
                    // Create a batch request.
                    var request = new BatchRequest(service);
                    //loop through each email and get what fields you want...
                    foreach (var email in emailListResponse.Messages)
                    {
                        request.Queue<Message>(service.Users.Messages.Get("me", email.Id),
                            (content, error, i, message) =>
                            {
                                var from = content.Payload?.Headers.Where(item => string.Compare(item.Name, "From", StringComparison.OrdinalIgnoreCase) == 0).Select(item => item.Value).FirstOrDefault();
                                var unsubscribe = content.Payload?.Headers.Where(item => string.Compare(item.Name, "List-Unsubscribe", StringComparison.OrdinalIgnoreCase) == 0).Select(item => item.Value).FirstOrDefault();
                                if (!string.IsNullOrWhiteSpace(from))
                                {
                                    SenderHolder holder = new SenderHolder(from, content.SizeEstimate);
                                }
                            });
                    }

                    await request.ExecuteAsync().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(1.5)).ConfigureAwait(false);
                }
            } while (!String.IsNullOrEmpty(emailListRequest.PageToken));
        }
    }
}