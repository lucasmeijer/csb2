using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using csb2.Caching;
using NiceIO;
using ServiceStack;
using Unity.IL2CPP;

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
        }

        public object Post(CompilationRequest request)
        {
            if (!RemoteCompilationService.Enabled)
                return new HttpError(HttpStatusCode.Forbidden);

            Console.WriteLine("Incoming compilation request: "+request.FileName);
            var s = new Stopwatch();
            s.Start();

            var tmp = NPath.SystemTemp.Combine("csb_remote").EnsureDirectoryExists().Combine(request.FileName);

            tmp.WriteAllBytes(request.Contents);
            
            var exeArgs = new Shell.ExecuteArgs() {Arguments = request.Arguments, WorkingDirectory = tmp.Parent.ToString(), Executable = request.Program};
            var result = Shell.Execute(exeArgs);

            var obj = tmp.ChangeExtension("obj");
            
            Console.WriteLine($"Finished compilation of {request.FileName}, duration: {s.ElapsedMilliseconds}ms");

            return new CompilationResponse()
            {
                ExitCode = result.ExitCode,
                Contents = obj.FileExists() ? obj.ReadAllBytes() : null,
                SourceIdentifier = Environment.UserName,
                Output = result.StdOut + result.StdErr
            };
        }
    }

    [Route("/compile")]
    public class CompilationRequest : IReturn<CompilationResponse>
    {
        public byte[] Contents { get; set; }
        public string FileName { get; set; }
        public string Program { get; set; }
        public string Arguments { get; set; }
    }

    public class CompilationResponse
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public byte[] Contents { get; set; }
        public string SourceIdentifier { get; set; }
    }

   


}


