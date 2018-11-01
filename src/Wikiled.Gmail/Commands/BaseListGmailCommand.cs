using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using NLog;
using Polly;
using Wikiled.Common.Logging;
using Wikiled.Gmail.Analysis;
using Wikiled.Gmail.Logic;

namespace Wikiled.Gmail.Commands
{
    public abstract class BaseListGmailCommand : BaseGmailCommand
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private PerformanceMonitor monitor;

        protected abstract void OnMessageCallback(MessageHolder message);

        protected abstract bool IsChat { get; }

        protected virtual void ProgressNotification()
        {
            var line = new string('-', 50);
            log.Info(line);
            log.Info(monitor);
        }

        protected override async Task Process(GmailService service)
        {
            MessageHandler messages = new MessageHandler(service);
            var chatLabel = IsChat ? "+" : "-";
            messages.Setup($"{chatLabel}label:chats");
            await messages.ReadMessageDefinitions().ConfigureAwait(false);
            monitor = new PerformanceMonitor(messages.TotalMessages);
            using (Observable.Interval(TimeSpan.FromSeconds(60)).Subscribe(item => ProgressNotification()))
            {
                await messages.ReadMessages().Select(item =>
                {
                    monitor.Increment();
                    OnMessageCallback(item);
                    return item;
                }).LastOrDefaultAsync();
            }
        }
    }
}
