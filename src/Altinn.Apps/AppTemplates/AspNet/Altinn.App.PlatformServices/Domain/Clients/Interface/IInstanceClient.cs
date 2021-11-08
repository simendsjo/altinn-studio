using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Altinn.Platform.Storage.Interface.Models;

using Microsoft.Extensions.Primitives;

namespace Altinn.App.Domain.Clients.Interface
{
    /// <summary>
    /// Interface describing client implementation to handle instances
    /// </summary>
    public interface IInstanceClient
    {
        #region Actions on full instance object

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The guid identificator of the instance.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<Instance> GetInstance(int instanceOwnerPartyId, Guid instanceGuid);

        /// <summary>
        /// Gets a list of instances based of a dictionary of query parameters.
        /// </summary>
        /// <param name="queryParams"> A dictionary of query parameters.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<List<Instance>> GetInstances(Dictionary<string, StringValues> queryParams);

        /// <summary>
        /// Deletes an instance.
        /// </summary>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The guid identificator of the instance.</param>
        /// <param name="hardDelete">A boolean indicating if the instance should be hard deleted.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<Instance> DeleteInstance(int instanceOwnerPartyId, Guid instanceGuid, bool hardDelete);

        /// <summary>
        /// Creates a new instance based on a template.
        /// </summary>
        /// <param name="org">The application owner.</param>
        /// <param name="app">The name of the application.</param>
        /// <param name="instanceTemplate">A template of the instance to be created.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<Instance> PostInstance(string org, string app, Instance instanceTemplate);

        #endregion

        #region Actions on instance properties

        /// <summary>
        /// Posts a complete confirmation to the instance.
        /// </summary>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The guid identificator of the instance.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<Instance> PostCompleteConfirmation(int instanceOwnerPartyId, Guid instanceGuid);

        /// <summary>
        /// Puts a new read status on the instance.
        /// </summary>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The id of the instance to confirm as complete.</param>
        /// <param name="readStatus">The new instance read status.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<Instance> PutReadStatus(int instanceOwnerPartyId, Guid instanceGuid, string readStatus);

        /// <summary>
        /// Puts a new substatus on the instance.
        /// </summary>

        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The guid identificator of the instance.</param>
        /// <param name="substatus">The new substatus.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<Instance> PutSubstatus(int instanceOwnerPartyId, Guid instanceGuid, Substatus substatus);

        /// <summary>
        /// Puts new presentation texts on the instance.
        /// </summary>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The guid identificator of the instance.</param>
        /// <param name="presentationTexts">The presentation texts</param>
        /// <returns>Returns the updated instance.</returns>
        Task<Instance> PutPresentationTexts(int instanceOwnerPartyId, Guid instanceGuid, PresentationTexts presentationTexts);

        /// <summary>
        /// Puts new data values on the instance.
        /// </summary>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The guid identificator of the instance.</param>
        /// <param name="dataValues">The data values</param>
        /// <returns>Returns the updated instance.</returns>
        Task<Instance> PutDataValues(int instanceOwnerPartyId, Guid instanceGuid, DataValues dataValues);

        /// <summary>
        /// Puts a new process state on the instance
        /// </summary>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The guid identificator of the instance.</param>
        /// <param name="processState">The updated process state.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<Instance> PutProcess(int instanceOwnerPartyId, Guid instanceGuid, ProcessState processState);

        #endregion
    }
}
