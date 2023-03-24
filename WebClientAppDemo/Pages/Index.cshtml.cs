using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text;
using static WebClientAppDemo.Classes;

namespace WebClientAppDemo.Pages
{
    public class IndexModel : PageModel
    {
        public bool IsLoginRequired = true;
        public IEnumerable<Client>? Clients { get; set; } = null;
        public string ProcessInfo { get; set; } = "";

        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            IsLoginRequired = CheckLoginRequired(Request);
        }
        public void OnPostLogin()
        {
            ClearCookies(Response);
            var Token = GetTokenFromApi(Request);
            if (Token != null)
            {
                SaveCookies(Response, Token);
            }
        }
        public void OnPostLogout()
        {
            ClearCookies(Response);
        }
        public void OnPostShowClients()
        {
            var Token = GetTokenFromCookie(Request);
            if (Token != null)
            {
                Clients = FindClientsApi(Token);
            }
            IsLoginRequired = CheckLoginRequired(Request);
        }

        private bool CheckLoginRequired(HttpRequest request)
        {
            return GetTokenFromCookie(request) == null;
        }
        private void ClearCookies(HttpResponse response)
        {
            response.Cookies.Delete("bearer");
            response.Cookies.Delete("username");
            IsLoginRequired = true;
        }
        private string? GetTokenFromCookie(HttpRequest request)
        {
            if (Request.Cookies.ContainsKey("bearer"))
            {
                string? token = Request.Cookies["bearer"];
                if ((token != null) && (token != ""))
                {
                    // TODO: parse token and check Expiraion
                    return token;
                }
            }
            return null;
        }
        private TokenDetails? GetTokenFromApi(HttpRequest pageRequest)
        {
            var usr = pageRequest.Form["User"].ToString();
            var pwd = pageRequest.Form["Password"].ToString();
            if ((usr == "") || (pwd == ""))
            {
                ProcessInfo = "Please specify username and password.";
                return null;
            }
            HttpClient api = new HttpClient();
            api.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
                { CharSet = Encoding.UTF8.WebName });
            api.BaseAddress = new Uri("https://localhost:7088");
            HttpResponseMessage? response = null;
            try
            {
                response = api.PostAsJsonAsync<LoginDetails>("api/Users/GetToken", new LoginDetails()
                {
                    Username = usr,
                    Password = pwd
                }).Result;
            }
            catch (Exception ex)
            {
                string connRefusedText = "No connection";
                if (ex.Message.IndexOf(connRefusedText) >= 0)
                {
                    ProcessInfo = "Error: " + connRefusedText;
                    return null;
                }
                if (response != null)
                {
                    ProcessInfo = response.ReasonPhrase;
                    return null;
                }
                ProcessInfo = "Error: " + "unknown error";
                return null;
            };
            if (!response.IsSuccessStatusCode)
            {
                ProcessInfo = "Error: " + "Cannot authorize this user.";
                return null;
            }
            var Token = response.Content.ReadFromJsonAsync<TokenDetails>().Result;
            if (Token == null)
            {
                ProcessInfo = "Error:" + "Cannot obtain user token. Unexpected API problem.";
                return null;
            }
            Token.Username = usr;
            return Token;
        }
        private IEnumerable<Client> FindClientsApi(string token)
        {
            HttpClient api = new HttpClient();
            api.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
                { CharSet = Encoding.UTF8.WebName });
            api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            api.BaseAddress = new Uri("https://localhost:7088");
            var response = api.GetAsync("api/Clients/FindClients").Result;
            if (response == null)
            {
                throw new Exception("Response was not received");
            }
            if (!response.IsSuccessStatusCode)
            {
                ProcessInfo = response.ReasonPhrase;
                return null;
                //throw new Exception(response.ReasonPhrase);
            }
            var Clients = response.Content.ReadFromJsonAsync<IEnumerable<Client>>().Result;
            if (Clients == null)
            {
                throw new Exception("Unexpected error in api/Clients/FindClients method");
            }
            return Clients;
        }
        private void SaveCookies(HttpResponse response, TokenDetails token)
        {
            response.Cookies.Append("bearer", token.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                IsEssential = true,
                Expires = token.Expiration
            });
            response.Cookies.Append("username", token.Username, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                IsEssential = true,
                Expires = token.Expiration
            });
            IsLoginRequired = false;
        }

        //private TokenDetails? GetTokenFromFile()
        //{
        //    string path = Directory.GetCurrentDirectory() + "\\token.json";
        //    if (!System.IO.File.Exists(path))
        //        return null;

        //    var json = System.IO.File.ReadAllText(path);
        //    if (json == null)
        //        return null;

        //    var token = JsonSerializer.Deserialize<TokenDetails>(json, new JsonSerializerOptions()
        //    {
        //        PropertyNameCaseInsensitive = true,
        //        AllowTrailingCommas = false,
        //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        //    });
        //    if ((token == null) || (token.Token == null))
        //        return null;
        //    return token;
        //}

        //private void SaveToFile(TokenDetails token)
        //{
        //    string path = Directory.GetCurrentDirectory() + "\\token.json";
        //    string json = JsonSerializer.Serialize(token);
        //    System.IO.File.WriteAllText(path, json);
        //}
    }
}