using Microsoft.Rtc.Collaboration;
using System.Threading.Tasks;

namespace LyncAsyncExtensionMethods
{
    public static class CollaborationPlatformMethods
    {
       

        public static Task StartupAsync(this CollaborationPlatform platform)
        {
            return Task.Factory.FromAsync(platform.BeginStartup,
                platform.EndStartup, null);
        }

        public static Task ShutdownAsync
            (this CollaborationPlatform platform)
        {
            return Task.Factory.FromAsync(platform.BeginShutdown,
                platform.EndShutdown, null);
        }
    }
}
