﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Common.Interfaces;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Common.Services
{
    public class StandardHttpClient : IHttpClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<StandardHttpClient> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StandardHttpClient(ILogger<StandardHttpClient> logger, IHttpContextAccessor httpContextAccessor)
        {
            _client = new HttpClient();
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetAuthenticationHeader(string scheme, string accessToken)
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, accessToken);
        }

        public async Task<HttpResponseMessage> GetAsync(string uri)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            return await _client.SendAsync(requestMessage);
        }

        public async Task<string> GetStringAsync(string uri)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await _client.SendAsync(requestMessage);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<HttpResponseMessage> DeleteAsync(string uri, string requestId = null)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            if (requestId != null)
            {
                requestMessage.Headers.Add("x-requestid", requestId);
            }

            return await _client.SendAsync(requestMessage);
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string uri, T item, string requestId = null)
        {
            return await DoPostPutAsync(HttpMethod.Post, uri, item, requestId);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string uri, T item, string requestId = null)
        {
            return await DoPostPutAsync(HttpMethod.Put, uri, item, requestId);
        }

        private async Task<HttpResponseMessage> DoPostPutAsync<T>(HttpMethod method, string uri, T item, string requestId = null)
        {
            if (method != HttpMethod.Post && method != HttpMethod.Put)
            {
                throw new ArgumentException("Value must be either post or put.", nameof(method));
            }

            // a new StringContent must be created for each retry
            // as it is disposed after each call

            var requestMessage = new HttpRequestMessage(method, uri);

            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(item), System.Text.Encoding.UTF8, "application/json");

            if (requestId != null)
            {
                requestMessage.Headers.Add("x-requestid", requestId);
            }

            var response = await _client.SendAsync(requestMessage);

            // raise exception if HttpResponseCode 500
            // needed for circuit breaker to track fails

            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                throw new HttpRequestException();
            }

            return response;
        }

        public async Task<HttpResponseMessage> SendFormUrlEncodedAsync(string uri, Dictionary<string, string> parameters)
        {
            HttpResponseMessage response = await _client.PostAsync(uri, new FormUrlEncodedContent(parameters));

            return response;
        }
    }
}
