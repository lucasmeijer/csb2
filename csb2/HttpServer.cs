using System;
using Funq;
using ServiceStack;

namespace csb2.Caching
{
    public class HttpServer
    {
        //Define the Web Services AppHost
        public class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base("HttpListener Self-Host",typeof(CachingService).Assembly)
            { }

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig { DebugMode = true });
            }
        }


        public void Start()
        { 
            var appHost = new AppHost()
                .Init()
                .Start(Url);

            Console.WriteLine("Starting HttpServer at " + Url);
        }
        
        public static string Url { get; set; } = "http://+:8080/";
    }
}