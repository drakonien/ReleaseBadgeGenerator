using Newtonsoft.Json;

namespace ReleaseBadge.GenerateBadge
{
    /// <summary>
    /// Helper class that abstracts access to the Deployment completed event.
    /// </summary>
    internal class DeploymentCompletedEventHelper
    {
        private readonly dynamic _data;

        #region Properties

        public string Id => (string)_data.id;

        public string Status => (string)_data.resource.environment.status;

        public string EnvironmentName => (string)_data.resource.environment.name;

        public string ReleaseDefinitionName => (string)_data.resource.environment.releaseDefinition.name;

        public string ReleaseName => (string)_data.resource.environment.release.name;

        #endregion

        #region Constructor

        /// <summary>
        /// ctor. Receives the JSON content of the event
        /// 
        /// Only works with deployment completed events
        /// </summary>
        /// <param name="jsonContent"></param>
        public DeploymentCompletedEventHelper(string jsonContent)
        {
            _data = JsonConvert.DeserializeObject(jsonContent);
        }

        #endregion

        /// <summary>
        /// is the event valid?
        /// </summary>
        /// <returns>true if it is, false otherwise</returns>
        public bool IsValidEvent()
        {
            return _data != null && _data.id != null && _data.eventType != "ms.vss-release.deployment-completed-event";
        }

        /// <summary>
        /// Gets the color based on the release status
        /// </summary>
        /// <returns>green if status is "succedded", yellow if is "partiallySucceeded" and red otherwise</returns>
        public string GetColor()
        {
            switch ((string)_data.resource.environment.status)
            {
                case "succeeded":
                    return "green";
                case "partiallySucceeded":
                    return "yellow";
                case "failed":
                default:
                    return "red";
            }
        }

        /// <summary>
        /// The release identifier is composed of the project guid plus the release definition id
        /// </summary>
        /// <returns></returns>
        internal string GetReleaseIdentifier()
        {
            return _data.resource.environment.releaseDefinition.id;
        }

        /// <summary>
        /// Gets the Team Project name
        /// </summary>
        /// <returns>the id of the project</returns>
        internal string GetTeamProjectName()
        {
            return _data.resource.project.id;
        }

        /// <summary>
        /// Gets the Release name
        /// </summary>
        /// <returns>the nam of the "releaseDefinition"</returns>
        internal string GetReleaseName()
        {
            return _data.resource.environment.releaseDefinition.name;
        }
    }
}
