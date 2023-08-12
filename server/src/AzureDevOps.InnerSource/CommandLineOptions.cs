using CommandLine;

namespace AzureDevOps.InnerSource;

internal class CommandLineOptions
{
    [Value(0)] public string Command { get; set; } = "";

    [Option('o', "output-folder", Default = "./")]
    public string OutputFolder { get; set; } = "./";
}