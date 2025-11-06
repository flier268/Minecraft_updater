using System;
using System.Diagnostics;

namespace Minecraft_updater.Services;

/// <summary>
/// Provides a single place to launch external URLs.
/// </summary>
public static class LinkNavigator
{
    /// <summary>
    /// Attempt to open the given URL using the system browser.
    /// </summary>
    /// <param name="url">Destination URL.</param>
    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true };

            Process.Start(psi);
        }
        catch (Exception)
        {
            // Swallow exceptions to avoid crashing the UI when navigation fails.
        }
    }
}
