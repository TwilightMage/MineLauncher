using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using System.Security.AccessControl;

namespace MineLauncher;

public static class JavaUtils
{
    private const int KEY_WOW64_64KEY = 0x0100;
    private const int KEY_WOW64_32KEY = 0x0200;

    // Copied and adapted from MMC launcher - https://github.com/MultiMC/Launcher/blob/2eb4cc9694f891ebc4139b9164cc733a856f0818/launcher/java/JavaUtils.cpp
    public static IEnumerable<string> FindJavaPaths()
    {
        var javaCandidates = new List<string>();

        // Oracle
        var JRE64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\JavaSoft\Java Runtime Environment", "JavaHome");
        var JDK64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\JavaSoft\Java Development Kit", "JavaHome");
        var JRE32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\JavaSoft\Java Runtime Environment", "JavaHome");
        var JDK32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\JavaSoft\Java Development Kit", "JavaHome");

        // Oracle for Java 9 and newer
        var NEWJRE64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\JavaSoft\JRE", "JavaHome");
        var NEWJDK64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\JavaSoft\JDK", "JavaHome");
        var NEWJRE32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\JavaSoft\JRE", "JavaHome");
        var NEWJDK32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\JavaSoft\JDK", "JavaHome");

        // AdoptOpenJDK
        var ADOPTOPENJRE32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\AdoptOpenJDK\JRE", "Path", @"\hotspot\MSI");
        var ADOPTOPENJRE64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\AdoptOpenJDK\JRE", "Path", @"\hotspot\MSI");
        var ADOPTOPENJDK32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\AdoptOpenJDK\JDK", "Path", @"\hotspot\MSI");
        var ADOPTOPENJDK64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\AdoptOpenJDK\JDK", "Path", @"\hotspot\MSI");

        // Eclipse Foundation
        var FOUNDATIONJDK32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\Eclipse Foundation\JDK", "Path", @"\hotspot\MSI");
        var FOUNDATIONJDK64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\Eclipse Foundation\JDK", "Path", @"\hotspot\MSI");

        // Eclipse Adoptium
        var ADOPTIUMJRE32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\Eclipse Adoptium\JRE", "Path", @"\hotspot\MSI");
        var ADOPTIUMJRE64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\Eclipse Adoptium\JRE", "Path", @"\hotspot\MSI");
        var ADOPTIUMJDK32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\Eclipse Adoptium\JDK", "Path", @"\hotspot\MSI");
        var ADOPTIUMJDK64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\Eclipse Adoptium\JDK", "Path", @"\hotspot\MSI");

        // Microsoft
        var MICROSOFTJDK64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\Microsoft\JDK", "Path", @"\hotspot\MSI");

        // Azul Zulu
        var ZULU64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\Azul Systems\Zulu", "InstallationPath");
        var ZULU32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\Azul Systems\Zulu", "InstallationPath");

        // BellSoft Liberica
        var LIBERICA64s = FindJavaFromRegistryKey(KEY_WOW64_64KEY, @"SOFTWARE\BellSoft\Liberica", "InstallationPath");
        var LIBERICA32s = FindJavaFromRegistryKey(KEY_WOW64_32KEY, @"SOFTWARE\BellSoft\Liberica", "InstallationPath");

        // List x64 before x86
        javaCandidates.AddRange(JRE64s);
        javaCandidates.AddRange(NEWJRE64s);
        javaCandidates.AddRange(ADOPTOPENJRE64s);
        javaCandidates.AddRange(ADOPTIUMJRE64s);
        javaCandidates.Add(VerifyPath(@"C:\Program Files\Java\jre8\bin\javaw.exe"));
        javaCandidates.Add(VerifyPath(@"C:\Program Files\Java\jre7\bin\javaw.exe"));
        javaCandidates.Add(VerifyPath(@"C:\Program Files\Java\jre6\bin\javaw.exe"));
        javaCandidates.AddRange(JDK64s);
        javaCandidates.AddRange(NEWJDK64s);
        javaCandidates.AddRange(ADOPTOPENJDK64s);
        javaCandidates.AddRange(FOUNDATIONJDK64s);
        javaCandidates.AddRange(ADOPTIUMJDK64s);
        javaCandidates.AddRange(MICROSOFTJDK64s);
        javaCandidates.AddRange(ZULU64s);
        javaCandidates.AddRange(LIBERICA64s);

        javaCandidates.AddRange(JRE32s);
        javaCandidates.AddRange(NEWJRE32s);
        javaCandidates.AddRange(ADOPTOPENJRE32s);
        javaCandidates.AddRange(ADOPTIUMJRE32s);
        javaCandidates.Add(VerifyPath(@"C:\Program Files (x86)\Java\jre8\bin\javaw.exe"));
        javaCandidates.Add(VerifyPath(@"C:\Program Files (x86)\Java\jre7\bin\javaw.exe"));
        javaCandidates.Add(VerifyPath(@"C:\Program Files (x86)\Java\jre6\bin\javaw.exe"));
        javaCandidates.AddRange(JDK32s);
        javaCandidates.AddRange(NEWJDK32s);
        javaCandidates.AddRange(ADOPTOPENJDK32s);
        javaCandidates.AddRange(FOUNDATIONJDK32s);
        javaCandidates.AddRange(ADOPTIUMJDK32s);
        javaCandidates.AddRange(ZULU32s);
        javaCandidates.AddRange(LIBERICA32s);

        return javaCandidates.Distinct().Where(candidate => !string.IsNullOrEmpty(candidate));
    }

    private static IEnumerable<string> FindJavaFromRegistryKey(int keyType, string keyName, string keyJavaDir, string subkeySuffix = "")
    {
        var view = keyType == KEY_WOW64_64KEY ? RegistryView.Registry64 : RegistryView.Registry32;

        using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
        {
            using (var jreKey = baseKey.OpenSubKey(keyName, RegistryRights.ReadKey))
            {
                if (jreKey == null) yield break;

                // Get all subkey names
                var subKeyNames = jreKey.GetSubKeyNames();

                foreach (var subKeyName in subKeyNames)
                {
                    var newKeyName = $"{keyName}\\{subKeyName}{subkeySuffix}";
                    using (var newKey = baseKey.OpenSubKey(newKeyName, RegistryRights.ReadKey))
                    {
                        if (newKey == null) continue;

                        var javaHome = newKey.GetValue(keyJavaDir) as string;
                        if (string.IsNullOrEmpty(javaHome)) continue;

                        var javaPath = Path.Combine(javaHome, "bin", "javaw.exe");
                        if (File.Exists(javaPath))
                        {
                            yield return javaPath;
                        }
                    }
                }
            }
        }
    }
    
    private static string VerifyPath(string path) => File.Exists(path) ? path : null;
}