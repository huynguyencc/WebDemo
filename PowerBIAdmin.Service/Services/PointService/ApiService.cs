using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PowerBIAdmin.Service
{   
    
    public static class HttpContentExtensions
    {
        public static async Task<T> ReadAsAsync<T>(this HttpContent content)
        {
            var rs = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(rs);
        }
    }
    

    public class ApiService
    {
        private const string TokenUtcExpiration = "TokenUtcExpiration";
        private const string Token = "Token";
        private string _token;
        private string _userName;
        private string _baseUrl;
        private string _password;
        private static string _acceptLanguage = "nb-NO";

        public void Initialize(string apiUrl, string username = "", string password = "", string token = "")
        {
            if (string.IsNullOrWhiteSpace(apiUrl)) throw(new ArgumentNullException(nameof(apiUrl)));
            //if (string.IsNullOrWhiteSpace(username)) throw (new ArgumentNullException(nameof(username)));
            //if (string.IsNullOrWhiteSpace(password)) throw (new ArgumentNullException(nameof(password)));
            apiUrl = apiUrl.TrimEnd('/').ToLower();
            if (!apiUrl.Contains("/webapi") && !apiUrl.EndsWith("/api/v1"))
                apiUrl = apiUrl + "/webapi/api/v1";
            else if(!apiUrl.EndsWith("/api/v1"))
                apiUrl = apiUrl + "/api/v1";
            _baseUrl = apiUrl + "/";
            _userName = username;
            _password = password;
            _token = token;
        }

        public async Task<IEnumerable<T>> GetManyAsync<T>(string url)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();

            ReauthenticateOn401(() =>
            {
                using (var client = CreateClient())
                {
                    responseMessage = client.GetAsync(url).Result;
                    return responseMessage.StatusCode;
                }
            });

            responseMessage.EnsureSuccessStatusCode();
            return await responseMessage.Content.ReadAsAsync<IEnumerable<T>>();
        }

        public async Task<T> GetAsync<T>(string url)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();

            ReauthenticateOn401(() =>
            {
                using (var client = CreateClient())
                {
                    responseMessage = client.GetAsync(url).Result;
                    return responseMessage.StatusCode;
                }
            });

            responseMessage.EnsureSuccessStatusCode();
            return await responseMessage.Content.ReadAsAsync<T>();
        }

        public async Task DeleteAsync(string url)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();

            ReauthenticateOn401(() =>
            {
                using (var client = CreateClient())
                {
                    responseMessage = client.DeleteAsync(url).Result;
                    return responseMessage.StatusCode;
                }
            });

            responseMessage.EnsureSuccessStatusCode();
            await Task.CompletedTask;
        }

        private ByteArrayContent CreateByteContent(object model)
        {
            var myContent = JsonConvert.SerializeObject(model);
            var buffer = Encoding.UTF8.GetBytes(myContent);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return byteContent;
        }

        public async Task<T> PutAsync<T>(string url, object model)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();

            ReauthenticateOn401(() =>
            {
                using (var client = CreateClient())
                {
                    var byteContent = CreateByteContent(model);
                    responseMessage = client.PutAsync(url, byteContent).Result;
                    return responseMessage.StatusCode;
                }
            });
            responseMessage.EnsureSuccessStatusCode();
            return await responseMessage.Content.ReadAsAsync<T>();
        }

        
        public async Task PutAsync(string url, object model)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();

            ReauthenticateOn401(() =>
            {
                using (var client = CreateClient())
                {
                    var byteContent = CreateByteContent(model);
                    responseMessage = client.PutAsync(url, byteContent).Result;
                    return responseMessage.StatusCode;
                }
            });
            responseMessage.EnsureSuccessStatusCode();
            await Task.CompletedTask;
        }
      

        public async Task<T> PostAsync<T>(string url, object model)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();

            ReauthenticateOn401(() =>
            {
                using (var client = CreateClient())
                {
                    var byteContent = CreateByteContent(model);
                    responseMessage = client.PostAsync(url, byteContent).Result;
                    return responseMessage.StatusCode;
                }
            });
            responseMessage.EnsureSuccessStatusCode();
            return await responseMessage.Content.ReadAsAsync<T>();
        }

        public async Task PostAsync(string url, object model)
        {
            await PostAsync<string>(url, model);
        }

        
        private HttpClient CreateClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(_acceptLanguage));
            client.Timeout = TimeSpan.FromMinutes(5); //set the timeout to 5 mins in cases server is busy.
            if (string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_userName)) // token expired, get the new one.
            {                
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.UTF8Encoding.UTF8.GetBytes(string.Format("{0}:{1}", _userName, _password))));
                var byteContent = CreateByteContent(new { });
                var response = client.PostAsync("authenticate", byteContent).Result;
                if (response.Headers.Any(x => x.Key == Token))
                {
                    _token = response.Headers.GetValues(Token).First();
                    var expireDate = DateTime.Parse(response.Headers.GetValues(TokenUtcExpiration).First());
                }                
            }

            client.DefaultRequestHeaders.TryAddWithoutValidation(Token, _token);
            return client;
        }
        

        private void ReauthenticateOn401(Func<HttpStatusCode> method)
        {
            if (method == null)
                throw new ArgumentNullException("action");

            int attemptsRemaining = 2;
            while (attemptsRemaining > 0)
            {
                var statusCode = method();
                if (statusCode == HttpStatusCode.Unauthorized)
                {
                    _token = null;
                    attemptsRemaining--;
                    if (attemptsRemaining > 0)
                    {
                        // _logger.Info("Token expired.");
                    }
                    else
                    {
                        // _logger.Error("Cannot get token. Please check the security settings or the api url.");
                    }
                }
                else
                {
                    attemptsRemaining = 0; //No need to retry if everything is ok.
                }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

    }
}