

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

}