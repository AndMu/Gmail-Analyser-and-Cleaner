using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using NLog;
using Wikiled.Gmail.Analysis;

namespace Wikiled.Gmail.Commands
{
    public class NewslettersCommand : BaseListGmailCommand
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentQueue<string> deleteMessage = new ConcurrentQueue<string>();

        private GmailService currentGmailService;

        protected override bool IsChat => false;

        public override Task StartExecution(CancellationToken token)
        {
            log.Info("Deleting newsletters...");
            return StartExecution(token);
        }

        protected override void OnMessageCallback(MessageHolder message)
        {
            if (message.Sender.HasUnsubscribeTag)
            {
                var result = currentGmailService.Users.Messages.Trash("me", message.Message.Id);
                var exResExecute = result.Execute();
                deleteMessage.Enqueue(message.Message.Id);
            }
        }

        protected override async Task Process(GmailService service)
        {
            currentGmailService = service;
            await base.Process(service).ConfigureAwait(false);
            await DeleteMessages().ConfigureAwait(false);
        }

        protected override void ProgressNotification()
        {
            base.ProgressNotification();
            Task.Run(DeleteMessages);
        }

        private async Task DeleteMessages()
        {
            var request = new BatchRequest(currentGmailService);
            List<int> errors = new List<int>();
            while (deleteMessage.TryDequeue(out var id))
            {
                request.Queue<Message>(
                    currentGmailService.Users.Messages.Trash(id, "me"),
                    (content, error, index, message) =>
                        {
                            if (error != null)
                            {
                                errors.Add(index);
                            }
                        });
            }

            if (request.Count > 0)
            {
                log.Info("Deleting {0} messages", request.Count);
                try
                {
                    await request.ExecuteAsync().ConfigureAwait(false);
                    if (errors.Count > 0)
                    {
                        log.Error("Failed <{0}> requests", errors.Count);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
        }
    }
}
