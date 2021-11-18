using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MyFunctionAPI
{
    public static class Function1
    {
        
        [Function("Function1")]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            
             var logger = executionContext.GetLogger("Function1");

            var principal = new System.Security.Claims.ClaimsPrincipal(req.Identities);
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString($"Welcome, {principal.Identity.Name}, to Azure Functions!");
            response.WriteString(System.Environment.NewLine);
            foreach (var claim in principal.Claims)
            {
                response.WriteString($@"
{nameof(claim.Type)}:   
    {claim.Type}
{nameof(claim.ValueType)}:
    {claim.ValueType}
{nameof(claim.Value)}:
    {claim.Value}
{nameof(claim.Issuer)}:
    {claim.Issuer}
-------------------------");
            }
            return response;
        }
    }
}

