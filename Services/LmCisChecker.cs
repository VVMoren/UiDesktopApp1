using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;

namespace UiDesktopApp1.Services
{
    public class LmCisChecker
    {
        private readonly HttpClient _httpClient;

        public LmCisChecker()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://127.0.0.1:5995")
            };

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:admin")));

            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", "27113567-53cf-4b80-9d1c-640c532f18d5");
        }

        public async Task<CisCheckResponse?> CheckCodesAsync(List<string> cisList, string fn = "9960440300120936")
        {
            var request = new CisCheckRequest
            {
                codes = cisList.ToArray(),
                fiscalDriveNumber = fn
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v4/true-api/codes/check", request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<CisCheckResponse>();
        }
    }
}
