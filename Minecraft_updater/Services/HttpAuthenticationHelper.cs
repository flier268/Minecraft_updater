using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Minecraft_updater.Models;

namespace Minecraft_updater.Services
{
    /// <summary>
    /// Utility methods for applying authentication settings to HTTP requests.
    /// </summary>
    public static class HttpAuthenticationHelper
    {
        public static HttpRequestMessage CreateAuthenticatedGetRequest(
            string url,
            DownloadAuthenticationOptions? options
        )
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL cannot be null or whitespace.", nameof(url));
            }

            var requestUri = AppendQueryParameterIfNeeded(url, options);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            ApplyHeaders(request, options);

            return request;
        }

        public static string GetSanitizedUrlForLogging(
            Uri? uri,
            DownloadAuthenticationOptions? options
        )
        {
            if (uri == null)
            {
                return string.Empty;
            }

            if (
                options?.Mode == DownloadAuthenticationMode.ApiKeyQuery
                && !string.IsNullOrWhiteSpace(options.QueryParameterName)
            )
            {
                var builder = new UriBuilder(uri);
                var query = builder.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    var trimmed = query.TrimStart('?');
                    var parts = trimmed.Split(
                        '&',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    );
                    for (var i = 0; i < parts.Length; i++)
                    {
                        var kvp = parts[i].Split('=', 2);
                        if (
                            kvp.Length == 2
                            && string.Equals(
                                kvp[0],
                                options.QueryParameterName,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            parts[i] = $"{kvp[0]}=<redacted>";
                        }
                    }

                    builder.Query = string.Join("&", parts);
                    return builder.Uri.ToString();
                }
            }

            return uri.ToString();
        }

        private static void ApplyHeaders(
            HttpRequestMessage request,
            DownloadAuthenticationOptions? options
        )
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (options == null || !options.IsConfigured)
            {
                return;
            }

            switch (options.Mode)
            {
                case DownloadAuthenticationMode.Basic:
                    if (!string.IsNullOrEmpty(options.Username))
                    {
                        var password = options.Password ?? string.Empty;
                        var credentialBytes = Encoding.UTF8.GetBytes(
                            $"{options.Username}:{password}"
                        );
                        request.Headers.Authorization = new AuthenticationHeaderValue(
                            "Basic",
                            Convert.ToBase64String(credentialBytes)
                        );
                    }
                    break;

                case DownloadAuthenticationMode.BearerToken:
                    if (!string.IsNullOrWhiteSpace(options.BearerToken))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue(
                            "Bearer",
                            options.BearerToken
                        );
                    }
                    break;

                case DownloadAuthenticationMode.ApiKeyHeader:
                    if (
                        !string.IsNullOrWhiteSpace(options.HeaderName)
                        && options.HeaderValue != null
                    )
                    {
                        request.Headers.Remove(options.HeaderName);
                        request.Headers.TryAddWithoutValidation(
                            options.HeaderName,
                            options.HeaderValue
                        );
                    }
                    break;
            }
        }

        private static string AppendQueryParameterIfNeeded(
            string url,
            DownloadAuthenticationOptions? options
        )
        {
            if (options == null || !options.IsConfigured)
            {
                return url;
            }

            if (
                options.Mode != DownloadAuthenticationMode.ApiKeyQuery
                || string.IsNullOrWhiteSpace(options.QueryParameterName)
                || options.QueryParameterValue == null
            )
            {
                return url;
            }

            var separator = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            return $"{url}{separator}{Uri.EscapeDataString(options.QueryParameterName)}={Uri.EscapeDataString(options.QueryParameterValue)}";
        }
    }
}
