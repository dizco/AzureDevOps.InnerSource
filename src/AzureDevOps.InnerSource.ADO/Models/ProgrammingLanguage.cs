namespace AzureDevOps.InnerSource.ADO.Models;

public class ProgrammingLanguage
{
    public ProgrammingLanguage(string name)
    {
	    if (string.IsNullOrWhiteSpace(name))
	    {
		    throw new ArgumentNullException(nameof(name));
	    }

        Name = name;
    }

    public string Name { get; }

    internal string GetBadgeUrl()
    {
		// See: https://dev.to/envoy_/150-badges-for-github-pnk#skills
		// See: https://github.com/Ileriayo/markdown-badges
		return Name switch
        {
            //"C#" => "https://img.shields.io/badge/-512BD4?logo=.net",
            "C#" => "https://img.shields.io/badge/c%23-239120.svg?logo=c-sharp&logoColor=white",
            "TypeScript" => "https://img.shields.io/badge/TypeScript-007ACC?logo=typescript&logoColor=white",
            "JavaScript" => "https://img.shields.io/badge/javascript-323330.svg?logo=javascript&logoColor=%23F7DF1E",
            "C++" => "https://img.shields.io/badge/c++-%2300599C.svg?logo=c%2B%2B&logoColor=white",
            "Python" => "https://img.shields.io/badge/Python-14354C?logo=python&logoColor=white",
            "Swift" => "https://img.shields.io/badge/Swift-FA7343?logo=swift&logoColor=white",
            "Go" => "https://img.shields.io/badge/Go-00ADD8?logo=go&logoColor=white",
            "PowerShell" => "https://img.shields.io/badge/Powershell-2CA5E0?logo=powershell&logoColor=white",
            "Kotlin" => "https://img.shields.io/badge/Kotlin-0095D5?logo=kotlin&logoColor=white",
            "CSS" => "https://img.shields.io/badge/CSS3-1572B6?logo=css3&logoColor=white",
            "HCL" => "https://img.shields.io/badge/terraform-%235835CC.svg?logo=terraform&logoColor=white",
            _ => $"https://img.shields.io/badge/{Name}-lightgrey"
		};
    }
}