using System;
using System.IO;
using System.Linq;


public static class Program
{
    public enum Command { None, New, Help }

    public static void Main(string[] args)
    {
        try
        {
            ParseCommands(args);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static void ParseCommands(string[] cmds)
    {
        if(cmds.Length == 0) PrintHelp();

        if(!Enum.TryParse<Command>(cmds[0], true, out Command command))
            throw new Exception($"There is no command with the name '{cmds[0]}'");

        cmds = cmds.Skip(1).ToArray();

        switch(command)
        {
            case Command.New:
            {
                string templateName = cmds.Length == 0 || cmds[0].StartsWith('-') ? "default" : cmds[0];
                CreateProject(templateName, 
                    FindParameter(cmds, "-n", "--name"),
                    FindParameter(cmds, "-s", "--std"),
                    FindParameter(cmds, "-c", "--cmake-min")
                    );
                break;
            }
            case Command.Help:
            {
                PrintHelp();
                break;
            }
            case Command.None:
            {
                if(cmds.Length == 0) { PrintHelp(Command.None); return; }
                break;
            }
        }
    }

    private static void CreateProject(string templateName, string? projectName = null, string? std = null, string? cmakeMinVersion = null)
    {
        const string defaultProjectName = "NewProject";
        const string defaultCmakeMinVersion = "3.22";
        const string defaultStd = "17";

        string currentDirPath = "./";
        if(projectName != null) 
        {
            Directory.CreateDirectory(projectName);
            currentDirPath += projectName;
        }
        else
        {
            projectName = defaultProjectName;
        }

        string buildShSrc;
        string cmakeSrc;
        switch(templateName)
        {
            case "default":
            {
                buildShSrc = BashSrc.DefaultExeBuildSh
                    .Replace("{{project_name}}", projectName);
                cmakeSrc = CMakeSrc.DefaultExeCMakeLists
                    .Replace("{{project_name}}", projectName)
                    .Replace("{{cmake_min_version}}", cmakeMinVersion ?? defaultCmakeMinVersion)
                    .Replace("{{cpp_standard}}", std ?? defaultStd);
                break;
            }
            case "lib":
            case "library":
            {
                buildShSrc = BashSrc.DefaultLibBuildSh
                    .Replace("{{project_name}}", projectName);
                cmakeSrc = CMakeSrc.DefaultLibCMakeLists
                    .Replace("{{project_name}}", projectName)
                    .Replace("{{cmake_min_version}}", cmakeMinVersion ?? defaultCmakeMinVersion)
                    .Replace("{{cpp_standard}}", std ?? defaultStd);
                break;
            }
            default:
            {
                throw new Exception($"There is no template with the given name: '{templateName}'");
            }

        }

        // create build.sh
        using StreamWriter buildShWriter = File.CreateText(Path.Combine(currentDirPath, "build.sh"));
        buildShWriter.Write(buildShSrc);
        buildShWriter.Close();

        // create CMakeLists.txt
        using StreamWriter cmakeListsWriter = File.CreateText(Path.Combine(currentDirPath, "CMakeLists.txt"));
        cmakeListsWriter.Write(cmakeSrc);
        buildShWriter.Close();

        // create src/main.cpp
        string srcDirPath = Directory.CreateDirectory(Path.Combine(currentDirPath, "src")).FullName;
        using StreamWriter mainWriter = File.CreateText(Path.Combine(srcDirPath, "main.cpp"));
        mainWriter.Write(CodeSrc.DefaultMainSrc);
        mainWriter.Close();

        // create external/lib/win  and external/lib/linux
        Directory.CreateDirectory(Path.Combine(currentDirPath, "external/lib/win"));
        Directory.CreateDirectory(Path.Combine(currentDirPath, "external/lib/linux"));

        Console.WriteLine($"Project '{projectName}' was successfully created! (template: '{templateName}')");
    }

    private static void PrintHelp(Command command = Command.None)
    {
        Console.WriteLine("Commands:");
        foreach(string cmd in Enum.GetNames(typeof(Command)))
        {
            Console.WriteLine($"    {cmd},");
        }

        Console.WriteLine("Flags:");
        Console.WriteLine($"    '-n', '--name' : Name of the project,");
        Console.WriteLine($"    '-s', '--std' : Required version of C++ standard,");
        Console.WriteLine($"    '-c', '--cmake-min' : Minimal required version of CMake,");
    }

    private static string? FindParameter(string[] cmds, params string[] flag)
    {
        string? parameter = null;
        for(int i = 0; i < flag.Length; ++i)
        {
            int idx = Array.IndexOf(cmds, flag[i]);
            if(idx > -1 && cmds.Length > idx + 1)
                parameter = cmds[idx + 1];
        }
        return parameter;
    }
}