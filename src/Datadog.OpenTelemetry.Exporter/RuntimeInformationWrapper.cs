using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Datadog.OpenTelemetry.Exporter
{
    internal static class RuntimeInformationWrapper
    {
        private static readonly Assembly RootAssembly = typeof(object).Assembly;

        private static readonly Tuple<int, string>[] DotNetFrameworkVersionMapping =
        {
            // known min value for each framework version
            Tuple.Create(528040, "4.8"),
            Tuple.Create(461808, "4.7.2"),
            Tuple.Create(461308, "4.7.1"),
            Tuple.Create(460798, "4.7"),
            Tuple.Create(394802, "4.6.2"),
            Tuple.Create(394254, "4.6.1"),
            Tuple.Create(393295, "4.6"),
            Tuple.Create(379893, "4.5.2"),
            Tuple.Create(378675, "4.5.1"),
            Tuple.Create(378389, "4.5"),
        };

        public static string Name { get; private set; } = "unknown";

        public static string ProductVersion { get; private set; } = "unknown";

        public static string OsPlatform { get; private set; } = "unknown";

        public static string OsArchitecture { get; private set; } = "unknown";

        public static string ProcessArchitecture { get; private set; } = "unknown";

        static RuntimeInformationWrapper()
        {
            var assemblyName = RootAssembly.GetName();

            if (string.Equals(assemblyName.Name, "mscorlib", StringComparison.OrdinalIgnoreCase))
            {
                // .NET Framework
                Name = ".NET Framework";
                ProductVersion = GetNetFrameworkVersion() ?? "unknown";
                OsPlatform = "Windows";
                OsArchitecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                ProcessArchitecture = Environment.Is64BitProcess ? "x64" : "x86";
            }

            // .NET Core
            InitializeFromRuntimeInformation();
        }

        private static void InitializeFromRuntimeInformation()
        {
            try
            {
                // RuntimeInformation.FrameworkDescription returns a string like ".NET Framework 4.7.2" or ".NET Core 2.1",
                // we want to return everything before the last space
                string frameworkDescription = RuntimeInformation.FrameworkDescription;
                int index = frameworkDescription.LastIndexOf(' ');
                Name = frameworkDescription.Substring(0, index).Trim();
            }
            catch (Exception)
            {
                // Log.ErrorException("Error getting framework name from RuntimeInformation", e);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OsPlatform = "Windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                OsPlatform = "Linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                OsPlatform = "MacOS";
            }

            ProductVersion = GetNetCoreVersion();
            OsArchitecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        }

        private static string? GetNetFrameworkVersion()
        {
            string? productVersion = null;

            try
            {
                object? registryValue;

                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                using (var subKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
                {
                    registryValue = subKey?.GetValue("Release");
                }

                if (registryValue is int release)
                {
                    // find the known version on the list with the largest release number
                    // that is lower than or equal to the release number in the Windows Registry
                    productVersion = DotNetFrameworkVersionMapping.FirstOrDefault(t => release >= t.Item1)?.Item2;
                }
            }
            catch (Exception e)
            {
                // Log.ErrorException("Error getting .NET Framework version from Windows Registry", e);
            }

            if (productVersion == null)
            {
                // if we fail to extract version from assembly path,
                // fall back to the [AssemblyInformationalVersion] or [AssemblyFileVersion]
                productVersion = GetVersionFromAssemblyAttributes();
            }

            return productVersion;
        }

        private static string GetNetCoreVersion()
        {
            string? productVersion = null;

            if (Environment.Version.Major is 3 or >= 5)
            {
                // Environment.Version returns "4.x" in .NET Core 2.x,
                // but it is correct since .NET Core 3.0.0
                productVersion = Environment.Version.ToString();
            }

            if (productVersion == null)
            {
                try
                {
                    // try to get product version from assembly path
                    Match match = Regex.Match(
                        RootAssembly.CodeBase,
                        @"/[^/]*microsoft\.netcore\.app/(\d+\.\d+\.\d+[^/]*)/",
                        RegexOptions.IgnoreCase);

                    if (match.Success && match.Groups.Count > 0 && match.Groups[1].Success)
                    {
                        productVersion = match.Groups[1].Value;
                    }
                }
                catch (Exception e)
                {
                    // Log.ErrorException("Error getting .NET Core version from assembly path", e);
                }
            }

            if (productVersion == null)
            {
                // if we fail to extract version from assembly path,
                // fall back to the [AssemblyInformationalVersion] or [AssemblyFileVersion]
                productVersion = GetVersionFromAssemblyAttributes();
            }

            if (productVersion == null)
            {
                // at this point, everything else has failed (this is probably the same as [AssemblyFileVersion] above)
                productVersion = Environment.Version.ToString();
            }

            return productVersion;
        }

        private static string? GetVersionFromAssemblyAttributes()
        {
            string? productVersion = null;

            try
            {
                // if we fail to extract version from assembly path, fall back to the [AssemblyInformationalVersion],
                var informationalVersionAttribute = (AssemblyInformationalVersionAttribute?)RootAssembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

                // split remove the commit hash from pre-release versions
                productVersion = informationalVersionAttribute?.InformationalVersion?.Split('+')[0];
            }
            catch
            {
                // Log.ErrorException("Error getting framework version from [AssemblyInformationalVersion]", e);
            }

            if (productVersion == null)
            {
                try
                {
                    // and if that fails, try [AssemblyFileVersion]
                    var fileVersionAttribute = (AssemblyFileVersionAttribute?)RootAssembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute));
                    productVersion = fileVersionAttribute?.Version;
                }
                catch
                {
                    // Log.ErrorException("Error getting framework version from [AssemblyFileVersion]", e);
                }
            }

            return productVersion;
        }
    }
}
