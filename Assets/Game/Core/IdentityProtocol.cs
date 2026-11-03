using System;
namespace ScrapGuard.Core
{
    [Serializable] public class GoogleChallengeRequest {public bool link;public string secret;}
    [Serializable] public class GoogleChallenge {public string nonce,clientId;}
    [Serializable] public class GoogleLoginRequest {public string nonce,idToken,secret;public bool link;}
}
