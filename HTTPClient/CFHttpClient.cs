using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http; // for IFormFile

namespace HTTPClient
{
    public class CFHttpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CFHttpClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CreateClient(string clientKey)
        {
            return _httpClientFactory.CreateClient(clientKey);
        }

        /// <summary>
        /// Sends a GET request to retrieve complaints.
        /// </summary>
        public async Task<T?> GetAsync<T>(string url, string clientKey, string authToken = null, CancellationToken cancellationToken =
          default)
        {
            var client = CreateClient(clientKey);
            if (authToken != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var response = await client.GetAsync(url, cancellationToken);
            return await HandleResponse<T>(response, cancellationToken);
        }

        /// <summary>
        /// Sends a GET request using a request model converted to query string. Separate generic types for request and response.
        /// The request model's public readable properties (non-null) are turned into query parameters.
        /// Example: await GetAsync<MyFilter, MyDto>("https://api/items", filterModel);
        /// </summary>
        public async Task<TResponse?> GetAsync<TRequest, TResponse>(string url, TRequest requestModel, string clientKey, string authToken = null, CancellationToken cancellationToken = default)
        {
            var client = CreateClient(clientKey);
            if (authToken != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var finalUrl = AppendQueryString(url, requestModel);
            var response = await client.GetAsync(finalUrl, cancellationToken);
            return await HandleResponse<TResponse>(response, cancellationToken);
        }

        /// <summary>
        /// GET file content by path (query parameter 'path'). Returns response deserialized to TResponse.
        /// Example: await GetFileContentAsync<string>(baseUrl, "/folder/file.txt");
        /// </summary>
        //public async Task<byte[]?> GetFileContentAsync<TResponse>(string url, string path, string clientKey, string authToken = null, CancellationToken cancellationToken = default)
        //{
        //    var client = CreateClient(clientKey);
        //    if (authToken != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        //    var finalUrl = AppendSingleQuery(url, "fileName", path);
        //    var response = await client.GetAsync(finalUrl, cancellationToken);
        //    // Read the file content as byte array
        //    var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        //    return fileBytes;
        //}
        public async Task<byte[]?> GetFileContentAsync<TResponse>(string url, string clientKey, string authToken = null, CancellationToken cancellationToken = default)
        {
            var client = CreateClient(clientKey);
            if (authToken != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var response = await client.GetAsync(url, cancellationToken);
            // Read the file content as byte array
            var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            response.EnsureSuccessStatusCode();

            return fileBytes;
        }

        /// <summary>
        /// Sends a POST request with JSON body.
        /// </summary>
        public async Task<T?> PostJsonAsync<T>(string url, object data, string clientKey, string authToken = null, CancellationToken cancellationToken =
          default)
        {
            var client = CreateClient(clientKey);
            if (authToken != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var content = CreateJsonContent(data);
            var response = await client.PostAsync($"{url}", content, cancellationToken);
            return await HandleResponse<T>(response, cancellationToken);
        }

        /// <summary>
        /// Upload multiple IFormFile-like files to a single server path. Accepts IFormFile instances directly.
        /// Adds form field 'path' and file parts named 'files'.
        /// </summary>
        public async Task<TResponse?> PostFilesAsync<TResponse>(string url, IEnumerable<IFormFile> files, string path, string clientKey, string authToken = null, CancellationToken cancellationToken = default)
        {
            var client = CreateClient(clientKey);
            if (authToken != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var form = new MultipartFormDataContent();
            form.Add(new StringContent(path ?? string.Empty), "path");
            foreach (var file in files ?? Array.Empty<IFormFile>())
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;
                var part = new ByteArrayContent(ms.ToArray());
                part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                form.Add(part, "files", file.FileName ?? string.Empty);
            }
            var response = await client.PostAsync(url, form, cancellationToken);
            return await HandleResponse<TResponse>(response, cancellationToken);
        }

        /// <summary>
        /// Sends a POST request with form-data (custom multipart prepared outside).
        /// </summary>
        public async Task<T?> PostFormDataAsync<T>(string url, MultipartFormDataContent formData, string clientKey, CancellationToken cancellationToken =
          default)
        {
            var client = CreateClient(clientKey);
            var response = await client.PostAsync(url, formData, cancellationToken);
            return await HandleResponse<T>(response, cancellationToken);
        }

        public async Task<T?> PostFormUrlEncodedAsync<T>(string url, Dictionary<string, string> formData, string clientKey, string authToken = null, CancellationToken cancellationToken = default)
        {
            var client = CreateClient(clientKey);
            if (authToken != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var content = new FormUrlEncodedContent(formData);
            var response = await client.PostAsync(url, content, cancellationToken);
            return await HandleResponse<T>(response, cancellationToken);
        }

        /// <summary>
        /// Sends a PATCH request with JSON body.
        /// </summary>
        public async Task<T?> PatchJsonAsync<T>(string url, object data, string clientKey, CancellationToken cancellationToken =
          default)
        {
            var client = CreateClient(clientKey);
            var content = CreateJsonContent(data);
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = content
            };
            var response = await client.SendAsync(request, cancellationToken);
            return await HandleResponse<T>(response, cancellationToken);
        }

        /// <summary>
        /// Sends a PUT request with JSON body.
        /// </summary>
        public async Task<T?> PutJsonAsync<T>(string url, object data, string clientKey, CancellationToken cancellationToken =
          default)
        {
            var client = CreateClient(clientKey);
            var content = CreateJsonContent(data);
            var response = await client.PutAsync(url, content, cancellationToken);
            return await HandleResponse<T>(response, cancellationToken);
        }

        /// <summary>
        /// Sends a DELETE request.
        /// </summary>
        public async Task<bool> DeleteAsync(string url, string clientKey, CancellationToken cancellationToken =
          default)
        {
            var client = CreateClient(clientKey);
            var response = await client.DeleteAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Handles the HTTP response, deserializing JSON.
        /// </summary>
        private async Task<T?> HandleResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(content)!;
            }
            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize JSON response to type {typeof(T)}. Content: {content}", ex);
            }
        }

        private static StringContent CreateJsonContent(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static string AppendQueryString<TRequest>(string url, TRequest model)
        {
            var qs = ToQueryString(model);
            if (string.IsNullOrWhiteSpace(qs)) return url;
            var separator = url.Contains("?") ? '&' : '?';
            return $"{url}{separator}{qs}";
        }

        private static string ToQueryString<TRequest>(TRequest model)
        {
            if (model == null) return string.Empty;
            var props = typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);
            var pairs = new List<string>();
            foreach (var p in props)
            {
                var value = p.GetValue(model);
                if (value == null) continue;
                if (value is System.Collections.IEnumerable enumerable && value is not string)
                {
                    var listVals = new List<string>();
                    foreach (var item in enumerable)
                    {
                        if (item != null)
                            listVals.Add(Uri.EscapeDataString(Convert.ToString(item)!));
                    }
                    if (listVals.Count == 0) continue;
                    pairs.Add($"{Uri.EscapeDataString(p.Name)}={string.Join(",", listVals)}");
                }
                else
                {
                    pairs.Add($"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(Convert.ToString(value)!)}");
                }
            }
            return string.Join('&', pairs);
        }

        private static string AppendSingleQuery(string url, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return url;
            var encoded = Uri.EscapeDataString(value ?? string.Empty);
            var separator = url.Contains('?') ? '&' : '?';
            return $"{url}{separator}{Uri.EscapeDataString(key)}={encoded}";
        }
    }
}
