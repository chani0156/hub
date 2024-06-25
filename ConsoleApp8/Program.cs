using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR.Client;
using System.Reflection;
using Newtonsoft.Json;
using CSharpFunctionalExtensions;

namespace ConsoleApp8
{
    class Program
    {

        private static string BaseUriFormat = "http://{0}/api/";
        private static string BaseHttpsUriFormat = "https://{0}/api/";
        private const string LoginSuffix = "Admin/Login";
        private const string LogoutSuffix = "Admin/Logout";
        private const string DevicesSuffix = "Devices";
        private const string JsonMediaType = "application/json";
        private static string serverAddress = "62.90.222.249:10001";
        private const string RelatedDeviceSuffix = "Employees/SetRelatedDevice/"; //http://172.20.42.105:10000/api/Employees/SetRelatedDevice/AAAA
        private const string GetEmployeesSuffix = "Employees/";
        //private const string _pass = "test123";
        private const string _pass = "^raRoFAl747*";
        private static HttpClient mClient = new HttpClient();
        private static JwtSecurityToken mToken;

        private static readonly TimeSpan TimeoutSpan = TimeSpan.FromMinutes(1);

        private static bool bUseHttps = false;
        private static string certificateFileName;

        static void Main(string[] args)
        {
            Console.WriteLine("\r\nsEnter server IP and port");
            string str = Console.ReadLine();
            if (str.Length > 4)
                serverAddress = str;
            Console.WriteLine("\r\nstart...");
            //Result<JwtSecurityToken> result = Login("Driver", "test123");           
            TrainRoutine();

        }


        private static void TrainRoutine()
        {
            //  Result<JwtSecurityToken> result = Login("Driver", _pass);
              Result<JwtSecurityToken> result = Login("test", "test123");

            if (result.IsSuccess)
            {
                mToken = result.Value;

                using (HttpClient client = new HttpClient())
                {
                    //http://172.20.42.81:10001/api/Admin/GetMovies
                    //http://62.90.222.249:10001
                    string url = $"{string.Format(BaseUriFormat, serverAddress)}Admin/GetMovies";
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + mToken.RawData);
                    Task<HttpResponseMessage> responseTask = client.GetAsync(url);
                    responseTask.Wait(TimeoutSpan);
                    Console.WriteLine($"StatusCode: {responseTask.Result.StatusCode},{responseTask.Result.ReasonPhrase}");
                    if (responseTask.Result.StatusCode == HttpStatusCode.OK)
                    {
                        Task<string> contentTask = responseTask.Result.Content.ReadAsStringAsync();
                        string retVal = contentTask.Result;
                        //  File.WriteAllText(@"Movies", retVal, Encoding.UTF8);
                        Console.WriteLine(retVal);
                    }
                }

                string hubAddress = $"http://{serverAddress}/ClientHub";

                var connectionDriver = new HubConnectionBuilder().WithUrl(hubAddress, options =>
                {
                    options.AccessTokenProvider = async () => { return mToken.RawData; };
                }).WithAutomaticReconnect().Build();
                connectionDriver.On<dynamic>("DataReceived", (message) =>
                {
                    Console.WriteLine(message);
                });
                connectionDriver.StartAsync().GetAwaiter().GetResult();

                Console.WriteLine("waiting...");
                ConsoleKeyInfo cki;
                do
                {
                    Thread.Sleep(1);
                    cki = Console.ReadKey(false);
                } while (cki.Key != ConsoleKey.Escape);
            }
            else
            {
                Console.WriteLine("login failed");
                Console.ReadKey();
            }

        }

        private static Result<JwtSecurityToken> Login(string userName, string password)
        {
            try
            {
                //get server address

                string url = $"{string.Format(BaseUriFormat, serverAddress)}{LoginSuffix}";
                string data = JsonConvert.SerializeObject(new { username = userName, password = password });
                HttpContent content = new StringContent(data, Encoding.UTF8, JsonMediaType);

                Task<HttpResponseMessage> responseTask = mClient.PostAsync(url, content);
                responseTask.Wait(TimeoutSpan);
                if (responseTask.Result == null)
                {
                    return Result.Fail<JwtSecurityToken>(
                        "Failed to get response from server on login");
                }

                HttpResponseMessage response = responseTask.Result;
                if (response.IsSuccessStatusCode == false)
                {
                    return Result.Fail<JwtSecurityToken>( "call failed");
                }

                Task<string> readResponseTask = response.Content.ReadAsStringAsync();
                readResponseTask.Wait(TimeoutSpan);
                if (readResponseTask.Result == null)
                {
                    return Result.Fail<JwtSecurityToken>( "Login Failed.");
                }

                mToken = new JwtSecurityToken(JsonConvert.DeserializeObject<LoginResponseDto>(readResponseTask.Result)
                    .Token);
                return Result.Ok(mToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Result.Ok(mToken);
            }
        }
    }class LoginResponseDto
    {
        public string Token { get; set; }
    }
}
