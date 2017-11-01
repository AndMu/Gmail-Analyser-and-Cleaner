using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Newtonsoft.Json;
using Wikiled.Gmail.Analysis;

namespace Wikiled.Gmail.Commands
{
    public class CalculateCommand : BaseListGmailCommand
    {
        private ConcurrentBag<SenderHolder> senderHolders = new ConcurrentBag<SenderHolder>();

        public override void Execute()
        {
            base.Execute();
            Save();
        }

        protected override void OnMessageCallback(Message content, RequestError error, int i, HttpResponseMessage message)
        {
            var from = content.Payload?.Headers.Where(item => string.Compare(item.Name, "From", StringComparison.OrdinalIgnoreCase) == 0).Select(item => item.Value).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(@from))
            {
                SenderHolder holder = new SenderHolder(@from, content.SizeEstimate);
                senderHolders.Add(holder);
            }

            if (senderHolders.Count % 1000 == 0)
            {
                Save();
            }
        }

        private void Save()
        {
            var data = senderHolders.GroupBy(item => item.Domain)
                .Select(item => new
                {
                    Domain = item.Key,
                    Size = item.Sum(x => x.Size),
                    Total = item.Count()
                })
                .OrderByDescending(item => item.Size);
            var json = JsonConvert.SerializeObject(data);
            File.WriteAllText("results.json", json);
        }
    }
}
