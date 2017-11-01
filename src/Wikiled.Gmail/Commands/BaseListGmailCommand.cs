using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using NLog;
using Polly;

namespace Wikiled.Gmail.Commands
{
    public abstract class BaseListGmailCommand : BaseGmailCommand
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        protected abstract void OnMessageCallback(Message content);

        protected virtual void AddFilters(UsersResource.MessagesResource.ListRequest emailListRequest)
        {
            emailListRequest.Q = "-label:chats";
            emailListRequest.IncludeSpamTrash = false;
            emailListRequest.MaxResults = 200;
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
                
                if (emailListResponse.Messages != null)
                {
                    List<Message> error = null;
                    await Policy.HandleResult<List<Message>>(result => result.Count > 0)
                                .WaitAndRetryAsync(
                                    new[]
                                        {
                                            TimeSpan.FromSeconds(2),
                                            TimeSpan.FromSeconds(4),
                                            TimeSpan.FromSeconds(6)
                                        },
                                    (result, span) =>
                                        {
                                            var messages = error ?? emailListResponse.Messages;
                                            log.Warn("Retrying after errors. With {0} messages", messages.Count);
                                            return ProcessBatchRequest(
                                                service,
                                                messages);
                                        })
                                .ExecuteAsync(
                                    async () =>
                                        {
                                            error = await ProcessBatchRequest(
                                                        service,
                                                        emailListResponse.Messages).ConfigureAwait(false);
                                            return error;
                                        })
                                .ConfigureAwait(false);

                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
            }
            while (!string.IsNullOrEmpty(emailListRequest.PageToken));
        }

        private async Task<List<Message>> ProcessBatchRequest(GmailService service, IList<Message> messages)
        {
            var request = new BatchRequest(service);
            List<Message> errors = new List<Message>();
            foreach (var email in messages)
            {
                request.Queue<Message>(
                    service.Users.Messages.Get("me", email.Id),
                    (content, error, index, message) =>
                        {
                            if (error != null)
                            {
                                errors.Add(email);
                            }
                            else
                            {
                                OnMessageCallback(content);
                            }
                        });
            }

            await request.ExecuteAsync().ConfigureAwait(false);
            return errors;
        }
    }
}
