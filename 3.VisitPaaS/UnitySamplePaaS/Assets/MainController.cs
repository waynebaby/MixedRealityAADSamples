

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using System;
using System.Linq;
using Microsoft.Identity.Client;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;

public class MainController : MonoBehaviour
{
    public TextMeshPro CallResultText;
    public TextMeshPro TokenDetailText;
    public GameObject SignOutButton;
    public GameObject CallingGraphButton;
    public string[] GraphScopes;
    public string[] CustomeScopes;
    public string ClientId;
    public string Tenant;
    public string[] scopesStorage = new string[] { "https://storage.azure.com/user_impersonation" };
    public string storageAccountUrl = "https://mraaddemosa.blob.core.windows.net/";
    public string Authority => "https://login.microsoftonline.com/" + Tenant;
    public string MSGraphURL = "https://graph.microsoft.com/v1.0/";
    public string CustomAPIUrl = "https://mrapp1apifunctions.azurewebsites.net/api/Function1";
    private static IPublicClientApplication PublicClientApp;
    private static AuthenticationResult authResult;

    private void DisplayBasicTokenInfo(AuthenticationResult authResult)
    {
        TokenDetailText.text = "";
        if (authResult != null)
        {
            TokenDetailText.text += $"User Name: {authResult.Account.Username}" + Environment.NewLine;
            TokenDetailText.text += $"Token Expires: {authResult.ExpiresOn.ToLocalTime()}" + Environment.NewLine;
        }
    }



    private async Task DisplayMessageAsync(string message)
    {
        CallResultText.text = message;
    }
    private async Task<AuthenticationResult> SignInUserAndGetTokenUsingMSAL(string[] scopes)
    {

        PublicClientApp = PublicClientApplicationBuilder.Create(ClientId)
                    .WithAuthority(Authority)
#if ENABLE_WINMD_SUPPORT
                    .WithUseCorporateNetwork(true)
#endif
                    .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
                    .WithLogging((level, message, containsPii) =>
                        {
                            System.Diagnostics.Debug.WriteLine($"MSAL: {level} {message} ");
                        },
                        LogLevel.Warning, enablePiiLogging: false, enableDefaultPlatformLogging: true)
                    .Build();

        // It's good practice to not do work on the UI thread, so use ConfigureAwait(false) whenever possible.
        IEnumerable<IAccount> accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(true);
        IAccount firstAccount = accounts.FirstOrDefault();

        try
        {
            authResult = await PublicClientApp.AcquireTokenSilent(scopes, firstAccount)
                                              .ExecuteAsync();
        }
        catch (MsalUiRequiredException ex)
        {
            // A MsalUiRequiredException happened on AcquireTokenSilentAsync. This indicates you need to call AcquireTokenAsync to acquire a token
            System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

            try
            {
                authResult = await PublicClientApp.AcquireTokenInteractive(scopes)
                                                  .ExecuteAsync()
                                                  .ConfigureAwait(true);
            }
            catch (Exception exIntactive)
            {
                var msg = $"{exIntactive.GetType().Name}: {exIntactive.Message}";
                System.Diagnostics.Debug.WriteLine(exIntactive);
                await DisplayMessageAsync(msg);
                throw;
            }
        }
        return authResult;

    }

    /// <summary>
    /// Perform an HTTP GET request to a URL using an HTTP Authorization header
    /// </summary>
    /// <param name="url">The URL</param>
    /// <param name="token">The token</param>
    /// <returns>String containing the results of the GET operation</returns>
    public async Task<string> GetHttpContentWithToken(string url, string token)
    {
        var httpClient = new System.Net.Http.HttpClient();
        System.Net.Http.HttpResponseMessage response;
        try
        {
            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
            //Add the token in Authorization header
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
    public async void SignOutButton_Click()
    {

        IEnumerable<IAccount> accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(true);
        IAccount firstAccount = accounts.FirstOrDefault();

        try
        {
            await PublicClientApp.RemoveAsync(firstAccount).ConfigureAwait(true);
            CallResultText.text = "User has signed out";
            this.CallingGraphButton.SetActive(true);
            this.SignOutButton.SetActive(false);
        }
        catch (MsalException ex)
        {
            await DisplayMessageAsync($"Error signing out user: {ex.Message}");
        }

    }
    public async void CallGraphButton_Click()
    {
        try
        {
            var authResult = await SignInUserAndGetTokenUsingMSAL(GraphScopes);
            DisplayBasicTokenInfo(authResult);
            SignOutButton.SetActive(true);
            var outstring = await GetHttpContentWithToken("https://graph.microsoft.com/v1.0/me", authResult.AccessToken);
            await DisplayMessageAsync(outstring);
        }
        catch (Exception ex)
        {
            await DisplayMessageAsync(ex.Message);
        }

    }



    public async void CallApi_Click()
    {
        try
        {
            var token = await SignInUserAndGetTokenUsingMSAL(CustomeScopes);
            var result = await GetHttpContentWithToken(CustomAPIUrl, token.AccessToken);
            await DisplayMessageAsync(result);
            SignOutButton.SetActive(true);
        }
        catch (Exception ex)
        {
            await DisplayMessageAsync($"Error CallApi_Click:{System.Environment.NewLine}{ex}");
            return;
        }

    }

    public async void CallUpload_Click()
    {
        try
        {
            var token = await SignInUserAndGetTokenUsingMSAL(scopesStorage);
            var containerName = "targetcontainer";
            var sb = new StringBuilder();

            //Get Container Properties
            var existsResponse = await CallWithTokenAsync($"{storageAccountUrl}{containerName}?restype=container", HttpMethod.Get, token);
            if (existsResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var createContainerResponse = await CallWithTokenAsync($"{storageAccountUrl}{containerName}?restype=container", HttpMethod.Put, token);
                if (createContainerResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    sb.AppendLine($"Created container:{containerName}");
                }
                else
                {
                    sb.AppendLine($"Create container failed:{containerName}");
                }
            }
            else
            {
                sb.AppendLine($"Check container existed:{containerName}");
            }
            var blobName = Guid.NewGuid().ToString();
            var existGetBlobRequest = await CallWithTokenAsync($"{storageAccountUrl}{containerName}/{blobName}", HttpMethod.Get, token);
            if (existGetBlobRequest.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var createBlobResponse = await CallWithTokenAsync($"{storageAccountUrl}{containerName}/{blobName}", HttpMethod.Put, token,
                    req => req.Content = new ByteArrayContent(Guid.NewGuid().ToByteArray()));
                if (createBlobResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    sb.AppendLine($"Created blob:{blobName}");
                }
                else
                {
                    sb.AppendLine($"Create blob failed:{blobName}");
                }
            }
            else
            {
                sb.AppendLine($"Check blob existed:{containerName}");
            }
            await DisplayMessageAsync(sb.ToString());
            SignOutButton.SetActive(true);
        }
        catch (MsalException msalEx)
        {
            await DisplayMessageAsync($"Error Acquiring Token:{System.Environment.NewLine}{msalEx}");
        }
        catch (Exception ex)
        {
            await DisplayMessageAsync($"Error {nameof(CallUpload_Click)}:{System.Environment.NewLine}{ex}");
            return;
        }

    }

    private static async Task<HttpResponseMessage> CallWithTokenAsync(string url, HttpMethod method, AuthenticationResult token, Action<HttpRequestMessage> configRequest = null)
    {
        var httpClient = new HttpClient();
        HttpResponseMessage response;
        var request = new System.Net.Http.HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
        configRequest?.Invoke(request);
        response = await httpClient.SendAsync(request);
        return response;

    }
}