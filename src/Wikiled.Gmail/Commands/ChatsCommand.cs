using CsvHelper;
using Google.Apis.Gmail.v1.Data;
using NLog;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Gmail.Analysis;

namespace Wikiled.Gmail.Commands
{
    public class ChatsCommand : BaseListGmailCommand
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private CsvWriter csvTarget;

        protected override bool IsChat => true;

        protected override async Task Execute(CancellationToken token)
        {
            log.Info("Starting analysis...");
            using (StreamWriter streamWrite = new StreamWriter(@"chat.csv", false, Encoding.UTF8))
            using (csvTarget = new CsvWriter(streamWrite))
            {
                csvTarget.WriteField("Time");
                csvTarget.WriteField("Email");
                csvTarget.WriteField("Message");
                csvTarget.NextRecord();
                await base.Execute(token).ConfigureAwait(false);
            }
        }

        protected override void OnMessageCallback(MessageHolder message)
        {
            csvTarget.WriteField(message.Message.InternalDate);
            csvTarget.WriteField(message.Sender.Email);
            csvTarget.WriteField(message.Message.Snippet);
            csvTarget.NextRecord();
        }
    }
}
