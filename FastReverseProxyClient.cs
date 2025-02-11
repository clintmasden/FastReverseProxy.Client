using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastReverseProxy.Client
{
    /// <summary>
    /// <see href="https://github.com/fatedier/frp"/>
    /// </summary>
    public class FastReverseProxyClient
    {
        private readonly HttpClient _httpClient;

        public FastReverseProxyClient(string baseAddress, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(baseAddress))
                throw new ArgumentNullException(nameof(baseAddress));

            _httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        #region FRPC (Client) Methods

        /// <summary>
        /// GET /api/status – Retrieves the client’s status.
        /// </summary>
        public async Task<Result<T>> GetClientStatus<T>(CancellationToken cancellationToken = default)
        {
            return await SendRequest<T>("/api/status", HttpMethod.Get, null, "application/json", cancellationToken);
        }

        /// <summary>
        /// GET /api/config – Retrieves the current client configuration.
        /// </summary>
        public async Task<Result<T>> GetClientConfig<T>(string contentType = "text/plain", CancellationToken cancellationToken = default)
        {
            return await SendRequest<T>("/api/config", HttpMethod.Get, null, contentType, cancellationToken);
        }

        /// <summary>
        /// PUT /api/config – Updates the client configuration.
        /// </summary>
        public async Task<Result<T>> UpdateClientConfig<T>(object config, string contentType = "text/plain", CancellationToken cancellationToken = default)
        {
            return await SendRequest<T>("/api/config", HttpMethod.Put, config, contentType, cancellationToken);
        }

        /// <summary>
        /// GET /api/reload – Reloads the client configuration.
        /// </summary>
        public async Task<Result<T>> ReloadClientConfig<T>(CancellationToken cancellationToken = default)
        {
            return await SendRequest<T>("/api/reload", HttpMethod.Get, null, "application/json", cancellationToken);
        }

        /// <summary>
        /// POST /api/stop – Stops the client. This is a fire‑and‑forget endpoint (an empty response is considered success).
        /// </summary>
        public async Task<Result<T>> StopClient<T>(CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/stop");
                await _httpClient.SendAsync(request, cancellationToken);
                return Result<T>.Success(default);
            }
            catch (TaskCanceledException)
            {
                return Result<T>.Success(default);
            }
            catch (Exception ex)
            {
                return Result<T>.Fail(ex.Message);
            }
        }

        #endregion

        #region FRPS (Server) Methods

        /// <summary>
        /// GET /api/serverinfo – Retrieves the server’s information.
        /// </summary>
        public async Task<Result<T>> GetServerInfo<T>(CancellationToken cancellationToken = default)
        {
            return await SendRequest<T>("/api/serverinfo", HttpMethod.Get, null, "application/json", cancellationToken);
        }

        /// <summary>
        /// GET /api/proxy/{type} – Retrieves proxies by type (e.g. "tcp", "https").
        /// </summary>
        public async Task<Result<T>> GetProxiesByType<T>(string type, CancellationToken cancellationToken = default)
        {
            return await SendRequest<T>($"/api/proxy/{type}", HttpMethod.Get, null, "application/json", cancellationToken);
        }

        /// <summary>
        /// GET /api/traffic/{name} – Retrieves traffic statistics for the specified proxy.
        /// </summary>
        public async Task<Result<T>> GetTrafficByProxy<T>(string proxyName, CancellationToken cancellationToken = default)
        {
            return await SendRequest<T>($"/api/traffic/{proxyName}", HttpMethod.Get, null, "application/json", cancellationToken);
        }

        /// <summary>
        /// POST /api/shutdown – Shuts down the server.
        /// </summary>
        //public async Task<Result<T>> ShutdownServer<T>(CancellationToken cancellationToken = default)
        //{
        //    return await SendRequest<T>("/api/shutdown", HttpMethod.Post, null, "application/json", cancellationToken);
        //}

        #endregion

        #region Internal Helper

        private async Task<Result<T>> SendRequest<T>(
            string endpoint,
            HttpMethod method,
            object content = null,
            string contentType = "application/json",
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new HttpRequestMessage(method, endpoint);

                if (content != null)
                {
                    StringContent stringContent;
                    if (content is string s &&
                        (contentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase)))
                    {
                        stringContent = new StringContent(s, Encoding.UTF8, contentType);
                    }
                    else
                    {
                        var jsonContent = JsonConvert.SerializeObject(content);
                        stringContent = new StringContent(jsonContent, Encoding.UTF8, contentType);
                    }
                    request.Content = stringContent;
                }
                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseData = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseData))
                {
                    if (typeof(T) == typeof(string))
                        return Result<T>.Success((T)(object)"");
                    return Result<T>.Success(default);
                }

                if (typeof(T) == typeof(string))
                    return Result<T>.Success((T)(object)responseData);

                var data = JsonConvert.DeserializeObject<T>(responseData);
                return Result<T>.Success(data);
            }
            catch (Exception ex)
            {
                return Result<T>.Fail(ex.Message);
            }
        }

        #endregion

        public class Result<T>
        {
            public T Data { get; }
            public string ErrorMessage { get; }
            public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);

            private Result(T data, string errorMessage)
            {
                Data = data;
                ErrorMessage = errorMessage;
            }

            public static Result<T> Success(T data) => new Result<T>(data, null);
            public static Result<T> Fail(string errorMessage) => new Result<T>(default, errorMessage);
        }
    }
}
