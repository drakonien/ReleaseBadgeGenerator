using System;
using System.Linq;
using System.Net.Http;

namespace ReleaseBadge.GenerateBadge
{
    /// <summary>
    /// Helper to get configuration value.
    /// 
    /// The value is fetched from HTTP headers and if not defined it is fetched from application settings.
    /// </summary>
    internal class ConfigurationHelper
    {
        private readonly HttpRequestMessage _req;
        
        #region Constructor

        public ConfigurationHelper(HttpRequestMessage req)
        {
            _req = req;
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Predicate to check if badges should be generated for any status.
        /// 
        /// If disable only sucessfull deploys generated a badge and other status are ignored.
        /// </summary>
        /// <returns>true if the bagde should be generated for any status, false otherwise</returns>
        internal bool EnabledAllStatus()
        {
            return GetConfigurationValue("EnableForAllStatus", "false").ToLower() == "true"; 
        }

        /// <summary>
        /// Get badge style
        /// </summary>
        /// <returns></returns>
        internal string GetStyle()
        {
            return GetConfigurationValue("Style", null);
        }

        /// <summary>
        /// Get badge file type (png, gif, svg...)
        /// 
        /// Default value is PNG
        /// </summary>
        /// <returns>the badge file type if specified, "png" as default value</returns>
        internal string GetFileType()
        {
            return GetConfigurationValue("FileType", "png");
        }

        /// <summary>
        /// Gets release definition friendly name. Use an alternate name for the badge release definition name
        /// </summary>
        /// <returns></returns>
        internal string GetReleaseDefinitionFriendlyName()
        {
            return _req.Headers.Contains("X-ReleaseDefinitionFileFriendlyName") ? _req.Headers.GetValues("X-ReleaseDefinitionFileFriendlyName").FirstOrDefault() : null;
        }

        /// <summary>
        /// Gets badge cache (seconds) duration
        /// </summary>
        /// <returns>the cache duration if specified, 15 as default value</returns>
        internal string GetMaxAge()
        {
            return GetConfigurationValue("MaxAge", "15");
        }

        /// <summary>
        /// Predicate to check if we should use release name to generate the badge filename
        /// 
        /// default value: true
        /// </summary>
        /// <returns>true if the release name should be used and false otherwise</returns>
        internal bool UseReleaseName()
        {
            return GetConfigurationValue("UseReleaseName", "false").ToLower() == "true";
        }

        /// <summary>
        /// Gets a configuration value from the HTTP headers and if not defined fetched it
        /// from application settings.
        /// 
        /// On HTTP header the header should be X-{settingName}
        /// </summary>
        /// <param name="settingName">the name of the setting</param>
        /// <param name="defaultValue">The default value to return if no configuration value is found</param>
        /// <returns>configured value or default value if not defined</returns>
        internal string GetConfigurationValue(string settingName, string defaultValue)
        {
            var headerName = "X-" + settingName;

            if (_req.Headers.Contains(headerName))
            {
                return _req.Headers.GetValues(headerName).FirstOrDefault();
            }

            return GetApplicationSetting(settingName) ?? defaultValue;
        }

        /// <summary>
        /// Gets an azure function application setting value
        /// </summary>
        /// <param name="settingName">name of the setting to recover</param>
        /// <returns>Value of the setting</returns>
        internal static string GetApplicationSetting(string settingName)
        {
            return Environment.GetEnvironmentVariable(settingName);
        }

        #endregion
    }
}