using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using csb2.Caching;
using NiceIO;
using ServiceStack;

namespace csb2
{    
    public class RemoteCompilationService
    {
        public static bool Enabled;

     
    }

    public class ServiceStackService : Service
    {
        public static NPath _cachePath;

        public ServiceStackService()
        {
            Console.WriteLine("Hallo");
        }

        public object Any(CompilationRequest request)
        {
            if (!RemoteCompilationService.Enabled)
                return new HttpError(HttpStatusCode.Forbidden);

            return new CompilationResponse();
        }
    }

    [Route("/compile")]
    public class CompilationRequest : IReturn<CompilationResponse>
    {
        public string Contents { get; set; }
        public string FileName { get; set; }
        public string Program { get; set; }
        public string Arguments { get; set; }
    }

    class CompilationResponse
    {
        public int ExitCode;
        public string Output;
    }

   


}


