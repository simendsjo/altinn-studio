using System;
using System.IO;
using System.Text;
using System.Text.Json;

using Altinn.App.Domain.Services.Interface;
using Altinn.App.Services.Configuration;
using Altinn.Platform.Storage.Interface.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Domain.Services.Implementation
{
    /// <summary>
    /// Class that exposes functionality for handling application
    /// </summary>
    public class ApplicationService : IApplicationService
    {
        private readonly AppSettings _settings;
        private readonly ILogger _logger;

        private readonly JsonSerializerOptions _serializerOptions;
        private Application _application;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationService"/> class.
        /// </summary>
        /// <param name="settings">The app repository settings.</param>
        /// <param name="logger">A logger from the built in logger factory.</param>
        public ApplicationService(
            IOptions<AppSettings> settings,
            ILogger<IApplicationService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                AllowTrailingCommas = true
            };
        }

        /// <inheritdoc/>    
        public Application GetApplication()
        {
            // Cache application metadata
            if (_application != null)
            {
                return _application;
            }

            string filedata = string.Empty;
            string filename = _settings.AppBasePath + _settings.ConfigurationFolder + _settings.ApplicationMetadataFileName;

            try
            {
                if (File.Exists(filename))
                {
                    filedata = File.ReadAllText(filename, Encoding.UTF8);
                }

                _application = JsonSerializer.Deserialize<Application>(filedata, _serializerOptions);
                return _application;
            }
            catch (Exception ex)
            {
                _logger.LogError("// ApplicationService failed to retrieve applicationMetadata. Exception {0}", ex);
                return null;
            }
        }
    }
}
