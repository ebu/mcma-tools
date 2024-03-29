﻿using Mcma.Tools.Dotnet;

namespace Mcma.Tools.Modules.Dotnet;

public class DotnetProjectCreator : IDotnetProjectCreator
{
    public DotnetProjectCreator(IDotnetCli dotnetCli)
    {
        DotnetCli = dotnetCli ?? throw new ArgumentNullException(nameof(dotnetCli));
    }

    private IDotnetCli DotnetCli { get; }

    public async Task CreateProjectAsync(NewModuleParameters moduleParameters,
                                         string srcFolder,
                                         string template,
                                         bool addJobTypeArg = false)
    {
        var options = new List<(string name, string value)>
        {
            ("moduleName", moduleParameters.NameInPascalCase),
            ("mcmaNamespace", moduleParameters.NamespaceInPascalCase)
        };

        if (addJobTypeArg)
        {
            if (string.IsNullOrWhiteSpace(moduleParameters.JobType))
                throw new Exception("Must specify 'jobType' parameter");

            options.Add(("jobType", moduleParameters.JobType));
        }

        await DotnetCli.NewAsync(template, srcFolder, options.ToArray());
    }
}