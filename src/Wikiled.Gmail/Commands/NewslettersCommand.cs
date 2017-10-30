using System;
using System.Linq;
using System.Net.Http;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;

namespace Wikiled.Gmail.Commands
{
    public class NewslettersCommand : BaseListGmailCommand
    {
        protected override void OnMessageCallback(Message content, RequestError error, int i, HttpResponseMessage message)
        {
            var unsubscribe = content.Payload?.Headers.Where(item => string.Compare(item.Name, "List-Unsubscribe", StringComparison.OrdinalIgnoreCase) == 0)
                                     .Select(item => item.Value)
                                     .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(unsubscribe))
            {
            }
        }
    }
}
