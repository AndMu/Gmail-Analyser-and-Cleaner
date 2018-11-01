using System;
using System.Linq;
using Google.Apis.Gmail.v1.Data;

namespace Wikiled.Gmail.Analysis
{
    public class SenderHolderFactory
    {
        public SenderHolder Construct(Message message)
        {
            var from = message.Payload?.Headers
                .Where(item => string.Compare(item.Name, "From", StringComparison.OrdinalIgnoreCase) == 0)
                .Select(item => item.Value)
                .FirstOrDefault();
            var unsubscribe = message.Payload?.Headers
                .Where(item => string.Compare(item.Name, "List-Unsubscribe", StringComparison.OrdinalIgnoreCase) == 0)
                .Select(item => item.Value)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(@from))
            {
                return null;
            }

            SenderHolder holder = new SenderHolder(@from, message.SizeEstimate);
            holder.HasUnsubscribeTag = !string.IsNullOrWhiteSpace(unsubscribe);
            return holder;
        }
    }
}
