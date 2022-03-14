using Azure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UWPAADSample
{
    public class StringTokenCredential : TokenCredential
    {
        private readonly string tokenString;
        public StringTokenCredential(string tokenString)
        {
            if (string.IsNullOrWhiteSpace(tokenString))
            {
                throw new ArgumentException($"'{nameof(tokenString)}' cannot be null or whitespace.", nameof(tokenString));
            }
            this.tokenString = tokenString;
        }
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(tokenString, DateTimeOffset.Now.AddDays(1));
        }

        public async override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(tokenString, DateTimeOffset.Now.AddDays(1));
        }
    }
}
