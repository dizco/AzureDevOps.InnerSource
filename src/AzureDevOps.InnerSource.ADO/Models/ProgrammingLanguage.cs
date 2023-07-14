namespace AzureDevOps.InnerSource.ADO.Models;

public class ProgrammingLanguage
{
    public ProgrammingLanguage(string name)
    {
        Name = name;
    }

    public string Name { get; }

    internal string GetHtmlBadge()
    {
        return Name switch
        {
            "C#" => "<img src=\"https://img.shields.io/badge/-512BD4?logo=.net\" alt=\".NET\">",
            "TypeScript" => "<img src=\"https://img.shields.io/badge/TypeScript-007ACC?logo=typescript&logoColor=white\" alt=\"TypeScript\">",
            "JavaScript" => "<img src=\"https://img.shields.io/badge/javascript-%23323330.svg?logo=javascript&logoColor=%23F7DF1E\" alt=\"JavaScript\">",
            "C++" => "<img src=\"https://img.shields.io/badge/c++-%2300599C.svg?logo=c%2B%2B&logoColor=white\" alt=\"C++\">",
            _ => ""
        };
    }
}