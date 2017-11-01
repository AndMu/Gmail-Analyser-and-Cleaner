using System.Text.RegularExpressions;
using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Gmail.Analysis
{
    public class SenderHolder
    {
        public SenderHolder(string email, int? size)
        {
            Guard.NotNullOrEmpty(() => email, email);
            var match = Regex.Match(email, @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                Email = match.Value;
                var parts = Email.Split('@');
                if (parts.Length > 1)
                {
                    Domain = parts[1];
                }
            }
            else
            {
                Email = email;
            }

            Size = size;
        }

        public bool HasUnsubscribeTag { get; set; }

        public string Email { get; }

        public string Domain { get; }

        public int? Size { get; }
    }
}
