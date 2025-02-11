# FastReverseProxy.Client

A .NET Standard/C# implementation of the /fatedier/frp API.

## Important Notice

I welcome contributions in the form of pull requests (PRs) for any breaking changes or modifications. However, please ensure that all PRs include a comprehensive sample demonstrating the proposed changes. Upon verification and confirmation of the sample, a new version will be released.

## Resources

| Name       | Resources                                       |
|------------|-------------------------------------------------|
| APIs       | [API Documentation](https://github.com/fatedier/frp) |

## Getting Started

```csharp

using FastReverseProxy.Client;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Xunit;

namespace ExternalClient.Tests
{
    public class FastReverseProxyTests
    {
        public class FrpClientTests
        {
            private readonly FastReverseProxyClient _client;
            private readonly FastReverseProxyClient _server;

            public FrpClientTests()
            {
                // Initialize FastReverseProxyClient for both client and server.
                // (Ensure these URLs, ports, and credentials match your environment.)
                var clientUrl = "https://dummy-client.com/";
                var serverUrl = "https://dummy-server:7500";
                var username = "admin";
                var password = "admin";

                _client = new FastReverseProxyClient(clientUrl, username, password);
                _server = new FastReverseProxyClient(serverUrl, username, password);
            }

            #region FRPC (Client) Tests

            [Fact]
            public async Task GetClientStatus_ShouldReturnStatus()
            {
                var result = await _client.GetClientStatus<string>();
                Assert.True(result.IsSuccess, result.ErrorMessage);
                Assert.NotNull(result.Data);
            }

            [Fact]
            public async Task ReloadClientConfig_ShouldSucceed()
            {
                var result = await _client.ReloadClientConfig<string>();
                Assert.True(result.IsSuccess, result.ErrorMessage);
            }

            /// <summary>
            /// This works – /api/stop is fire-and-forget.
            /// </summary>
            //[Fact(Skip = "Manual test: run only when needed")]
            [Fact]
            public async Task StopClient_ShouldSucceed()
            {
                var result = await _client.StopClient<string>();
                Assert.True(result.IsSuccess, result.ErrorMessage);
            }

            [Fact]
            public async Task GetClientConfig_ShouldReturnConfig()
            {
                var result = await _client.GetClientConfig<string>();
                Assert.True(result.IsSuccess, result.ErrorMessage);
                Assert.NotNull(result.Data);
            }

            [Fact]
            public async Task UpdateClientConfig_ShouldSucceed()
            {
                // Retrieve the original configuration.
                var originalConfigResult = await _client.GetClientConfig<string>();
                Assert.True(originalConfigResult.IsSuccess, originalConfigResult.ErrorMessage);
                Assert.NotNull(originalConfigResult.Data);

                var config = new
                {
                    proxies = new[]
                    {
                        new
                        {
                            name = "example-proxy",
                            type = "tcp",
                            local_port = 8080,
                            remote_port = 80
                        }
                    }
                };

                var updateResult = await _client.UpdateClientConfig<string>(config);
                Assert.True(updateResult.IsSuccess, updateResult.ErrorMessage);

                var confirmConfigChangeResult = await _client.GetClientConfig<string>();
                Assert.True(confirmConfigChangeResult.IsSuccess, confirmConfigChangeResult.ErrorMessage);
                Assert.NotEqual(originalConfigResult.Data, confirmConfigChangeResult.Data);

                // Optionally revert to the original configuration.
                var revertResult = await _client.UpdateClientConfig<string>(originalConfigResult.Data);
                Assert.True(revertResult.IsSuccess, revertResult.ErrorMessage);
            }

            #endregion

            #region FRPS (Server) Tests

            [Fact]
            public async Task GetServerInfo_ShouldReturnInfo()
            {
                var result = await _server.GetServerInfo<string>();
                Assert.True(result.IsSuccess, result.ErrorMessage);
                Assert.NotNull(result.Data);
            }

            [Fact]
            public async Task GetProxiesByType_ShouldReturnProxies()
            {
                // For example, querying proxies of type "https".
                var result = await _server.GetProxiesByType<string>("https");
                Assert.True(result.IsSuccess, result.ErrorMessage);
                Assert.NotNull(result.Data);
            }

            [Fact]
            public async Task GetTrafficByProxy_ShouldReturnTraffic()
            {
                // Query traffic statistics for a proxy by its name.
                // (Replace "example-proxy" with a valid proxy name from your FRPS configuration.)
                var result = await _server.GetTrafficByProxy<string>("init-username.frp portal");
                Assert.True(result.IsSuccess, result.ErrorMessage);
                Assert.NotNull(result.Data);
            }

            //[Fact(Skip = "Manual test: run only when needed")]
            //[Fact]
            //public async Task ShutdownServer_ShouldSucceed()
            //{
            //    var result = await _server.ShutdownServer<string>();
            //    Assert.True(result.IsSuccess, result.ErrorMessage);
            //}

            #endregion
        }
    }
}


```
