using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnityXrefMaps.Tests
{
    public class UtilsTests
    {
        private const int RateLimitedRequestCount = 2;

        [Theory]
        [InlineData(RateLimitedRequestCount + 1, true)]
        [InlineData(RateLimitedRequestCount, false)]
        public async Task TestUriExists_RateLimited_RetriesUpToMaxAttempts(int maxAttempts, bool expected)
        {
            int port = GetFreePort();

            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();

            int requestCount = 0;

            // Answers 429 (Retry-After: 0) RateLimitedRequestCount times, then 200.
            Task server = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        HttpListenerContext context = await listener.GetContextAsync();

                        if (Interlocked.Increment(ref requestCount) <= RateLimitedRequestCount)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                            context.Response.Headers.Add("Retry-After", "0");
                        }

                        context.Response.Close();
                    }
                }
                catch (HttpListenerException)
                {
                    // Listener stopped.
                }
                catch (ObjectDisposedException)
                {
                    // Listener disposed.
                }
            }, TestContext.Current.CancellationToken);

            bool exists = await Utils.TestUriExists(
                $"http://127.0.0.1:{port}/",
                maxAttempts,
                NullLogger.Instance,
                TestContext.Current.CancellationToken);

            listener.Stop();
            await server;

            Assert.Equal(expected, exists);
            Assert.Equal(maxAttempts, requestCount);
        }

        private static int GetFreePort()
        {
            using var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();

            return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        }
    }
}
