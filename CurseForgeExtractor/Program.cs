using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using CurseForge.APIClient;
using CurseForge.APIClient.Models.Mods;
using Newtonsoft.Json.Linq;

namespace CurseForgeExtractor
{
    internal class Program
    {
        private static ApiClient _apiClient = null;
        
        public static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: CurseForgeExtractor <type> <source_zip> <destination_dir>");
                Console.WriteLine("Example: CurseForgeExtractor extract-modpack D:\\mods.zip D:\\output");
                return;
            }

            string type = args[0].ToLower();
            string sourceZipPath = args[1];
            string destinationDir = args[2];

            // Validate arguments
            if (!File.Exists(sourceZipPath))
            {
                Console.WriteLine($"Error: Source file '{sourceZipPath}' does not exist");
                return;
            }

            if (!Directory.Exists(destinationDir))
            {
                try
                {
                    Directory.CreateDirectory(destinationDir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: Could not create destination directory: {ex.Message}");
                    return;
                }
            }

            switch (type)
            {
                case "extract-modpack":
                    ExtractModpack(sourceZipPath, destinationDir).Wait();
                    break;
                default:
                    Console.WriteLine($"Error: Unknown type '{type}'");
                    break;
            }
        }

        private static ApiClient GetApiClient()
        {
            if (_apiClient == null)
            {
                var apiKey = Environment.GetEnvironmentVariable("CURSEFORGE_API_KEY");
                _apiClient = new ApiClient(apiKey);
            }
            
            return _apiClient;
        }

        private static async Task ExtractModpack(string sourceZipPath, string destinationDir)
        {
            Version mcVersion;
            Dictionary<int, int> mods = new();
            string title;
            Version forgeVersion;
            
            try
            {
                using (var archive = System.IO.Compression.ZipFile.OpenRead(sourceZipPath))
                {
                    var manifestEntry = archive.GetEntry("manifest.json");
                    if (manifestEntry == null)
                    {
                        Console.WriteLine("Error: manifest.json not found in the zip file");
                        return;
                    }

                    using (var reader = new StreamReader(manifestEntry.Open()))
                    {
                        string manifestContent = reader.ReadToEnd();
                        var json = JObject.Parse(manifestContent);

                        if (json["minecraft"] is JObject minecraft)
                        {
                            if (minecraft["version"] is JValue version)
                            {
                                mcVersion = new Version(version.Value<string>());
                            }
                            else
                            {
                                Console.Error.WriteLine("Error: minecraft.version is not a string");
                                return;
                            }

                            if (minecraft["modLoaders"] is JArray modloaders)
                            {
                                if (modloaders.Count != 1 || modloaders[0]["id"]!.Value<string>().StartsWith("forge-"))
                                {
                                    forgeVersion = Version.Parse(modloaders[0]["id"]!.Value<string>().Substring("forge-".Length));
                                }
                                else
                                {
                                    Console.Error.WriteLine("Error: Only one modloader is supported - FORGE!");
                                    return;
                                }
                            }
                            else
                            {
                                Console.Error.WriteLine("Error: minecraft.modloaders is not an array");
                                return;
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("Error: minecraft is not an object");
                            return;
                        }

                        if (json["name"] is JValue name)
                        {
                            title = name.Value<string>();
                        }
                        else
                        {
                            Console.Error.WriteLine("Error: name is not a string");
                            return;
                        }

                        if (json["files"] is JArray files)
                        {
                            foreach (var file in (JArray)json["files"])
                            {
                                mods.Add(file["projectID"]!.Value<int>(), file["fileID"]!.Value<int>());
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("Error: files is not an array");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing zip file: {ex.Message}");
                return;
            }

            using (var apiClient = GetApiClient())
            {
                var response = await apiClient.GetModsByIdListAsync(new GetModsByIdsListRequestBody
                {
                    ModIds = mods.Keys.ToList(),
                    FilterPcOnly = true,
                });

                var invalidMods = response.Data.Where(mod => !mod.IsAvailable).ToList();
                
                if (invalidMods.Any())
                {
                    Console.Error.WriteLine("These mods were invalid and skipped:");
                    foreach (var mod in invalidMods)
                    {
                        Console.Error.WriteLine($"- ({mod.Id}) {mod.Name}");
                    }
                }
                else
                {
                    Console.WriteLine("All mods are valid");
                }
                
                var modsDir = Path.Combine(destinationDir, "mods");
                Directory.CreateDirectory(modsDir);

                using (var client = new System.Net.WebClient())
                {
                    List<Mod> errored = new();
                    
                    int i = 0;
                    foreach (var mod in response.Data)
                    {
                        if (!mod.IsAvailable)
                            continue;
                    
                        var url = await apiClient.GetModFileDownloadUrlAsync(mod.Id, mods[mod.Id]);

                        if (url.Error is { } error)
                        {
                            Console.Error.Write($"[{++i}/{response.Data.Count}] ({mod.Id}) {mod.Name} -> {error.ErrorCode} {error.ErrorMessage} -> skipped due error\n");
                            errored.Add(mod);
                            continue;
                        }

                        Console.Write($"[{++i}/{response.Data.Count}] ({mod.Id}) {mod.Name} -> {url.Data}");

                        var fileName = Path.GetFileName(HttpUtility.UrlDecode(url.Data));
                        var filePath = Path.Combine(modsDir, fileName);

                        if (File.Exists(filePath))
                        {
                            Console.Write(" -> skipped\n");
                        }
                        else
                        {
                            client.DownloadFile(url.Data!, filePath);
                            Console.Write(" -> downloaded\n");
                        }
                    }

                    if (errored.Any())
                    {
                        Console.Error.WriteLine("Overal errored:");
                        foreach (var mod in errored)
                        {
                            Console.Error.WriteLine($"- ({mod.Id}) {mod.Name}");
                        }
                    }
                }
                
                // Extract overrides folder if it exists
                try
                {
                    using (var archive = ZipFile.OpenRead(sourceZipPath))
                    {
                        var overrideEntries = archive.Entries.Where(e => e.FullName.StartsWith("overrides/", StringComparison.OrdinalIgnoreCase));
        
                        foreach (var entry in overrideEntries)
                        {
                            // Remove the "overrides/" prefix from the path
                            string destinationPath = Path.Combine(destinationDir, 
                                entry.FullName.Substring("overrides/".Length));

                            // Create the directory if this is a directory entry
                            if (entry.Name == "")
                            {
                                Directory.CreateDirectory(destinationPath);
                                continue;
                            }

                            // Create directory structure for the file
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                            // Extract the file
                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                    Console.WriteLine("Overrides extracted successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error extracting overrides: {ex.Message}");
                }
            }
            
            try
            {
                var repoData = new
                {
                    Title = title,
                    Loader = "forge",
                    MCVersion = mcVersion.ToString(),
                    LoaderVersion = forgeVersion.ToString(),
                    RepoUrl = "<REPO_URL>",
                };
        
                string repoJsonPath = Path.Combine(destinationDir, "repo.json");
                File.WriteAllText(repoJsonPath, 
                    Newtonsoft.Json.JsonConvert.SerializeObject(repoData, Newtonsoft.Json.Formatting.Indented));
        
                Console.WriteLine($"Repository information written to {repoJsonPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing repo.json: {ex.Message}");
            }
        }
    }
}