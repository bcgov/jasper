using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Scv.Api.Documents.Conversion;

internal static class LibreOfficeProcessConfiguration
{
    private const string SecurityProfileResourceName =
        "Scv.Api.Documents.Conversion.LibreOfficeSecurityProfile.xcu";

    public static ProcessStartInfo CreateStartInfo(
        string executablePath,
        string inputPath,
        string outputDirectory,
        string profileDirectory,
        string workingDirectory,
        string pdfExportFilter)
    {
        var profileUri = new Uri(
            Path.GetFullPath(profileDirectory) + Path.DirectorySeparatorChar).AbsoluteUri;
        var startInfo = new ProcessStartInfo(executablePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--nodefault");
        startInfo.ArgumentList.Add("--norestore");
        // Do not use --safe-mode: it replaces the configured user profile and would
        // bypass the per-attempt security settings written below.
        startInfo.ArgumentList.Add($"-env:UserInstallation={profileUri}");
        startInfo.ArgumentList.Add("--convert-to");
        startInfo.ArgumentList.Add(pdfExportFilter);
        startInfo.ArgumentList.Add("--outdir");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add(inputPath);

        // Do not expose API credentials or unrelated application configuration to the child.
        var allowedEnvironment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PATH"] = Environment.GetEnvironmentVariable("PATH"),
            ["LANG"] = Environment.GetEnvironmentVariable("LANG"),
            ["LC_ALL"] = Environment.GetEnvironmentVariable("LC_ALL"),
            ["TZ"] = Environment.GetEnvironmentVariable("TZ"),
            ["LD_LIBRARY_PATH"] = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")
        };
        startInfo.Environment.Clear();
        foreach (var (name, value) in allowedEnvironment)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                startInfo.Environment[name] = value;
            }
        }

        // Keep LibreOffice state and temporary files inside the per-attempt directory.
        startInfo.Environment["HOME"] = workingDirectory;
        startInfo.Environment["TMPDIR"] = workingDirectory;
        startInfo.Environment["SAL_USE_VCLPLUGIN"] = "svp";
        return startInfo;
    }

    public static async Task WriteSecurityProfileAsync(
        string profileDirectory,
        CancellationToken cancellationToken)
    {
        var userDirectory = Path.Combine(profileDirectory, "user");
        Directory.CreateDirectory(userDirectory);
        var registryPath = Path.Combine(userDirectory, "registrymodifications.xcu");
        await using var source = typeof(LibreOfficeProcessConfiguration)
            .Assembly
            .GetManifestResourceStream(SecurityProfileResourceName)
            ?? throw new InvalidOperationException(
                "The LibreOffice security profile resource is missing.");
        await using var destination = new FileStream(
            registryPath,
            new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous
            });
        await source.CopyToAsync(destination, cancellationToken);
    }
}
