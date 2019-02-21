using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ReleaseBadge.GenerateBadge
{
    /// <summary>
    /// Class to generate the badge in the Azure Function
    /// </summary>
    public static class GetBadge
    {
        /// <summary>
        /// Generates a badge for a given release.
        /// 
        /// The badge is generated when the completed deployment event is received.
        /// 
        /// It generates a badge with the name of the environment and the name of the current release.
        /// 
        /// It has a different color based on the status of the deploy.
        /// 
        /// The badge is stored in a azure blob so it can be easily (and cheaply) accessed from anywhere.
        /// 
        /// By default it only creates a badge for successfull deploys. Pass the paramter X-EnableForAllStatus with value true to generate a badge for any status.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="binder"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("GenerateBadge")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, Binder binder, TraceWriter log)
        {
            log.Info($"Webhook was triggered!");

            var eventHelper = new DeploymentCompletedEventHelper(await req.Content.ReadAsStringAsync());
            var parameterHelper = new ConfigurationHelper(req);

            if (eventHelper.IsValidEvent())
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "invalid event" });
            }

            log.Info($"{eventHelper.Id} with status {eventHelper.Status} for environment {eventHelper.EnvironmentName}");

            if (eventHelper.Status != "succeeded" && parameterHelper.EnabledAllStatus() == false)
            {
                return req.CreateResponse(HttpStatusCode.OK, new { result = $"status {eventHelper.Status} ignored for for {eventHelper.Id}" });
            }

            var releaseIdentifier = GetReleaseIdentifier(parameterHelper, eventHelper);

            var badgeFileName = string.Format($"{eventHelper.GetTeamProjectName()}/{releaseIdentifier}-{eventHelper.EnvironmentName}.{parameterHelper.GetFileType()}");

            log.Info($"going to generate badge with name {badgeFileName}");

            var blobUri = await WriteBadgeToStorage(eventHelper, parameterHelper, binder, badgeFileName, parameterHelper.GetFileType());

            log.Info($"badge stored on {blobUri}");

            return req.CreateResponse(HttpStatusCode.OK, new { result = $"Generated {badgeFileName} for {eventHelper.Id} on {blobUri}" });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterHelper"></param>
        /// <param name="eventHelper"></param>
        /// <returns></returns>
        private static string GetReleaseIdentifier(ConfigurationHelper parameterHelper, DeploymentCompletedEventHelper eventHelper)
        {
            var releaseFriendlyName = parameterHelper.GetReleaseDefinitionFriendlyName();

            if (releaseFriendlyName != null)
                return releaseFriendlyName;

            return parameterHelper.UseReleaseName() ? eventHelper.GetReleaseName() : eventHelper.GetReleaseIdentifier();
        }

        /// <summary>
        /// Generates the badge and stores it on the BadgesBlob blob storage.
        /// 
        /// The BadgesBlob needs to be configured in the config files
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="configurationHelper"></param>
        /// <param name="binder"></param>
        /// <param name="badgeFileName"></param>
        /// <param name="contentType"></param>
        /// <returns>the uri of the blob the badge was written to</returns>
        private static async Task<string> WriteBadgeToStorage(DeploymentCompletedEventHelper helper, ConfigurationHelper configurationHelper, Binder binder, string badgeFileName, string contentType)
        {
            var badgeContent = await ShieldsIOBadgeGenerator.GenerateBadge(helper.ReleaseDefinitionName,
                                                                           helper.ReleaseName,
                                                                           helper.GetColor(),
                                                                           configurationHelper.GetFileType(),
                                                                           configurationHelper.GetStyle());

            var attributes = new Attribute[]
            {
                new BlobAttribute($"badges/{badgeFileName}", FileAccess.ReadWrite),
                new StorageAccountAttribute("BadgesBlob")
            };

            var maxAge = configurationHelper.GetMaxAge();

            var cloudBlob = await binder.BindAsync<CloudBlockBlob>(attributes);

            cloudBlob.Properties.CacheControl = $"public, max-age={maxAge}";
            cloudBlob.Properties.ContentType = contentType == "svg" ? $"image/{contentType}+xml" : $"image/{contentType}";

            await cloudBlob.UploadFromByteArrayAsync(badgeContent, 0, badgeContent.Length);

            return cloudBlob.Uri.AbsoluteUri;
        }
    }
}
