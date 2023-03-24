namespace WebClientAppDemo
{
    public class Classes
    {
        public class LoginDetails
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
        public class TokenDetails 
        {
            public string Token { get; set;}
            public DateTime Expiration { get; set;}
            public string Username { get; set; } = "";
        }
        public class Client
        {
            public Guid Id { get; set;}
            public int Type { get; set; } 
            public string Alias { get; set; }
            public int Status { get; set; }
        }
    }
}
