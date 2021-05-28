﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mcma.Tools.Dotnet;
using Mcma.Tools.Modules.Templates;

namespace Mcma.Tools.Modules.Dotnet.GoogleCloud
{
    public abstract class GoogleCloudFunctionModuleTemplate : INewProviderModuleTemplate
    {
        protected GoogleCloudFunctionModuleTemplate(IDotnetCli dotnetCli)
        {
            DotnetCli = dotnetCli ?? throw new ArgumentNullException(nameof(dotnetCli));
        }
        
        protected IDotnetCli DotnetCli { get; }

        public Provider Provider => Provider.AWS;

        public virtual string GetModuleTf(IDictionary<string, string> args)
        {
            return $@"terraform {{
  required_providers {{
    google = {{
      source  = ""hashicorp/google""
      version = ""~> 3.69.0""
    }} 
  }}
}}

provider ""google"" {{
  {string.Join(Environment.NewLine + "  ", args.Select(kvp => $"{kvp.Key} = \"{kvp.Value}\""))}
}}";
        }

        public virtual string GetVariablesTf(IDictionary<string, string> args) => string.Empty;

        public virtual string GetOutputsTf(IDictionary<string, string> args) => string.Empty;


        protected virtual async Task CreateProjectAsync(NewModuleParameters moduleParameters,
                                                        NewProviderModuleParameters providerParameters,
                                                        string srcFolder,
                                                        string template,
                                                        bool addJobTypeArg = false)
        {
            var dotnetNewArgs = new List<string>
            {
                template,
                "-o",
                srcFolder,
                "--moduleName",
                moduleParameters.NameInPascalCase,
                "--mcmaNamespace",
                moduleParameters.NamespaceInPascalCase
            };

            if (addJobTypeArg)
                dotnetNewArgs.AddRange(new[]
                {
                    "--jobType",
                    moduleParameters.JobType
                });

            Console.WriteLine("Running dotnet new " + string.Join(" ", dotnetNewArgs));

            await DotnetCli.RunCmdWithOutputAsync("new", dotnetNewArgs.ToArray());
        }
    }
}