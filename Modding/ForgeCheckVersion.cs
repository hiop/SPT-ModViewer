using SPTModViewer.Config;

namespace SPTarkov.Server.Modding;

using System;
using System.Linq;

public class VersionInfo
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
}

public class ForgeCheckVersion
{

    public static SPTForgeModVersion? FindLastVersion(String currentSptVersion, List<SPTForgeModVersion> forgeVersions )
    {
        SPTForgeModVersion forgeModVersion = null;
        foreach (var forgeVersion in forgeVersions.AsEnumerable().Reverse())
        {
            if (CheckVersion(forgeVersion.SptVersion, currentSptVersion))
            {
                forgeModVersion = forgeVersion;
                break;
            }
        }
        
        return forgeModVersion;
    }

    /// <summary>
    /// Compares two versions of SPT
    /// </summary>
    /// <param name="condition">version with condition (>=4.0.1, ~4.0.0, ...)</param>
    /// <param name="currentVersion">current spt version (only digit and dots)</param>
    /// <returns>True if current version satisfies all conditions</returns>
    public static bool CheckVersion(string condition, string currentVersion)
    {
        // Parsing
        VersionInfo ParseVersion(string versionStr)
        {
            var parts = versionStr.Split('.')
                .Select(part => int.TryParse(part, out var result) ? result : 0)
                .ToArray();

            return new VersionInfo
            {
                Major = parts.Length > 0 ? parts[0] : 0,
                Minor = parts.Length > 1 ? parts[1] : 0,
                Patch = parts.Length > 2 ? parts[2] : 0
            };
        }

        var current = ParseVersion(currentVersion);

        // Version comparison function
        int CompareVersions(VersionInfo v1, VersionInfo v2)
        {
            if (v1.Major != v2.Major) return v1.Major - v2.Major;
            if (v1.Minor != v2.Minor) return v1.Minor - v2.Minor;
            return v1.Patch - v2.Patch;
        }

        // Handling the ~ condition
        bool ProcessTilde(string cond)
        {
            var baseVersionStr = cond.Substring(1);
            var baseVersion = ParseVersion(baseVersionStr);

            // ~4 means >=4.0.0 <5.0.0
            // ~4.0.1 means >=4.0.1 <4.1.0
            var partsCount = baseVersionStr.Split('.').Length;

            if (partsCount == 1)
            {
                // ~4
                return current.Major == baseVersion.Major;
            }
            else
            {
                // ~4.0.1
                return current.Major == baseVersion.Major &&
                       current.Minor == baseVersion.Minor &&
                       current.Patch >= baseVersion.Patch;
            }
        }

        // as is
        bool ProcessExact(string cond)
        {
            var targetVersion = ParseVersion(cond);
            return CompareVersions(current, targetVersion) == 0;
        }

        // Operators
        bool ProcessComparison(string cond)
        {
            var operators = new[] { ">=", "<=", ">", "<", "=" };
            string usedOperator = "";
            string versionStr = "";

            foreach (var op in operators)
            {
                if (cond.StartsWith(op))
                {
                    usedOperator = op;
                    versionStr = cond.Substring(op.Length).Trim();
                    break;
                }
            }

            if (string.IsNullOrEmpty(usedOperator))
                return false;

            var targetVersion = ParseVersion(versionStr);
            var comparison = CompareVersions(current, targetVersion);

            return usedOperator switch
            {
                ">=" => comparison >= 0,
                "<=" => comparison <= 0,
                ">" => comparison > 0,
                "<" => comparison < 0,
                "=" => comparison == 0,
                _ => false
            };
        }

        // Several conditions
        var conditions = condition.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Check all conditions
        foreach (var cond in conditions)
        {
            bool result = false;

            if (cond.StartsWith("~"))
            {
                result = ProcessTilde(cond);
            }
            else if (cond.Contains('>') || cond.Contains('<') || cond.Contains('='))
            {
                result = ProcessComparison(cond);
            }
            else
            {
                result = ProcessExact(cond);
            }

            if (!result)
            {
                return false;
            }
        }

        return true;
    }
}