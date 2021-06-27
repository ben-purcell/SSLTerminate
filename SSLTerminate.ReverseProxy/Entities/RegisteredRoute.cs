namespace SSLTerminate.ReverseProxy.Entities
{
    public class RegisteredRoute
    {
        public int Id { get; set; }
        public string Host { get; set; }
        public string Redirect { get; set; }
    }
}