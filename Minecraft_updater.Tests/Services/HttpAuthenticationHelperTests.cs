using System;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Minecraft_updater.Models;
using Minecraft_updater.Services;
using Xunit;

namespace Minecraft_updater.Tests.Services
{
    public class HttpAuthenticationHelperTests
    {
        [Fact]
        public void CreateAuthenticatedGetRequest_WithBasicMode_ShouldSetAuthorizationHeader()
        {
            // Arrange
            var options = new DownloadAuthenticationOptions
            {
                Mode = DownloadAuthenticationMode.Basic,
                Username = "user",
                Password = "pass",
            };

            // Act
            using var request = HttpAuthenticationHelper.CreateAuthenticatedGetRequest(
                "https://example.com/resource",
                options
            );

            // Assert
            request.Headers.Authorization.Should().NotBeNull();
            request.Headers.Authorization!.Scheme.Should().Be("Basic");
            var expectedParameter = Convert.ToBase64String(
                Encoding.UTF8.GetBytes("user:pass")
            );
            request.Headers.Authorization.Parameter.Should().Be(expectedParameter);
        }

        [Fact]
        public void CreateAuthenticatedGetRequest_WithApiKeyHeader_ShouldAppendHeader()
        {
            // Arrange
            var options = new DownloadAuthenticationOptions
            {
                Mode = DownloadAuthenticationMode.ApiKeyHeader,
                HeaderName = "X-Api-Key",
                HeaderValue = "secret",
            };

            // Act
            using var request = HttpAuthenticationHelper.CreateAuthenticatedGetRequest(
                "https://example.com/resource",
                options
            );

            // Assert
            request.Headers.TryGetValues("X-Api-Key", out var values).Should().BeTrue();
            values.Should().ContainSingle().Which.Should().Be("secret");
        }

        [Fact]
        public void CreateAuthenticatedGetRequest_WithApiKeyQuery_ShouldAppendQuery()
        {
            // Arrange
            var options = new DownloadAuthenticationOptions
            {
                Mode = DownloadAuthenticationMode.ApiKeyQuery,
                QueryParameterName = "token",
                QueryParameterValue = "abc123",
            };

            // Act
            using var request = HttpAuthenticationHelper.CreateAuthenticatedGetRequest(
                "https://example.com/resource",
                options
            );

            // Assert
            request.RequestUri.Should().Be(new Uri("https://example.com/resource?token=abc123"));

            var sanitized = HttpAuthenticationHelper.GetSanitizedUrlForLogging(
                request.RequestUri,
                options
            );
            sanitized.Should().Be("https://example.com/resource?token=<redacted>");
        }
    }
}
