using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Gmail.Analysis
{
    public class SenderHolder
    {
        public SenderHolder(string email, int? size)
        {
            Guard.NotNullOrEmpty(() => email, email);
            Email = email;
            Size = size;
        }

        public string Email { get; }

        public string Domain { get; }

        public int? Size { get; }
    }
}
