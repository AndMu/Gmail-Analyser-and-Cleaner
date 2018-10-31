using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using NLog;
using Polly;
using Wikiled.Core.Utility.Logging;
using Wikiled.Gmail.Analysis;

namespace Wikiled.Gmail.Commands
{
    public abstract class BaseListGmailCommand : BaseGmailCommand
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly SenderHolderFactory factory = new SenderHolderFactory();

        private PerformanceMonitor monitor;

        protected abstract void OnMessageCallback(Message content, SenderHolder sender);

        protected abstract bool IsChat { get; }

        protected virtual void ProgressNotification()
        {
            var line = new string('-', 50);
            log.Info(line);
            log.Info(monitor);
        }

        protected override async Task Process(GmailService service)
        {
            var emailListRequest = service.Users.Messages.List("me");
            AddFilters(emailListRequest, IsChat);
            monitor = new PerformanceMonitor(0);
            using (Observable.Interval(TimeSpan.FromSeconds(60)).Subscribe(item => ProgressNotification()))
            {
                await ProcessMessages(emailListRequest, service).ConfigureAwait(false);
            }
        }

        private void AddFilters(UsersResource.MessagesResource.ListRequest emailListRequest, bool chat)
        {
            var chatLabel = chat ? "+" : "-";
            emailListRequest.Q = $"{chatLabel}label:chats";
            emailListRequest.IncludeSpamTrash = false;
            emailListRequest.MaxResults = 200;
        }

        private async Task<List<Message>> ProcessBatchRequest(
            GmailService service,
            IList<Message> messages)
        {
            var request = new BatchRequest(service);
            List<Message> errors = new List<Message>();
            foreach (var email in messages)
            {
                request.Queue<Message>(
                    service.Users.Messages.Get("me", email.Id),
                    (content, error, index, message) =>
                        {
                            monitor.Increment();
                            if (error != null)
                            {
                                errors.Add(email);
                            }
                            else
                            {
                                var sender = factory.Construct(content);
                                if (sender != null)
                                {
                                    OnMessageCallback(content, sender);
                                }
                            }
                        });
            }

            try
            {
                await request.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Error(e);
                errors.Clear();
                errors.AddRange(messages);
            }

            return errors;
        }

        private async Task ProcessMessages(
            UsersResource.MessagesResource.ListRequest emailListRequest,
            GmailService service)
        {
            do
            {
                var emailListResponse = await emailListRequest.ExecuteAsync().ConfigureAwait(false);
                emailListRequest.PageToken = emailListResponse.NextPageToken;

                if (emailListResponse.Messages != null)
                {
                    List<Message> error = null;
                    await Policy.HandleResult<List<Message>>(result => result.Count > 0)
                                .Or<Exception>().WaitAndRetryAsync(
                                    new[]
                                        {
                                            TimeSpan.FromSeconds(2),
                                            TimeSpan.FromSeconds(4),
                                            TimeSpan.FromSeconds(6)
                                        },
                                    (result, span) =>
                                        {
                                            var messages = error ?? emailListResponse.Messages;
                                            log.Warn(
                                                "Retrying after errors. With {0} messages",
                                                messages.Count);
                                            return ProcessBatchRequest(service, messages);
                                        }).ExecuteAsync(
                                    async () =>
                                        {
                                            error = await ProcessBatchRequest(
                                                            service,
                                                            emailListResponse.Messages)
                                                        .ConfigureAwait(false);
                                            return error;
                                        }).ConfigureAwait(false);
                }
            }
            while (!string.IsNullOrEmpty(emailListRequest.PageToken));
        }
    }
}
