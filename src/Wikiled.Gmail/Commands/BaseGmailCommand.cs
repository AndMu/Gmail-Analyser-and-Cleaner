using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;
using NLog;
using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Gmail.Commands
{
    public abstract class BaseGmailCommand : Command
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        protected string ApplicationName { get; } = "Wikiled GMail Cleaner";

        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/gmail-dotnet-quickstart.json
        private readonly string[] scopes = {GmailService.Scope.GmailReadonly};

        public override void Execute()
        {
            UserCredential credential;

            using (var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/gmail-dotnet-quickstart.json");
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                                                             GoogleClientSecrets.Load(stream).Secrets,
                                                             scopes,
                                                             "user",
                                                             CancellationToken.None,
                                                             new FileDataStore(credPath, true))
                                                         .Result;
                log.Info("Credential file saved to: " + credPath);
            }

            Process(credential).Wait();
        }

        protected abstract Task Process(UserCredential credential);
    }
}
