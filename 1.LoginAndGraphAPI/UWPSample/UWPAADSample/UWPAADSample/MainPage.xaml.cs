using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Windows.Security.Authentication.Web;
using Windows.Security.Credentials;
using Windows.Security.Authentication.Web.Core;
using System.Text;
using System.IO;

namespace UWPAADSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        //Set the scope for API call to user.read
        private string[] scopes = new string[] { "user.read" };

        // Below are the clientId (Application Id) of your app registration and the tenant information.
        // You have to replace:
        // - the content of ClientID with the Application Id for your app registration
        private const string ClientId = "da357db4-ae75-4d9d-bf1c-520a8e7d4a40";
        //private const string ClientId = "d76b333f-f49d-455e-b2eb-1110f9c21a19";

        private const string Tenant = "microsoft.onmicrosoft.com";//
        //private const string Tenant = "AACMRTestAAD1outlook.onmicrosoft.com"; // Alternatively "[Enter your tenant, as obtained from the Azure portal, e.g. kko365.onmicrosoft.com]"
        private const string Authority = "https://login.microsoftonline.com/" + Tenant;

        // The MSAL Public client app
        private static IPublicClientApplication PublicClientApp;

        private static string MSGraphURL = "https://graph.microsoft.com/v1.0/";
        private static AuthenticationResult authResult;

        public MainPage()
        {
            this.InitializeComponent();
        }
        /// <summary>
        /// Sign in user using MSAL and obtain a token for Microsoft Graph
        /// </summary>
        /// <returns>GraphServiceClient</returns>
        private async static Task<GraphServiceClient> SignInAndInitializeGraphServiceClient(string[] scopes)
        {
            GraphServiceClient graphClient = new GraphServiceClient(MSGraphURL,
                new DelegateAuthenticationProvider(async (requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", await SignInUserAndGetTokenUsingMSAL(scopes));
                }));

            return graphClient;
        }

        /// <summary>
        /// Sign in user using MSAL and obtain a token for Microsoft Graph
        /// </summary>
        /// <returns>GraphServiceClient</returns>
        private async static Task<GraphServiceClient> SignInAndInitializeGraphServiceClientWAML(string[] scopes)
        {
            GraphServiceClient graphClient = new GraphServiceClient(MSGraphURL,
                new DelegateAuthenticationProvider(async (requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", await SignInUserAndGetTokenUsingWAML(scopes[0]));
                }));

            return graphClient;
        }
        /// <summary>
        /// Displays a message in the ResultText. Can be called from any thread.
        /// </summary>
        private async Task DisplayMessageAsync(string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    ResultText.Text = message;
                });
        }
        /// <summary>
        /// Display basic information contained in the token. Needs to be called from the UI thread.
        /// </summary>
        private void DisplayBasicTokenInfo(AuthenticationResult authResult)
        {
            TokenInfoText.Text = "";
            if (authResult != null)
            {
                TokenInfoText.Text += $"User Name: {authResult.Account.Username}" + Environment.NewLine;
                TokenInfoText.Text += $"Token Expires: {authResult.ExpiresOn.ToLocalTime()}" + Environment.NewLine;
            }
        }
        /// <summary>
        /// Sign out the current user
        /// </summary>
        private async void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<IAccount> accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(false);
            IAccount firstAccount = accounts.FirstOrDefault();

            try
            {
                await PublicClientApp.RemoveAsync(firstAccount).ConfigureAwait(false);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ResultText.Text = "User has signed out";
                    this.CallGraphButton.Visibility = Visibility.Visible;
                    this.SignOutButton.Visibility = Visibility.Collapsed;
                });
            }
            catch (MsalException ex)
            {
                ResultText.Text = $"Error signing out user: {ex.Message}";
            }
        }
        /// <summary>
        /// Call AcquireTokenAsync - to acquire a token requiring user to sign in
        /// </summary>
        private async void CallGraphButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Sign in user using MSAL and obtain an access token for Microsoft Graph
                GraphServiceClient graphClient = await SignInAndInitializeGraphServiceClient(scopes);

                // Call the /me endpoint of Graph
                User graphUser = await graphClient.Me.Request().GetAsync();

                // Go back to the UI thread to make changes to the UI
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ResultText.Text = "Display Name: " + graphUser.DisplayName + "\nBusiness Phone: " + graphUser.BusinessPhones.FirstOrDefault()
                                      + "\nGiven Name: " + graphUser.GivenName + "\nid: " + graphUser.Id
                                      + "\nUser Principal Name: " + graphUser.UserPrincipalName;
                    DisplayBasicTokenInfo(authResult);
                    this.SignOutButton.Visibility = Visibility.Visible;
                });
            }
            catch (MsalException msalEx)
            {
                await DisplayMessageAsync($"Error Acquiring Token:{System.Environment.NewLine}{msalEx}");
            }
            catch (Exception ex)
            {
                await DisplayMessageAsync($"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
                return;
            }
        }
        /// <summary>
        /// Call AcquireTokenAsync - to acquire a token requiring user to sign in
        /// </summary>
        private async void CallGraphButtonWAML_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Sign in user using MSAL and obtain an access token for Microsoft Graph
                GraphServiceClient graphClient = await SignInAndInitializeGraphServiceClientWAML(scopes);

                // Call the /me endpoint of Graph
                User graphUser = await graphClient.Me.Request().GetAsync();

                // Go back to the UI thread to make changes to the UI
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ResultText.Text = "Display Name: " + graphUser.DisplayName + "\nBusiness Phone: " + graphUser.BusinessPhones.FirstOrDefault()
                                      + "\nGiven Name: " + graphUser.GivenName + "\nid: " + graphUser.Id
                                      + "\nUser Principal Name: " + graphUser.UserPrincipalName;
                    DisplayBasicTokenInfo(authResult);
                    this.SignOutButton.Visibility = Visibility.Visible;
                });
            }
            catch (MsalException msalEx)
            {
                await DisplayMessageAsync($"Error Acquiring Token:{System.Environment.NewLine}{msalEx}");
            }
            catch (Exception ex)
            {
                await DisplayMessageAsync($"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
                return;
            }
        }
        /// <summary>
        /// Signs in the user and obtains an access token for Microsoft Graph
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns> Access Token</returns>
        private static async Task<string> SignInUserAndGetTokenUsingMSAL(string[] scopes)
        {
            // Initialize the MSAL library by building a public client application
            PublicClientApp = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority(Authority)
                .WithUseCorporateNetwork(true)
                .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
                 .WithLogging((level, message, containsPii) =>
                 {
                     Debug.WriteLine($"MSAL: {level} {message} ");
                 }, LogLevel.Warning, enablePiiLogging: false, enableDefaultPlatformLogging: true)
                .Build();

            // It's good practice to not do work on the UI thread, so use ConfigureAwait(false) whenever possible.
            IEnumerable<IAccount> accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(false);
            IAccount firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await PublicClientApp.AcquireTokenSilent(scopes, firstAccount)
                    
                                                  .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilentAsync. This indicates you need to call AcquireTokenAsync to acquire a token
                Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");
           
             
                    authResult = await PublicClientApp.AcquireTokenInteractive(scopes)
                    
                                                .ExecuteAsync()
                                                .ConfigureAwait(false);
                

            }
            return authResult.AccessToken;
        }

        /// <summary>
        /// Signs in the user and obtains an access token for Microsoft Graph
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns> Access Token</returns>
        private static async Task<string> SignInUserAndGetTokenUsingWAML(string scopes)
        {
            var BiometricsRequired = true;
            // Initialize the MSAL library by building a public client application
            string accessToken = string.Empty;

            if (BiometricsRequired)
            {
                if (await Windows.Security.Credentials.UI.UserConsentVerifier.CheckAvailabilityAsync() == Windows.Security.Credentials.UI.UserConsentVerifierAvailability.Available)
                {
                    var consentResult = await Windows.Security.Credentials.UI.UserConsentVerifier.RequestVerificationAsync("Please verify your credentials");
                    if (consentResult != Windows.Security.Credentials.UI.UserConsentVerificationResult.Verified)
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }


            string URI = string.Format("ms-appx-web://Microsoft.AAD.BrokerPlugIn/{0}",
                WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper());

            WebAccountProvider wap =
                await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", Authority);




            WebTokenRequest wtr = new WebTokenRequest(wap, "https://graph.microsoft.com/.default", ClientId);
            wtr.Properties.Add("resource", "https://graph.microsoft.com/.default");

            WebAccount account = null;

            //if (!string.IsNullOrEmpty((string)userId))
            //{
            //    account = await WebAuthenticationCoreManager.FindAccountAsync(wap, (string)userId);
            //    if (account != null)
            //    {
            //        Logger.Log("Found account: " + account.UserName);
            //    }
            //    else
            //    {
            //        Logger.Log("Account not found");
            //    }
            //}

            WebTokenRequestResult tokenResponse = null;
            try
            {
                if (account != null)
                {
                    tokenResponse = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(wtr, account);
                }
                else
                {
                    tokenResponse = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(wtr);
                    //tokenResponse = await WebAuthenticationCoreManager.RequestTokenAsync(wtr);
                }
            }
            catch (Exception ex)
            {
                throw;
                //Logger.Log(ex.Message);
            }

            if (tokenResponse.ResponseError != null)
            {

                var sb = new StringBuilder("ResponseError:");
                foreach (var errProp in tokenResponse.ResponseError.Properties)
                {
                    sb.AppendLine($"Error prop: ({errProp.Key}, {errProp.Value})");
                }
                throw new Exception(sb.ToString());
            }

            if (tokenResponse.ResponseStatus == WebTokenRequestStatus.UserInteractionRequired)
            {
                WebTokenRequestResult wtrr = null;
                try
                {
                    if (account != null)
                        wtrr = await WebAuthenticationCoreManager.RequestTokenAsync(wtr, account);
                    else
                        wtrr = await WebAuthenticationCoreManager.RequestTokenAsync(wtr);
                }
                catch (Exception ex)
                {
                    throw;
                }
                if (wtrr.ResponseError != null)
                {
                    var sb = new StringBuilder("ResponseError:");

                    sb.AppendLine("Error Code: " + wtrr.ResponseError.ErrorCode.ToString());
                    sb.AppendLine("Error Msg: " + wtrr.ResponseError.ErrorMessage.ToString());
                    foreach (var errProp in wtrr.ResponseError.Properties)
                    {
                        sb.AppendLine($"Error prop: ({errProp.Key}, {errProp.Value})");
                    }
                    throw new Exception(sb.ToString());
                }

                if (wtrr.ResponseStatus == WebTokenRequestStatus.Success)
                {
                    accessToken = wtrr.ResponseData[0].Token;
                    account = wtrr.ResponseData[0].WebAccount;
                    var properties = wtrr.ResponseData[0].Properties;
                    //Username = account.UserName;
                    //Logger.Log($"Username = {Username}");
                    var ras = await account.GetPictureAsync(WebAccountPictureSize.Size64x64);
                    var stream = ras.AsStreamForRead();
                    var br = new BinaryReader(stream);
                    //UserPicture = br.ReadBytes((int)stream.Length);

                    //Logger.Log("Access Token: " + accessToken, false);
                }
            }

            if (tokenResponse.ResponseStatus == WebTokenRequestStatus.Success)
            {
                foreach (var resp in tokenResponse.ResponseData)
                {
                    var name = resp.WebAccount.UserName;
                    accessToken = resp.Token;
                    account = resp.WebAccount;
                    //Username = account.UserName;
                    //Logger.Log($"Username = {Username}");
                    try
                    {
                        var ras = await account.GetPictureAsync(WebAccountPictureSize.Size64x64);
                        var stream = ras.AsStreamForRead();
                        var br = new BinaryReader(stream);
                        //UserPicture = br.ReadBytes((int)stream.Length);
                    }
                    catch (Exception ex)
                    {
                        throw;
                        //Logger.Log($"Exception when reading image {ex.Message}");
                    }
                }

                //Logger.Log("Access Token: " + accessToken, false);
            }

            //if (account != null && !string.IsNullOrEmpty(account.Id))
            //{
            //    Store.SaveUser(UserIdKey, account.Id);
            //}
            //#endif
            return accessToken;
        }
    }

}