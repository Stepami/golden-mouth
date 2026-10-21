using System.Net;
using System.Text;

namespace Reki.Tests.Generated;

public partial class SocialClient
{
    partial void ProcessResponse(HttpClient client, HttpResponseMessage response)
    {
        // Ok<ChallengeResponse?>(null) writes an empty 200 without a content type.
        // Normalize only this response for NSwag's JSON decoder; HTTP behavior stays unchanged.
        if (response.StatusCode == HttpStatusCode.OK &&
            response.Content.Headers.ContentType is null &&
            response.RequestMessage?.RequestUri?.OriginalString.EndsWith("challenges/active", StringComparison.Ordinal) == true)
        {
            response.Content.Dispose();
            response.Content = new StringContent("null", Encoding.UTF8, "application/json");
        }
    }
}
