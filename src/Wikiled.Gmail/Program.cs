using System.Threading;
using System.Threading.Tasks;
using Wikiled.Console.Arguments;

namespace Wikiled.Gmail
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            AutoStarter starter = new AutoStarter("GMail Utility", args);
            await starter.Start(CancellationToken.None).ConfigureAwait(false);
        }
    }
}