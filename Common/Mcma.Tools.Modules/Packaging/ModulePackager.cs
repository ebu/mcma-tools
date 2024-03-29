﻿using System.IO.Compression;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Newtonsoft.Json.Linq;

namespace Mcma.Tools.Modules.Packaging;

public class ModulePackager : IModulePackager
{
    private static readonly string[] TextFileExts = [".tf", ".json", ".yaml", ".txt", ".xml", ".config", ".html", ".md", ".ini"];

    private static void CopyFile(ModuleProviderContext moduleProviderContext, JToken additionalFile)
    {
        string src;
        string dest;
        bool flatten;
        
        switch (additionalFile.Type)
        {
            case JTokenType.Object:
                src =
                    additionalFile[nameof(src)]?.Value<string>()
                      ?? throw new Exception("Invalid entry in 'files' property of module json. Object must specify a value for 'src'");
                dest = additionalFile[nameof(dest)]?.Value<string>() ?? string.Empty;
                flatten = additionalFile[nameof(flatten)]?.Value<bool>() ?? false;
                break;
            
            case JTokenType.String:
                src =
                    additionalFile.Value<string>()
                      ?? throw new Exception("Invalid entry in 'files' property of module json. String value returned null");
                dest = string.Empty;
                flatten = false;
                break;
            
            case JTokenType.None:
            case JTokenType.Array:
            case JTokenType.Constructor:
            case JTokenType.Property:
            case JTokenType.Comment:
            case JTokenType.Integer:
            case JTokenType.Float:
            case JTokenType.Boolean:
            case JTokenType.Null:
            case JTokenType.Undefined:
            case JTokenType.Date:
            case JTokenType.Raw:
            case JTokenType.Bytes:
            case JTokenType.Guid:
            case JTokenType.Uri:
            case JTokenType.TimeSpan:
            default:
                throw new Exception("Invalid value in 'additionalFiles'. Value must be an object or a string.");
        }

        var matcher = new Matcher().AddInclude(src).AddExclude(".publish");

        var srcDir = new DirectoryInfo(moduleProviderContext.ProviderFolder);
        var srcDirWrapper = new DirectoryInfoWrapper(srcDir);
        var matches = matcher.Execute(srcDirWrapper);
            
        var destRootDir = Path.Combine(moduleProviderContext.OutputStagingFolder, dest);
        Directory.CreateDirectory(destRootDir);
            
        Console.WriteLine($"Copying files matching pattern '{src}' in {moduleProviderContext.ProviderFolder} to {destRootDir}...");

        foreach (var match in matches.Files)
        {
            var srcPath = Path.Combine(moduleProviderContext.ProviderFolder, match.Path);
            var destPath = Path.Combine(destRootDir, flatten ? Path.GetFileName(match.Path) : match.Path);
                
            var destDir = Path.GetDirectoryName(destPath) ?? throw new Exception($"Directory name for {destPath} returned null");
            Directory.CreateDirectory(destDir);
            
            if (TextFileExts.Any(x => x.Equals(Path.GetExtension(srcPath), StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"Replacing tokens in content in {srcPath}");
                var content = File.ReadAllText(srcPath);

                Console.WriteLine($"Writing content to {destPath}");
                File.WriteAllText(destPath, moduleProviderContext.ReplaceTokens(content));
            }
            else
            {
                Console.WriteLine($"Copying {srcPath} to {destPath}");
                File.Copy(srcPath, destPath, true);
            }
        }
    }

    public async Task PackageAsync(ModuleProviderContext moduleProviderContext, IModuleBuildSystem buildSystem)
    {
        Console.WriteLine($"Packaging module at {moduleProviderContext.ProviderFolder}...");
        try
        {
            if (Directory.Exists(moduleProviderContext.OutputStagingFunctionFolder))
                Directory.Delete(moduleProviderContext.OutputStagingFunctionFolder, true);
            
            Directory.CreateDirectory(moduleProviderContext.OutputStagingFunctionFolder);
            
            foreach (var function in moduleProviderContext.ModulePackage.Functions)
            {
                Console.WriteLine($"Packaging function '{function.Name}' of type '{function.Type}'");
                    
                await buildSystem.PackageFunctionAsync(moduleProviderContext, function);
                    
                Console.WriteLine($"Function '{function.Name}' packaged successfully.");
            }
                
            var files = moduleProviderContext.ModulePackage.Files;
            if (files is not null)
                foreach (var file in files)
                    CopyFile(moduleProviderContext, file);

            await File.WriteAllTextAsync(
                Path.Combine(moduleProviderContext.OutputStagingFolder, "module.json"),
                moduleProviderContext.GetProviderSpecificModule().ToJson());
                
            if (File.Exists(moduleProviderContext.OutputZipFile))
                File.Delete(moduleProviderContext.OutputZipFile);
        
            Console.WriteLine($"Zipping output to {moduleProviderContext.OutputZipFile}...");
            ZipFile.CreateFromDirectory(moduleProviderContext.OutputStagingFolder, moduleProviderContext.OutputZipFile);
            Console.WriteLine($"Successfully created package zip at {moduleProviderContext.OutputZipFile}");
        }
        catch
        {
            try
            {
                Directory.Delete(moduleProviderContext.OutputStagingFolder, true);
            }
            catch
            {
                // clean up isn't critical
            }
            throw;
        }
    }
}