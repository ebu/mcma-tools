﻿using System.Collections.Generic;

namespace Mcma.Management.Modules.Templates
{
    public interface INewProviderModuleTemplate
    {
        Provider Provider { get; }
        
        string GetModuleTf(IDictionary<string, string> args);

        string GetVariablesTf(IDictionary<string, string> args);

        string GetOutputsTf(IDictionary<string, string> args);
    }
}