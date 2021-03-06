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
using Azure.Storage.Blobs;
using Azure.Core;
using System.Net.Http;

namespace UWPAADSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        //Set the scope for API call to user.read
        private string[] scopesGraph = new string[] { "https://graph.microsoft.com/User.Read" };
        private string[] scopesCustom = new string[] { "api://ea9a5da2-e951-40c0-814c-d0536d3707a7/user_impersonation" };
        private string customeAPIUrl = "https://mrapp1apifunctions.azurewebsites.net/api/Function1";
        private string[] scopesStorage = new string[] { "https://storage.azure.com/user_impersonation" };
        private string storageAccountUrl = "https://mraaddemosa.blob.core.windows.net/";

        // Below are the clientId (Application Id) of your app registration and the tenant information.
        // You have to replace:
        // - the content of ClientID with the Application Id for your app registration
        private const string ClientId = "d76b333f-f49d-455e-b2eb-1110f9c21a19";
        private const string Tenant = "AACMRTestAAD1outlook.onmicrosoft.com"; // Alternatively "[Enter your tenant, as obtained from the Azure portal, e.g. kko365.onmicrosoft.com]"
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
                GraphServiceClient graphClient = await SignInAndInitializeGraphServiceClient(scopesGraph);

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
                await DisplayMessageAsync($"Error CallGraphButton_Click:{System.Environment.NewLine}{ex}");
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



        private async void CallApi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var token = await SignInUserAndGetTokenUsingMSAL(scopesCustom);
                var result = await GetHttpContentWithToken(customeAPIUrl, token);
                await DisplayMessageAsync(result);
                this.SignOutButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                await DisplayMessageAsync($"Error {nameof(CallApi_Click)}:{System.Environment.NewLine}{ex}");
                return;
            }

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

        async private void CallUpload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var token = await SignInUserAndGetTokenUsingMSAL(scopesStorage);
                var sb = new StringBuilder();
                BlobServiceClient service = new BlobServiceClient(new Uri(storageAccountUrl), new StringTokenCredential(token));
                BlobContainerClient container = service.GetBlobContainerClient("targetcontainer");
                if (!await container.ExistsAsync())
                {
                    var containerResponse = await container.CreateAsync();
                    sb.AppendLine($"Created container:{container.Name} at{containerResponse.Value.LastModified }");
                }
                var blob = container.GetBlobClient(Guid.NewGuid().ToString());
                //if (!await blob.ExistsAsync())
                //{
                var uploadResponse = await blob.UploadAsync(new BinaryData(Guid.NewGuid().ToByteArray()));
                sb.AppendLine($"Created container:{blob.Name} at{uploadResponse.Value.LastModified }");
                //}
                var content = await blob.DownloadContentAsync();
                var contentGuid = new Guid(content.Value.Content.ToArray());
                sb.AppendLine($"File {blob.Name},  Content:{contentGuid}");




                await DisplayMessageAsync(sb.ToString());
                this.SignOutButton.Visibility = Visibility.Visible;
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

        private static async Task<HttpResponseMessage> CallWithTokenAsync(string url, HttpMethod method, string token, Action<HttpRequestMessage> configRequest = null)
        {
            var httpClient = new HttpClient();
            HttpResponseMessage response;
            var request = new System.Net.Http.HttpRequestMessage(method, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            configRequest?.Invoke(request);
            response = await httpClient.SendAsync(request);
            return response;

        }
    }
}

