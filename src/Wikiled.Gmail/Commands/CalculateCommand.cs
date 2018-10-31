﻿using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Google.Apis.Gmail.v1.Data;
using NLog;
using Wikiled.Core.Utility.Logging;
using Wikiled.Gmail.Analysis;

namespace Wikiled.Gmail.Commands
{
    public class CalculateCommand : BaseListGmailCommand
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentBag<SenderHolder> senderHolders = new ConcurrentBag<SenderHolder>();

        private PerformanceMonitor monitor;

        protected override bool IsChat => false;

        public override void Execute()
        {
            log.Info("Starting analysis...");
            base.Execute();

            log.Info("Completed. Saving results");
            var first = Task.Run(() => SaveByDomain());
            var second = Task.Run(() => SaveBySender());
            Task.WaitAll(first, second);
        }

        protected override void OnMessageCallback(Message content, SenderHolder sender)
        {
            monitor.ManualyCount();
            senderHolders.Add(sender);
            monitor.Increment();
        }

        protected override void ProgressNotification()
        {
            var line = new string('-', 50);
            var result = senderHolders.GroupBy(item => item.Domain)
                         .Select(
                             item => new
                             {
                                 Domain = item.Key,
                                 Size = item.Sum(x => x.Size),
                                 Total = item.Count()
                             })
                         .OrderByDescending(item => item.Size)
                         .Take(5);
            log.Info("Top 5 domains by size so far:");
            foreach (var record in result)
            {
                log.Info("{0} - {1} messages, {2:F2}MB", record.Domain, record.Total, (double)record.Size / 1024 / 1024);
            }

            log.Info(line);
            var result2 = senderHolders.GroupBy(item => item.Email)
                                  .Select(
                                      item => new
                                      {
                                          Email = item.Key,
                                          Size = item.Sum(x => x.Size),
                                          Total = item.Count()
                                      })
                                  .OrderByDescending(item => item.Size)
                                  .Take(5);

            log.Info("Top 5 senders by size so far:");
            foreach (var record in result2)
            {
                log.Info("{0} - {1} messages, {2:F2}MB", record.Email, record.Total, (double)record.Size / 1024 / 1024);
            }

            log.Info(line);
        }

        private void SaveByDomain()
        {
            var data = senderHolders.GroupBy(item => item.Domain)
                                    .Select(
                                        item => new
                                        {
                                            Domain = item.Key,
                                            Size = item.Sum(x => x.Size),
                                            UnsubscribeCount = item.Count(x => x.HasUnsubscribeTag),
                                            Total = item.Count()
                                        })
                                    .OrderByDescending(item => item.Size);
            using (var streamWrite = new StreamWriter(@"domains.csv", false, Encoding.UTF8))
            using (var csvTarget = new CsvWriter(streamWrite))
            {
                csvTarget.WriteField("Domain");
                csvTarget.WriteField("Count");
                csvTarget.WriteField("UnsubscribeCount");
                csvTarget.WriteField("Size");
                csvTarget.NextRecord();
                foreach (var record in data)
                {
                    csvTarget.WriteField(record.Domain);
                    csvTarget.WriteField(record.Total);
                    csvTarget.WriteField(record.UnsubscribeCount);
                    csvTarget.WriteField(record.Size);
                    csvTarget.NextRecord();
                }
            }
        }

        private void SaveBySender()
        {
            var data = senderHolders.GroupBy(item => item.Email)
                                    .Select(
                                        item => new
                                        {
                                            Email = item.Key,
                                            Size = item.Sum(x => x.Size),
                                            Total = item.Count()
                                        })
                                    .OrderByDescending(item => item.Size);
            using (var streamWrite = new StreamWriter(@"senders.csv", false, Encoding.UTF8))
            using (var csvTarget = new CsvWriter(streamWrite))
            {
                csvTarget.WriteField("Email");
                csvTarget.WriteField("Count");
                csvTarget.WriteField("Size");
                csvTarget.NextRecord();
                foreach (var record in data)
                {
                    csvTarget.WriteField(record.Email);
                    csvTarget.WriteField(record.Total);
                    csvTarget.WriteField(record.Size);
                    csvTarget.NextRecord();
                }
            }
        }
    }
}
