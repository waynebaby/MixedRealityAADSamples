
#define ENABLE_UWP_CODE

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using System;
#if ENABLE_UWP_CODE
using System.Linq;
//using Microsoft.Identity.Client;
using System.Diagnostics;
using System.Net.Http.Headers;

#endif

public class MainController : MonoBehaviour
{
    public TextMeshPro CallResultText;
    public TextMeshPro TokenDetailText;
    public GameObject SignOutButton;
    public GameObject CallingGraphButton;
    public string[] scopes;
    public string ClientId;
    public string Tenant;
    public string Authority => "https://login.microsoftonline.com/" + Tenant;
    public string MSGraphURL = "https://graph.microsoft.com/v1.0/";
#if ENABLE_UWP_CODE
    //private static IPublicClientApplication PublicClientApp;
    //private static AuthenticationResult authResult;
    //private void DisplayBasicTokenInfo(AuthenticationResult authResult)
    //{
    //    TokenDetailText.text = "";
    //    if (authResult != null)
    //    {
    //        TokenDetailText.text += $"User Name: {authResult.Account.Username}" + Environment.NewLine;
    //        TokenDetailText.text += $"Token Expires: {authResult.ExpiresOn.ToLocalTime()}" + Environment.NewLine;
    //    }
    //}
#endif


    private async Task DisplayMessageAsync(string message)
    {
        CallResultText.text = message;
    }
    private async Task<string> SignInUserAndGetTokenUsingMSAL(string[] scopes)
    {
#if ENABLE_UWP_CODE
        var outString = await IdentityClientPlugin.IdentityClient.SignInUserAndGetTokenUsingMSAL(scopes);
        return outString;
        //PublicClientApp = PublicClientApplicationBuilder.Create(ClientId)
        //           .WithAuthority(Authority)
        //           .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
        //            .WithLogging((level, message, containsPii) =>
        //            {
        //                System.Diagnostics.Debug.WriteLine($"MSAL: {level} {message} ");
        //            }, LogLevel.Warning, enablePiiLogging: false, enableDefaultPlatformLogging: true)
        //           .Build();

        //// It's good practice to not do work on the UI thread, so use ConfigureAwait(false) whenever possible.
        //IEnumerable<IAccount> accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(false);
        //IAccount firstAccount = accounts.FirstOrDefault();

        //try
        //{
        //    authResult = await PublicClientApp.AcquireTokenSilent(scopes, firstAccount)
        //                                      .ExecuteAsync();
        //}
        //catch (MsalUiRequiredException ex)
        //{
        //    // A MsalUiRequiredException happened on AcquireTokenSilentAsync. This indicates you need to call AcquireTokenAsync to acquire a token
        //    System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

        //    authResult = await PublicClientApp.AcquireTokenInteractive(scopes)
        //                                      .ExecuteAsync()
        //                                      .ConfigureAwait(false);

        //}
        //return authResult.AccessToken;
#else
        return "This_Is_A_Fake_Token_For_Editor_Player";
#endif
    }
    public async void SignOutButton_Click()
    {
#if ENABLE_UWP_CODE
        //IEnumerable<IAccount> accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(false);
        //IAccount firstAccount = accounts.FirstOrDefault();

        //try
        //{
        //    await PublicClientApp.RemoveAsync(firstAccount).ConfigureAwait(false);
        //    CallResultText.text = "User has signed out";
        //    this.CallingGraphButton.SetActive(true);
        //    this.SignOutButton.SetActive(false);
        //}
        //catch (MsalException ex)
        //{
        //    await DisplayMessageAsync($"Error signing out user: {ex.Message}");
        //}
#else
        CallResultText.text = "This is a fake signout";
        SignOutButton?.SetActive(false);
#endif
    }
    public async void CallGraphButton_Click()
    {
#if ENABLE_UWP_CODE
      var outstring=  await SignInUserAndGetTokenUsingMSAL(scopes);
        await DisplayMessageAsync(outstring);

#else
        CallResultText.text = "This is a fake  success calling ";
        SignOutButton?.SetActive(true);
#endif
    }
}