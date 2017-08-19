using Microsoft.Rtc.Collaboration;
using System.Threading.Tasks;

namespace LyncAsyncExtensionMethods
{
    public static class CallMethods
    {
        public static Task<CallMessageData> AcceptAsync(this Call call)
        {
            return Task<CallMessageData>.Factory.FromAsync(call.BeginAccept,
                call.EndAccept, null);
        }


        public static Task<CallMessageData> EstablishAsync(this Call call)
        {
            return Task<CallMessageData>.Factory.FromAsync(
                call.BeginEstablish, call.EndEstablish,
                null);
        }

      
    }
}
