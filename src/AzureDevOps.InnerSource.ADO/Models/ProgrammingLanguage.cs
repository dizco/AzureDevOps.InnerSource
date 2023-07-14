namespace AzureDevOps.InnerSource.ADO.Models;

public class ProgrammingLanguage
{
    public ProgrammingLanguage(string name)
    {
        Name = name;
    }

    public string Name { get; }

    internal string GetBadgeUrl()
    {
        return Name switch
        {
            "C#" => "https://img.shields.io/badge/-512BD4?logo=.net",
            "TypeScript" => "https://img.shields.io/badge/TypeScript-007ACC?logo=typescript&logoColor=white",
            "JavaScript" => "https://img.shields.io/badge/javascript-%23323330.svg?logo=javascript&logoColor=%23F7DF1E",
            "C++" => "https://img.shields.io/badge/c++-%2300599C.svg?logo=c%2B%2B&logoColor=white",
            _ => ""
        };
    }
}