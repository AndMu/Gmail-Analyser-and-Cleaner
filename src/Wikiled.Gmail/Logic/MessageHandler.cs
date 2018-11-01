using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Polly;
using Wikiled.Gmail.Analysis;
using System.Linq;
using RateLimiter;

namespace Wikiled.Gmail.Logic
{
    public class MessageHandler
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly GmailService service;

        private readonly SenderHolderFactory factory = new SenderHolderFactory();

        private UsersResource.MessagesResource.ListRequest request;

        private readonly Queue<Message> messages = new Queue<Message>();

        private readonly int batchSize = 500;

        private readonly Policy policy;

        private TimeLimiter timeLimiter;

        public MessageHandler(GmailService service)
        {
            this.service = service;
            timeLimiter = TimeLimiter.GetFromMaxCountByInterval(30, TimeSpan.FromSeconds(100));
            var httpStatusCodesWorthRetrying = new[]
            {
                HttpStatusCode.RequestTimeout, // 408
                HttpStatusCode.InternalServerError, // 500
                HttpStatusCode.BadGateway, // 502
                HttpStatusCode.ServiceUnavailable, // 503
                HttpStatusCode.GatewayTimeout // 504
            };

            policy = Policy
                .Handle<WebException>(r => httpStatusCodesWorthRetrying.Contains(((HttpWebResponse)r.Response).StatusCode))
                .WaitAndRetryAsync(3,
                                   (retries, ex, ctx) =>
                                   {
                                       var web = ((WebException)ex);
                                       if (((HttpWebResponse)web.Response).StatusCode == HttpStatusCode.Forbidden)
                                       {
                                           var wait = TimeSpan.FromMinutes(3);
                                           log.Error("Forbidden detected [{1}]. Waiting {0}", wait, web.Response.ResponseUri);
                                           return wait;
                                       }

                                       return TimeSpan.FromSeconds(retries);
                                   },
                                   (ts, i, ctx, task) => Task.CompletedTask);
        }

        public int TotalMessages { get; private set; }

        public int Outstanding => messages.Count;

        public UsersResource.MessagesResource.ListRequest Setup(string filter)
        {
            request = service.Users.Messages.List("me");
            request.Q = filter;
            request.IncludeSpamTrash = false;
            request.MaxResults = 500;
            return request;
        }

        public async Task ReadMessageDefinitions()
        {
            if (request == null)
            {
                throw new InvalidOperationException("Request was not setup");
            }

            log.Info("Reading Message definitions...");
            do
            {
                ListMessagesResponse emailListResponse = await request.ExecuteAsync().ConfigureAwait(false);
                request.PageToken = emailListResponse.NextPageToken;
                foreach (Message message in emailListResponse.Messages)
                {
                    messages.Enqueue(message);
                }

            }
            while (!string.IsNullOrEmpty(request.PageToken));

            TotalMessages = messages.Count;
            log.Info("Total {0} messages", messages.Count);
        }

        public IObservable<MessageHolder> ReadMessages()
        {
            if (request == null)
            {
                throw new InvalidOperationException("Request was not setup");
            }

            return Observable.Create<MessageHolder>(
                async item =>
                {
                    while (messages.Count > 0)
                    {
                        await policy.ExecuteAsync(() => ProcessBatch(item)).ConfigureAwait(false);
                    }
                    
                    item.OnCompleted();
                });
        }

        private async Task ProcessBatch(IObserver<MessageHolder> item)
        {
            var batchRequest = new BatchRequest(service);
            for (int i = 0; i < batchSize && messages.Count > 0; i++)
            {
                Message email = messages.Dequeue();
                batchRequest.Queue<Message>(
                    service.Users.Messages.Get("me", email.Id),
                    (content, error, index, message) =>
                    {
                        if (error != null)
                        {
                            messages.Enqueue(email);
                        }
                        else
                        {
                            SenderHolder sender = factory.Construct(content);
                            if (sender != null)
                            {
                                item.OnNext(new MessageHolder(sender, content));
                            }
                        }
                    });
            }

            await timeLimiter.Perform(() => batchRequest.ExecuteAsync()).ConfigureAwait(false);
        }
    }
}
