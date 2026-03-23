using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;

namespace MatchTFE.Client.Handlers
{
    public class AuthInterceptor : DelegatingHandler
    {
        private readonly NavigationManager _navigationManager;
        private readonly IHttpClientFactory _clientFactory;
        private bool _isRefreshing = false;

        public AuthInterceptor(NavigationManager navigationManager, IHttpClientFactory clientFactory)
        {
            _navigationManager = navigationManager;
            _clientFactory = clientFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized && !_isRefreshing)
            {
                _isRefreshing = true;

                try
                {
                    var cleanClient = _clientFactory.CreateClient("CleanAPI");

                    var refreshResponse = await cleanClient.PostAsJsonAsync("api/auth/refresh", new { });

                    if (refreshResponse.IsSuccessStatusCode)
                    {
                        var retryRequest = await CloneRequestAsync(request);

                        response = await base.SendAsync(retryRequest, cancellationToken);
                    }
                    else
                    {
                        _navigationManager.NavigateTo("/login");
                    }
                }
                catch (Exception)
                {
                    _navigationManager.NavigateTo("/login");
                }
                finally
                {
                    _isRefreshing = false;
                }
            }

            return response;
        }

        private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = await CloneContentAsync(request.Content),
                Version = request.Version
            };

            foreach (var prop in request.Options)
                clone.Options.Set(new HttpRequestOptionsKey<object?>(prop.Key), prop.Value);

            foreach (var header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }

        private async Task<HttpContent?> CloneContentAsync(HttpContent? content)
        {
            if (content == null) return null;

            var ms = new MemoryStream();
            await content.CopyToAsync(ms);
            ms.Position = 0;

            var clone = new StreamContent(ms);
            foreach (var header in content.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }
    }
}