using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using NLog;
using Wikiled.Core.Utility.Arguments;

namespace Wikiled.Gmail.Commands
{
    public abstract class BaseGmailCommand : Command
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly string[] scopes = { GmailService.Scope.MailGoogleCom, GmailService.Scope.GmailModify, GmailService.Scope.GmailCompose };

        protected string ApplicationName { get; } = "Wikiled GMail Cleaner";

        public override void Execute()
        {
            UserCredential credential;

            using (var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/wikiled.gmail.json");
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                                                             GoogleClientSecrets.Load(stream).Secrets,
                                                             scopes,
                                                             "user",
                                                             CancellationToken.None,
                                                             new FileDataStore(credPath, true))
                                                         .Result;

                log.Info("Credential file saved to: " + credPath);
            }

            // Create Gmail API service.
            var service = new GmailService(
                new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName
                    });

            Process(service).Wait();
        }

        protected abstract Task Process(GmailService service);
    }
}
