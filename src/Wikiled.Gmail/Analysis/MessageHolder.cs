using System;
using Google.Apis.Gmail.v1.Data;

namespace Wikiled.Gmail.Analysis
{
    public class MessageHolder
    {
        public MessageHolder(SenderHolder sender, Message message)
        {
            Sender = sender ?? throw new ArgumentNullException(nameof(sender));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public SenderHolder Sender { get; }

        public Message Message { get; }
    }
}
