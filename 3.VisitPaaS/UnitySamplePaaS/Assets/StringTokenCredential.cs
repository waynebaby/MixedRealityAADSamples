using Azure.Core;
using Microsoft.Identity.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
public class StringTokenCredential : TokenCredential
{
    private readonly string tokenString;
    private readonly DateTimeOffset expires;
    public StringTokenCredential(AuthenticationResult authenticationResult)
    {
        tokenString = authenticationResult.AccessToken;
        expires = authenticationResult.ExpiresOn;
    }
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken(tokenString, expires);
    }

    public async override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken(tokenString, expires);
    }
}