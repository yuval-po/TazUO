using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using ClassicUO.Game;
using ClassicUO.Utility.Logging;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ClassicUO.LegionScripting;

public class ScriptFile
{
    public string Path;
    public string FileName;
    public string FullPath;
    public string Group = string.Empty;
    public string SubGroup = string.Empty;
    public string[] FileContents;
    public string FileContentsJoined;
    public Thread PythonThread;
    public ScriptEngine PythonEngine;
    public ScriptScope PythonScope;
    public API ScopedApi;
    public ScriptType Type;
    public Script<object> CSharpCompiledScript;
    public ScriptGlobals CSharpGlobals;

    public bool IsPlaying => PythonThread != null;

    public enum ScriptType
    {
        Python,
        CSharp
    }

    private World World;

    public ScriptFile(World world, string path, string fileName)
    {
        World = world;
        Path = path;

        string cleanPath = path.Replace(System.IO.Path.DirectorySeparatorChar, '/');
        string cleanBasePath = LegionScripting.ScriptPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
        cleanPath = cleanPath.Substring(cleanPath.IndexOf(cleanBasePath, StringComparison.Ordinal) + cleanBasePath.Length);

        if (cleanPath.Length > 0)
        {
            string[] paths = cleanPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
            if (paths.Length > 0)
                Group = paths[0];
            if (paths.Length > 1)
                SubGroup = paths[1];
        }

        FileName = fileName;
        FullPath = System.IO.Path.Combine(Path, FileName);
        FileContents = ReadFromFile();

        // Determine script type based on extension
        if (fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            Type = ScriptType.CSharp;
        else
            Type = ScriptType.Python;
    }

    public void OverrideFileContents(string contents)
    {
        string temp = System.IO.Path.GetTempFileName();

        try
        {
            File.WriteAllText(temp, contents);
            File.Move(temp, FullPath, true);

            GameActions.Print(World, $"Saved {FileName}.");
        }
        catch (Exception ex)
        {
            GameActions.Print(World, ex.ToString());
        }
    }

    public string[] ReadFromFile()
    {
        try
        {
            string[] c = File.ReadAllLines(FullPath, Encoding.UTF8);
            string newContents = string.Join("\n", c);

            // Check if contents changed for C# scripts and invalidate cache
            if (Type == ScriptType.CSharp && FileContentsJoined != newContents)
            {
                CSharpCompiledScript = null;
            }

            FileContentsJoined = newContents;

            string pattern = @"^\s*(?:from\s+[\w.]+\s+import\s+API|import\s+API)\s*$";
            FileContentsJoined = System.Text.RegularExpressions.Regex.Replace(FileContentsJoined, pattern, string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);

            return c;
        }
        catch (Exception e)
        {
            Log.Error($"Error reading script file: {e}");
            return [];
        }
    }

    public bool FileExists() => File.Exists(FullPath);

    public void SetupPythonEngine()
    {
        if (PythonEngine != null && !LegionScripting.LScriptSettings.DisableModuleCache)
            return;

        PythonEngine = Python.CreateEngine();

        string dir = System.IO.Path.GetDirectoryName(FullPath);
        ICollection<string> paths = PythonEngine.GetSearchPaths();
        paths.Add(System.IO.Path.Combine(CUOEnviroment.ExecutablePath, "iplib"));
        paths.Add(System.IO.Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts"));

        paths.Add(!string.IsNullOrWhiteSpace(dir) ? dir : Environment.CurrentDirectory);

        PythonEngine.SetSearchPaths(paths);
    }

    public void SetupPythonScope()
    {
        PythonScope = PythonEngine.CreateScope();
        var api = new API(PythonEngine);
        ScopedApi = api;
        PythonEngine.GetBuiltinModule().SetVariable("API", api);
    }

    public void PythonScriptStopped()
    {
        ScopedApi?.CloseGumps();
        ScopedApi?.Dispose();

        PythonScope = null;
        ScopedApi = null;
        if (LegionScripting.LScriptSettings.DisableModuleCache)
            PythonEngine = null;
    }

    public void SetupCSharpScript()
    {
        // Reuse cached compilation if available
        if (CSharpCompiledScript != null && !LegionScripting.LScriptSettings.DisableModuleCache)
            return;

        // Configure script options with assemblies and imports
        var options = ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,                              // System
                typeof(System.Linq.Enumerable).Assembly,             // System.Linq
                typeof(System.Collections.Generic.List<>).Assembly,  // System.Collections.Generic
                typeof(API).Assembly,                                 // ClassicUO.Client
                typeof(Microsoft.Xna.Framework.Vector3).Assembly     // Microsoft.Xna.Framework
            )
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Threading.Tasks",
                "ClassicUO.LegionScripting",
                "ClassicUO.LegionScripting.PyClasses"
            );

        // Compile the script
        CSharpCompiledScript = CSharpScript.Create<object>(
            FileContentsJoined,
            options,
            typeof(ScriptGlobals)
        );

        // Pre-compile to catch compilation errors early
        CSharpCompiledScript.Compile();
    }

    public void SetupCSharpGlobals()
    {
        var api = new API(null); // C# scripts pass null engine
        ScopedApi = api;
        CSharpGlobals = new ScriptGlobals { API = api };
    }

    public void CSharpScriptStopped()
    {
        ScopedApi?.CloseGumps();
        ScopedApi?.Dispose();

        CSharpGlobals = null;
        ScopedApi = null;

        // Clear compilation cache if module caching disabled
        if (LegionScripting.LScriptSettings.DisableModuleCache)
            CSharpCompiledScript = null;
    }
}
