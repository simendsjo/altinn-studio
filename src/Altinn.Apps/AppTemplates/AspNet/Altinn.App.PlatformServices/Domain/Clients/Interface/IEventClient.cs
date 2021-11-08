using System.Threading.Tasks;

using Altinn.App.Domain.Models;

namespace Altinn.App.Domain.Clients.Interface
{
    /// <summary>
    /// Interface describing client implementation for handling events 
    /// </summary>
    public interface IEventClient
    {
        /// <summary>
        /// Adds a new event to the events published by the Events component.
        /// </summary>
        public Task<string> PostCloudEvent(string accessToken, CloudEvent cloudEvent);
    }
}
