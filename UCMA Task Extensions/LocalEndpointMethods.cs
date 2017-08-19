using Microsoft.Rtc.Collaboration;
using Microsoft.Rtc.Signaling;
using System.Threading.Tasks;

namespace LyncAsyncExtensionMethods
{
    public static class LocalEndpointMethods
    {
        public static Task<SipResponseData> EstablishAsync(this 
            LocalEndpoint endpoint)
        {
            return Task<SipResponseData>.Factory.FromAsync(
                endpoint.BeginEstablish,
                endpoint.EndEstablish, null);
        }
             
        public static Task TerminateAsync(this LocalEndpoint endpoint)
        {
            return Task.Factory.FromAsync(endpoint.BeginTerminate,
                endpoint.EndTerminate, null);
        }
    }
}
