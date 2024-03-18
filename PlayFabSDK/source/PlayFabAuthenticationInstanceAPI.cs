#if !DISABLE_PLAYFABENTITY_API

using PlayFab.AuthenticationModels;
using PlayFab.Internal;
#pragma warning disable 0649
using System;
// This is required for the Obsolete Attribute flag
//  which is not always present in all API's
#pragma warning restore 0649
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlayFab
{
    /// <summary>
    /// The Authentication APIs provide a convenient way to convert classic authentication responses into entity authentication
    /// models. These APIs will provide you with the entity authentication token needed for subsequent Entity API calls. The
    /// game_server API is designed to create uniquely identifiable game_server entities. The game_server Entity token can be
    /// used to call Matchmaking Lobby and Pubsub for server scenarios.
    /// </summary>
    public class PlayFabAuthenticationInstanceAPI
    {
        public readonly PlayFabApiSettings apiSettings = null;
        public readonly PlayFabAuthenticationContext authenticationContext = null;

        public PlayFabAuthenticationInstanceAPI()
        {
            authenticationContext = new PlayFabAuthenticationContext();
        }

        public PlayFabAuthenticationInstanceAPI(PlayFabApiSettings settings)
        {
            apiSettings = settings;
            authenticationContext = new PlayFabAuthenticationContext();
        }

        public PlayFabAuthenticationInstanceAPI(PlayFabAuthenticationContext context)
        {
            authenticationContext = context ?? new PlayFabAuthenticationContext();
        }

        public PlayFabAuthenticationInstanceAPI(PlayFabApiSettings settings, PlayFabAuthenticationContext context)
        {
            apiSettings = settings;
            authenticationContext = context ?? new PlayFabAuthenticationContext();
        }

        /// <summary>
        /// Verify entity login.
        /// </summary>
        public bool IsEntityLoggedIn()
        {
            return authenticationContext == null ? false : authenticationContext.IsEntityLoggedIn();
        }

        /// <summary>
        /// Clear the Client SessionToken which allows this Client to call API calls requiring login.
        /// A new/fresh login will be required after calling this.
        /// </summary>
        public void ForgetAllCredentials()
        {
            authenticationContext?.ForgetAllCredentials();
        }

        /// <summary>
        /// Create a game_server entity token and return a new or existing game_server entity.
        /// </summary>
        public async Task<PlayFabResult<AuthenticateCustomIdResult>> AuthenticateGameServerWithCustomIdAsync(AuthenticateCustomIdRequest request, object customData = null, Dictionary<string, string> extraHeaders = null)
        {
            await new PlayFabUtil.SynchronizationContextRemover();

            var requestContext = request?.AuthenticationContext ?? authenticationContext;
            var requestSettings = apiSettings ?? PlayFabSettings.staticSettings;
            if (requestContext.EntityToken == null) throw new PlayFabException(PlayFabExceptionCode.EntityTokenNotSet, "Must call Client Login or GetEntityToken before calling this method");

            var httpResult = await PlayFabHttp.DoPost("/GameServerIdentity/AuthenticateGameServerWithCustomId", request, "X-EntityToken", requestContext.EntityToken, extraHeaders, requestSettings);
            if (httpResult is PlayFabError)
            {
                var error = (PlayFabError)httpResult;
                PlayFabSettings.GlobalErrorHandler?.Invoke(error);
                return new PlayFabResult<AuthenticateCustomIdResult> { Error = error, CustomData = customData };
            }

            var resultRawJson = (string)httpResult;
            var resultData = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer).DeserializeObject<PlayFabJsonSuccess<AuthenticateCustomIdResult>>(resultRawJson);
            var result = resultData.data;
            var updateContext = authenticationContext;
            updateContext.EntityToken = result.EntityToken.EntityToken;
            updateContext.EntityId = result.EntityToken.Entity.Id;
            updateContext.EntityType = result.EntityToken.Entity.Type;

            return new PlayFabResult<AuthenticateCustomIdResult> { Result = result, CustomData = customData };
        }

        /// <summary>
        /// Delete a game_server entity.
        /// </summary>
        public async Task<PlayFabResult<EmptyResponse>> DeleteAsync(DeleteRequest request, object customData = null, Dictionary<string, string> extraHeaders = null)
        {
            await new PlayFabUtil.SynchronizationContextRemover();

            var requestContext = request?.AuthenticationContext ?? authenticationContext;
            var requestSettings = apiSettings ?? PlayFabSettings.staticSettings;
            if (requestContext.EntityToken == null) throw new PlayFabException(PlayFabExceptionCode.EntityTokenNotSet, "Must call Client Login or GetEntityToken before calling this method");

            var httpResult = await PlayFabHttp.DoPost("/GameServerIdentity/Delete", request, "X-EntityToken", requestContext.EntityToken, extraHeaders, requestSettings);
            if (httpResult is PlayFabError)
            {
                var error = (PlayFabError)httpResult;
                PlayFabSettings.GlobalErrorHandler?.Invoke(error);
                return new PlayFabResult<EmptyResponse> { Error = error, CustomData = customData };
            }

            var resultRawJson = (string)httpResult;
            var resultData = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer).DeserializeObject<PlayFabJsonSuccess<EmptyResponse>>(resultRawJson);
            var result = resultData.data;

            return new PlayFabResult<EmptyResponse> { Result = result, CustomData = customData };
        }

        /// <summary>
        /// Method to exchange a legacy AuthenticationTicket or title SecretKey for an Entity Token or to refresh a still valid
        /// Entity Token.
        /// </summary>
        public async Task<PlayFabResult<GetEntityTokenResponse>> GetEntityTokenAsync(GetEntityTokenRequest request, object customData = null, Dictionary<string, string> extraHeaders = null)
        {
            await new PlayFabUtil.SynchronizationContextRemover();

            var requestContext = request?.AuthenticationContext ?? authenticationContext;
            var requestSettings = apiSettings ?? PlayFabSettings.staticSettings;
            string authKey = null, authValue = null;
#if !DISABLE_PLAYFABCLIENT_API
            if (requestContext.ClientSessionTicket != null) { authKey = "X-Authorization"; authValue = requestContext.ClientSessionTicket; }
#endif

#if ENABLE_PLAYFABSERVER_API || ENABLE_PLAYFABADMIN_API || ENABLE_PLAYFAB_SECRETKEY
            if (requestSettings.DeveloperSecretKey != null) { authKey = "X-SecretKey"; authValue = requestSettings.DeveloperSecretKey; }
#endif

#if !DISABLE_PLAYFABENTITY_API
            if (requestContext.EntityToken != null) { authKey = "X-EntityToken"; authValue = requestContext.EntityToken; }
#endif

            var httpResult = await PlayFabHttp.DoPost("/Authentication/GetEntityToken", request, authKey, authValue, extraHeaders, requestSettings);
            if (httpResult is PlayFabError)
            {
                var error = (PlayFabError)httpResult;
                PlayFabSettings.GlobalErrorHandler?.Invoke(error);
                return new PlayFabResult<GetEntityTokenResponse> { Error = error, CustomData = customData };
            }

            var resultRawJson = (string)httpResult;
            var resultData = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer).DeserializeObject<PlayFabJsonSuccess<GetEntityTokenResponse>>(resultRawJson);
            var result = resultData.data;
            var updateContext = authenticationContext;
            updateContext.EntityToken = result.EntityToken;
            updateContext.EntityId = result.Entity.Id;
            updateContext.EntityType = result.Entity.Type;

            return new PlayFabResult<GetEntityTokenResponse> { Result = result, CustomData = customData };
        }

        /// <summary>
        /// Method for a server to validate a client provided EntityToken. Only callable by the title entity.
        /// </summary>
        public async Task<PlayFabResult<ValidateEntityTokenResponse>> ValidateEntityTokenAsync(ValidateEntityTokenRequest request, object customData = null, Dictionary<string, string> extraHeaders = null)
        {
            await new PlayFabUtil.SynchronizationContextRemover();

            var requestContext = request?.AuthenticationContext ?? authenticationContext;
            var requestSettings = apiSettings ?? PlayFabSettings.staticSettings;

            var entityToken = requestContext?.EntityToken ?? PlayFabSettings.staticPlayer.EntityToken;
            if ((entityToken) == null) throw new PlayFabException(PlayFabExceptionCode.EntityTokenNotSet, "Must call Client Login or GetEntityToken before calling this method");

            var httpResult = await PlayFabHttp.DoPost("/Authentication/ValidateEntityToken", request, "X-EntityToken", entityToken, extraHeaders, requestSettings);
            if (httpResult is PlayFabError)
            {
                var error = (PlayFabError)httpResult;
                PlayFabSettings.GlobalErrorHandler?.Invoke(error);
                return new PlayFabResult<ValidateEntityTokenResponse> { Error = error, CustomData = customData };
            }

            var resultRawJson = (string)httpResult;
            var resultData = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer).DeserializeObject<PlayFabJsonSuccess<ValidateEntityTokenResponse>>(resultRawJson);
            var result = resultData.data;

            return new PlayFabResult<ValidateEntityTokenResponse> { Result = result, CustomData = customData };
        }

}
}
#endif
