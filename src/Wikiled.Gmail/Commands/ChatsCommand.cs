using System.Collections.Concurrent;
using System.IO;
using System.Text;
using CsvHelper;
using Google.Apis.Gmail.v1.Data;
using NLog;
using Wikiled.Gmail.Analysis;

namespace Wikiled.Gmail.Commands
{
    public class ChatsCommand : BaseListGmailCommand
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentBag<SenderHolder> senderHolders = new ConcurrentBag<SenderHolder>();

        private CsvWriter csvTarget;

        protected override bool IsChat => true;

        public override void Execute()
        {
            log.Info("Starting analysis...");
            using (var streamWrite = new StreamWriter(@"chat.csv", false, Encoding.UTF8))
             using (csvTarget = new CsvWriter(streamWrite))
            {
                csvTarget.WriteField("Time");
                csvTarget.WriteField("Email");
                csvTarget.WriteField("Message");
                csvTarget.NextRecord();
                base.Execute();
            }
        }

        protected override void OnMessageCallback(Message content, SenderHolder sender)
        {
            lock (csvTarget)
            {
                csvTarget.WriteField(content.InternalDate);
                csvTarget.WriteField(sender.Email);
                csvTarget.WriteField(content.Snippet);
                csvTarget.NextRecord();
            }
        }
    }
}
