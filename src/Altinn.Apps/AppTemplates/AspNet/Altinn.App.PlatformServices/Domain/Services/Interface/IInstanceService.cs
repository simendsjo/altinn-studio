using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Altinn.App.Common.RequestHandling;
using Altinn.App.Domain.Models;
using Altinn.Platform.Storage.Interface.Models;

using Microsoft.Extensions.Primitives;

namespace Altinn.App.Domain.Services.Interface
{
    /// <summary>
    /// Interface for handling instance related operations
    /// </summary>
    public interface IInstanceService
    {
        /// <summary>
        /// Gets the instance
        /// </summary>
        Task<InstanceResponse> GetInstance(string org, string app, int instanceOwnerPartyId, Guid instanceGuid);

        /// <summary>
        /// Gets a list of instances based on a dictionary of provided query parameters.
        /// </summary>
        Task<InstanceQueryResponse> GetInstances(Dictionary<string, StringValues> queryParams);

        /// <summary>
        /// Updates the process model of the instance and returns the updated instance.
        /// </summary>
        Task<InstanceResponse> UpdateProcess(int instanceOwnerPartyId, Guid instanceGuid, ProcessState process);

        /// <summary>
        /// Creates an instance of an application with no data.
        /// </summary>
        /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
        /// <param name="app">Application identifier which is unique within an organisation.</param>
        /// <param name="parsedRequest">The parsed multipart request</param>
        /// <returns>The created instance</returns>
        Task<InstanceResponse> CreateInstance(string org, string app, MultipartRequestReader parsedRequest);

        /// <summary>
        /// Add complete confirmation.
        /// </summary>
        /// <remarks>
        /// Add to an instance that a given stakeholder considers the instance as no longer needed by them. The stakeholder has
        /// collected all the data and information they needed from the instance and expect no additional data to be added to it.
        /// The body of the request isn't used for anything despite this being a POST operation.
        /// </remarks>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The id of the instance to confirm as complete.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<InstanceResponse> AddCompleteConfirmation(int instanceOwnerPartyId, Guid instanceGuid);

        /// <summary>
        /// Update read status.
        /// </summary>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The id of the instance to confirm as complete.</param>
        /// <param name="readStatus">The new instance read status.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<InstanceResponse> UpdateReadStatus(int instanceOwnerPartyId, Guid instanceGuid, string readStatus);

        /// <summary>
        /// Update substatus.
        /// </summary>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The id of the instance to be updated.</param>
        /// <param name="substatus">The new substatus.</param>
        /// <returns>Returns the updated instance.</returns>
        Task<InstanceResponse> UpdateSubstatus(int instanceOwnerPartyId, Guid instanceGuid, Substatus substatus);

        /// <summary>
        /// Update presentation texts.
        /// </summary>
        /// <remarks>
        /// The provided presentation texts will be merged with the existing collection of presentation texts on the instance.
        /// </remarks>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The id of the instance to update presentation texts for.</param>
        /// <param name="presentationTexts">The presentation texts</param>
        /// <returns>Returns the updated instance.</returns>
        Task<InstanceResponse> UpdatePresentationTexts(int instanceOwnerPartyId, Guid instanceGuid, PresentationTexts presentationTexts);

        /// <summary>
        /// Update data values.
        /// </summary>
        /// <remarks>
        /// The provided data values will be merged with the existing collection of data values on the instance.
        /// </remarks>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The id of the instance to update data values for.</param>
        /// <param name="dataValues">The data values</param>
        /// <returns>Returns the updated instance.</returns>
        Task<InstanceResponse> UpdateDataValues(int instanceOwnerPartyId, Guid instanceGuid, DataValues dataValues);

        /// <summary>
        /// Delete instance.
        /// </summary>
        /// <param name="instanceOwnerPartyId">The party id of the instance owner.</param>
        /// <param name="instanceGuid">The id of the instance to delete.</param>
        /// <param name="hard">Boolean to indicate if instance should be hard deleted.</param>
        /// <returns>Returns the deleted instance.</returns>
        Task<InstanceResponse> DeleteInstance(int instanceOwnerPartyId, Guid instanceGuid, bool hard);
    }
}
