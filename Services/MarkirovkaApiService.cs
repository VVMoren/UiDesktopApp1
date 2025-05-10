using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UiDesktopApp1.Helpers;
using UiDesktopApp1.Views.Windows;

namespace UiDesktopApp1.Services
{
    public class MarkirovkaApiService
    {
        private readonly HttpClient _client = new();
        private readonly string keyUrl = "https://markirovka.crpt.ru/api/v3/auth/cert/key";
        private readonly string certUrl = "https://markirovka.crpt.ru/api/v3/auth/cert/";
        private readonly string tokenPath = @"C:\Users\VVMor\source\repos\UiDesktopApp1-master\Resources\Token.txt";

        // Проверка токена
        public async Task<string> GetTokenWithRetryAsync()
        {
            string token = ReadTokenFromFile();

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    using var testClient = new HttpClient();
                    testClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                    var testRequest = new HttpRequestMessage(HttpMethod.Get, "https://markirovka.crpt.ru/api/v3/testEndpoint");
                    LogHelper.WriteLog("Проверка токена - запрос", $"URL: {testRequest.RequestUri}\nHeaders: {string.Join(", ", testRequest.Headers)}");

                    var testResponse = await testClient.SendAsync(testRequest);
                    var testResponseContent = await testResponse.Content.ReadAsStringAsync();

                    LogHelper.WriteLog("Проверка токена - ответ", $"Status: {testResponse.StatusCode}\nContent: {testResponseContent}");

                    if (testResponse.IsSuccessStatusCode)
                    {
                        return token;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLog("Ошибка проверки токена", ex.ToString());
                    MessageBox.Show($"Ошибка проверки токена: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // 🔄 Обновление иконки токена
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                        mainWindow.UpdateStatusIcons();
                });
            }

            token = await GetTokenAsync();
            File.WriteAllText(tokenPath, token);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.UpdateStatusIcons();
                }
            });


            return token;
        }

        // Получение Auth токена
        public async Task<HttpResponseMessage> SendAuthorizedRequestAsync(HttpRequestMessage request)
        {
            string token = await GetTokenWithRetryAsync();
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            LogHelper.WriteLog("Авторизованный запрос",
                $"URL: {request.RequestUri}\n" +
                $"Method: {request.Method}\n" +
                $"Headers: {string.Join(", ", request.Headers)}\n" +
                $"Content: {(request.Content != null ? await request.Content.ReadAsStringAsync() : "null")}");

            var response = await _client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            LogHelper.WriteLog("Ответ на авторизованный запрос",
                $"Status: {response.StatusCode}\n" +
                $"Content: {responseContent}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                token = await GetTokenAsync();
                File.WriteAllText(tokenPath, token);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                LogHelper.WriteLog("Повторный авторизованный запрос (после 401)",
                    $"URL: {request.RequestUri}\n" +
                    $"Method: {request.Method}\n" +
                    $"Headers: {string.Join(", ", request.Headers)}\n" +
                    $"Content: {(request.Content != null ? await request.Content.ReadAsStringAsync() : "null")}");

                response = await _client.SendAsync(request);
                responseContent = await response.Content.ReadAsStringAsync();

                LogHelper.WriteLog("Ответ на повторный запрос",
                    $"Status: {response.StatusCode}\n" +
                    $"Content: {responseContent}");
            }

            return response;
        }

        // Подпись данных
        public async Task<string> GetTokenAsync()
        {
            // Запрос к keyUrl
            var keyRequest = new HttpRequestMessage(HttpMethod.Get, keyUrl);
            LogHelper.WriteLog("Запрос ключа для подписи", $"URL: {keyRequest.RequestUri}");

            var keyResponse = await _client.SendAsync(keyRequest);
            var keyResponseContent = await keyResponse.Content.ReadAsStringAsync();

            LogHelper.WriteLog("Ответ на запрос ключа",
                $"Status: {keyResponse.StatusCode}\n" +
                $"Content: {keyResponseContent}");

            if (!keyResponse.IsSuccessStatusCode)
            {
                LogHelper.WriteLog("Ошибка получения ключа", keyResponseContent);
                MessageBox.Show($"Ошибка от keyUrl:\n{keyResponseContent}", "Ошибка сервера",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                throw new Exception($"Ошибка получения данных: {keyResponse.StatusCode}\n{keyResponseContent}");
            }

            var jsonData = JObject.Parse(keyResponseContent);
            string uuid = jsonData["uuid"].ToString();
            string data = jsonData["data"].ToString();

            string tempDir = Path.GetTempPath();
            string dataPath = Path.Combine(tempDir, "data.txt");
            string signPath = Path.Combine(tempDir, "data_sign.txt");
            File.WriteAllText(dataPath, data);

            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "ГОЛУБЕЦ ВЛАДИСЛАВ ВИТАЛЬЕВИЧ", false);
            if (certs.Count == 0)
            {
                LogHelper.WriteLog("Ошибка поиска сертификата", "Сертификат не найден в хранилище.");
                throw new Exception("Сертификат не найден в хранилище.");
            }

            var cert = certs[0];
            string dn = cert.SubjectName.Name;

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c G:\\py\\cryptcp.win32.exe -sign -dn \"{dn}\" \"{dataPath}\" \"{signPath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };

            LogHelper.WriteLog("Подпись данных", $"Запуск cryptcp с параметрами: {process.StartInfo.Arguments}");

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                LogHelper.WriteLog("Ошибка подписи данных", $"Код ошибки: {process.ExitCode}");
                throw new Exception("Ошибка выполнения команды cryptcp");
            }

            string signedData = File.ReadAllText(signPath).Replace("\r", "").Replace("\n", "");

            // Запрос к certUrl
            var requestData = new { uuid, data = signedData };
            var postContent = new StringContent(JsonConvert.SerializeObject(requestData),
                System.Text.Encoding.UTF8, "application/json");

            var certRequest = new HttpRequestMessage(HttpMethod.Post, certUrl)
            {
                Content = postContent
            };

            LogHelper.WriteLog("Запрос токена",
                $"URL: {certRequest.RequestUri}\n" +
                $"Content: {await certRequest.Content.ReadAsStringAsync()}");

            var certResponse = await _client.SendAsync(certRequest);
            var certResponseContent = await certResponse.Content.ReadAsStringAsync();

            LogHelper.WriteLog("Ответ на запрос токена",
                $"Status: {certResponse.StatusCode}\n" +
                $"Content: {certResponseContent}");

            if (!certResponse.IsSuccessStatusCode)
            {
                LogHelper.WriteLog("Ошибка получения токена", certResponseContent);
                MessageBox.Show($"Ошибка от certUrl:\n{certResponseContent}", "Ошибка сервера",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                throw new Exception($"Ошибка при получении токена: {certResponse.StatusCode}\n{certResponseContent}");
            }

            var token = JObject.Parse(certResponseContent)["token"].ToString();
            File.WriteAllText(tokenPath, token);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.UpdateStatusIcons();
                }
            });


            LogHelper.WriteLog("Токен получен", $"Токен сохранен в {tokenPath}");

            return token;
        }

        // Чтение токена
        public string ReadTokenFromFile()
        {
            if (!File.Exists(tokenPath))
            {
                LogHelper.WriteLog("Чтение токена", "Файл токена не найден");
                return null;
            }

            try
            {
                var token = File.ReadAllText(tokenPath).Trim();
                LogHelper.WriteLog("Чтение токена", "Токен успешно прочитан из файла");
                return token;
            }
            catch (IOException ex)
            {
                LogHelper.WriteLog("Ошибка чтения токена", ex.ToString());
                MessageBox.Show($"Ошибка чтения токена: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // Поиск чеков ККТ с фильтрами
        public async Task<JArray> SearchReceiptsV4Async(string pg, int limit)
        {
            try
            {
                var token = await GetTokenWithRetryAsync();

                var url = $"https://markirovka.crpt.ru/api/v4/true-api/receipt/list?" +
                          $"pg={Uri.EscapeDataString(pg)}&" +
                          $"limit={limit}";


                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token}");

                LogHelper.WriteLog("Поиск чеков - запрос",
                    $"URL: {request.RequestUri}\n" +
                    $"Headers: {string.Join(", ", request.Headers)}");

                var response = await _client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                LogHelper.WriteLog("Поиск чеков - ответ",
                    $"Status: {response.StatusCode}\n" +
                    $"Content: {json}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = $"Ошибка поиска чеков: {response.StatusCode}";
                    LogHelper.WriteLog("Ошибка API", errorMessage);
                    throw new HttpRequestException(errorMessage);
                }

                var result = JObject.Parse(json);
                return result["results"] as JArray ?? new JArray();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("Ошибка в SearchReceiptsV4Async", ex.ToString());
                throw;
            }
        }

        // Получение информации о конкретном чеке по ID
        public async Task<JArray> GetReceiptInfoV4Async(string receiptId, string pg, bool withBody = true, bool withContent = true)
        {
            try
            {
                var token = await GetTokenWithRetryAsync();

                var url = $"https://markirovka.crpt.ru/api/v4/true-api/receipt/{receiptId}/info?" +
                          //$"pg={Uri.EscapeDataString(pg)}&" +
                          $"body={withBody}";
                //$"content={withContent}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token}");

                LogHelper.WriteLog("Получение информации о чеке - запрос",
                    $"URL: {request.RequestUri}\n" +
                    $"Headers: {string.Join(", ", request.Headers)}");

                var response = await _client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                LogHelper.WriteLog("Получение информации о чеке - ответ",
                    $"Status: {response.StatusCode}\n" +
                    $"Content: {json}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = $"Ошибка получения чека: {response.StatusCode}";
                    LogHelper.WriteLog("Ошибка API", errorMessage);
                    throw new HttpRequestException(errorMessage);
                }

                return JArray.Parse(json);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("Ошибка в GetReceiptInfoV4Async", ex.ToString());
                throw;
            }
        }

        public class CisInfoWrapper
        {
            [JsonProperty("cisInfo")]
            public CisInfo CisInfo { get; set; }
        }

        public class CisInfo
        {
            [JsonProperty("requestedCis")]
            public string RequestedCis { get; set; }

            [JsonProperty("cisWithoutBrackets")]
            public string CisWithoutBrackets { get; set; }

            [JsonProperty("productName")]
            public string ProductName { get; set; }
        }



        public async Task<List<CisInfo>> GetCisesInfoAsync(List<string> cisList)
        {
            var url = "https://markirovka.crpt.ru/api/v3/true-api/cises/info";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(cisList), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenWithRetryAsync());

            var response = await _client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            LogHelper.WriteLog("Ответ от /cises/info", $"Status: {response.StatusCode}\nContent: {json}");

            // Десериализуем в обёртку
            var rawList = JsonConvert.DeserializeObject<List<CisInfoWrapper>>(json);

            return rawList
                .Where(x => x.CisInfo != null && !string.IsNullOrWhiteSpace(x.CisInfo.ProductName))
                .Select(x => x.CisInfo)
                .ToList();
        }
    }
}


