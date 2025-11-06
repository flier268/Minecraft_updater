using System;

namespace Minecraft_updater.Models
{
    /// <summary>
    /// Supported authentication strategies for HTTP downloads.
    /// </summary>
    public enum DownloadAuthenticationMode
    {
        None,
        Basic,
        BearerToken,
        ApiKeyHeader,
        ApiKeyQuery,
    }

    /// <summary>
    /// Describes the authentication configuration applied to outbound HTTP requests.
    /// </summary>
    public sealed record DownloadAuthenticationOptions
    {
        private const string ConfigSection = "Minecraft_updater";

        public DownloadAuthenticationMode Mode { get; init; } = DownloadAuthenticationMode.None;
        public string? Username { get; init; }
        public string? Password { get; init; }
        public string? BearerToken { get; init; }
        public string? HeaderName { get; init; }
        public string? HeaderValue { get; init; }
        public string? QueryParameterName { get; init; }
        public string? QueryParameterValue { get; init; }

        public bool IsConfigured => Mode != DownloadAuthenticationMode.None;

        public static DownloadAuthenticationOptions FromIni(IniFile ini)
        {
            if (ini == null)
            {
                throw new ArgumentNullException(nameof(ini));
            }

            var modeValue = ini.IniReadValue(ConfigSection, "DownloadAuthType");
            var mode = DownloadAuthenticationMode.None;
            if (!string.IsNullOrWhiteSpace(modeValue))
            {
                Enum.TryParse(modeValue, true, out mode);
            }

            var options = new DownloadAuthenticationOptions
            {
                Mode = mode,
                Username = ReadOptional(ini, "DownloadAuthUsername"),
                Password = ReadOptional(ini, "DownloadAuthPassword"),
                BearerToken = ReadOptional(ini, "DownloadAuthBearerToken"),
                HeaderName = ReadOptional(ini, "DownloadAuthHeaderName"),
                HeaderValue = ReadOptional(ini, "DownloadAuthHeaderValue"),
                QueryParameterName = ReadOptional(ini, "DownloadAuthQueryName"),
                QueryParameterValue = ReadOptional(ini, "DownloadAuthQueryValue"),
            };

            return Normalize(options);
        }

        private static string? ReadOptional(IniFile ini, string key)
        {
            var value = ini.IniReadValue(ConfigSection, key);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static DownloadAuthenticationOptions Normalize(
            DownloadAuthenticationOptions options
        )
        {
            if (!options.IsConfigured)
            {
                return options with { Mode = DownloadAuthenticationMode.None };
            }

            switch (options.Mode)
            {
                case DownloadAuthenticationMode.Basic:
                    if (string.IsNullOrWhiteSpace(options.Username))
                    {
                        return options with
                        {
                            Mode = DownloadAuthenticationMode.None,
                            Username = null,
                            Password = null,
                        };
                    }
                    break;
                case DownloadAuthenticationMode.BearerToken:
                    if (string.IsNullOrWhiteSpace(options.BearerToken))
                    {
                        return options with
                        {
                            Mode = DownloadAuthenticationMode.None,
                            BearerToken = null,
                        };
                    }
                    break;
                case DownloadAuthenticationMode.ApiKeyHeader:
                    if (
                        string.IsNullOrWhiteSpace(options.HeaderName)
                        || options.HeaderValue == null
                    )
                    {
                        return options with
                        {
                            Mode = DownloadAuthenticationMode.None,
                            HeaderName = null,
                            HeaderValue = null,
                        };
                    }
                    break;
                case DownloadAuthenticationMode.ApiKeyQuery:
                    if (
                        string.IsNullOrWhiteSpace(options.QueryParameterName)
                        || options.QueryParameterValue == null
                    )
                    {
                        return options with
                        {
                            Mode = DownloadAuthenticationMode.None,
                            QueryParameterName = null,
                            QueryParameterValue = null,
                        };
                    }
                    break;
            }

            return options;
        }
    }
}
