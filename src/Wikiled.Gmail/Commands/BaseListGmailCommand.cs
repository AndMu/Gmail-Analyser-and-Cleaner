using System;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;

namespace Wikiled.Gmail.Commands
{
    public abstract class BaseListGmailCommand : BaseGmailCommand
    {
        protected abstract void OnMessageCallback(Message content, RequestError error, int i, HttpResponseMessage message);

        protected virtual void AddFilters(UsersResource.MessagesResource.ListRequest emailListRequest)
        {
            emailListRequest.Q = "-label:chats";
            emailListRequest.IncludeSpamTrash = false;
            emailListRequest.MaxResults = 100;
        }

        protected override async Task Process(UserCredential credential)
        {
            // Create Gmail API service.
            var service = new GmailService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

            var emailListRequest = service.Users.Messages.List("me");
            AddFilters(emailListRequest);

            do
            {
                var emailListResponse = await emailListRequest.ExecuteAsync().ConfigureAwait(false);
                emailListRequest.PageToken = emailListResponse.NextPageToken;
                if (emailListResponse?.Messages != null)
                {
                    var request = new BatchRequest(service);
                    foreach (var email in emailListResponse.Messages)
                    {
                        request.Queue<Message>(service.Users.Messages.Get("me", email.Id), OnMessageCallback);
                    }

                    await request.ExecuteAsync().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(1.5)).ConfigureAwait(false);
                }
            }
            while (!string.IsNullOrEmpty(emailListRequest.PageToken));
        }
    }
}
