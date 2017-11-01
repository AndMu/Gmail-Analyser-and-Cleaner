using System;
using System.Linq;
using Google.Apis.Gmail.v1.Data;

namespace Wikiled.Gmail.Commands
{
    public class NewslettersCommand : BaseListGmailCommand
    {
        protected override void OnMessageCallback(Message content)
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
