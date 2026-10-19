using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UnityXrefMaps;

internal static class Utils
{
    /// <summary>
    /// Client for send HTTP requests and receiving HTTP responses.
    /// </summary>
    private static readonly HttpClient s_httpClient = new();

    /// <summary>
    /// Copy a source file to a destination file. Intermediate folders will be automatically created.
    /// </summary>
    /// <param name="sourcePath">The path of the source file to copy.</param>
    /// <param name="destPath">The destination path of the copied file.</param>
    public static async Task CopyFile(string sourcePath, string destPath, CancellationToken cancellationToken = default)
    {
        string? destDirectoryPath = Path.GetDirectoryName(destPath);

        Directory.CreateDirectory(destDirectoryPath!);

        using Stream source = File.OpenRead(sourcePath);
        using Stream destination = File.Create(destPath);

        await source.CopyToAsync(destination, cancellationToken);
    }

    /// <summary>
    /// Run a command in a hidden window and returns its output.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="arguments">The arguments of the command.</param>
    /// <param name="output">The function to call with the output data of the command.</param>
    /// <param name="error">The function to call with the error data of the command.</param>
    public static async Task<int> RunCommand(string command, string arguments, Action<string?> output, Action<string?> error, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(command, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        process.OutputDataReceived += (sender, args) => output(args.Data);
        process.ErrorDataReceived += (sender, args) => error(args.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode;
    }

    /// <summary>
    /// Maximum number of requests sent for a single URI when the server answers 429 or 503.
    /// </summary>
    private const int MaxAttempts = 5;

    /// <summary>
    /// Upper bound of the delay waited between two requests, whatever the server asks in Retry-After.
    /// </summary>
    private static readonly TimeSpan s_maxRetryDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Requests the specified URI with <see cref="s_httpClient"/> and returns if the response status code is in the
    /// range 200-299. Rate-limited (429) or unavailable (503) responses are retried up to <see cref="MaxAttempts"/>
    /// times, honoring Retry-After or backing off exponentially.
    /// </summary>
    /// <param name="uri">The URI to request.</param>
    /// <returns><c>true</c> if the response status code is in the range 200-299.</returns>
    public static async Task<bool> TestUriExists(string? uri, ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await s_httpClient.SendAsync(new(HttpMethod.Head, uri), cancellationToken);

            for (int attempt = 1; attempt < MaxAttempts && IsTransient(response.StatusCode); attempt++)
            {
                await Task.Delay(GetRetryDelay(response, attempt), cancellationToken);

                response.Dispose();
                response = await s_httpClient.SendAsync(new(HttpMethod.Head, uri), cancellationToken);
            }

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                logger.LogError("HTTP response code on {Uri} is {StatusCode}", uri, response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Exception on {Uri}", uri);

            return false;
        }
    }

    private static bool IsTransient(System.Net.HttpStatusCode statusCode) =>
        statusCode is System.Net.HttpStatusCode.TooManyRequests or System.Net.HttpStatusCode.ServiceUnavailable;

    /// <summary>
    /// Returns the delay asked by the Retry-After header, or 1s, 2s, 4s... when absent, capped at
    /// <see cref="s_maxRetryDelay"/>.
    /// </summary>
    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        TimeSpan delay = response.Headers.RetryAfter?.Delta
            ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow)
            ?? TimeSpan.FromSeconds(1 << (attempt - 1));

        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay > s_maxRetryDelay ? s_maxRetryDelay : delay;
    }
}
