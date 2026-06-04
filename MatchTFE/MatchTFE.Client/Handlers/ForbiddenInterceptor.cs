using Microsoft.AspNetCore.Components;
using System.Net;

namespace MatchTFE.Client.Handlers;

public class ForbiddenInterceptor : DelegatingHandler
{
    private readonly NavigationManager _navigationManager;

    public ForbiddenInterceptor(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _navigationManager.NavigateTo("/");
        }

        return response;
    }
}
