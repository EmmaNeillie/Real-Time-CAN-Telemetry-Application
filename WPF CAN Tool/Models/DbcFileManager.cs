using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WPF_CAN_Tool.Models
{
    public class DbcFileManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string DefaultLocalPath = "CAN_Config.dbc";

        // GitHub repository URLs - using raw.githubusercontent.com for direct file access
        private static readonly string[] GitHubRepoUrls = new[]
        {
            "https://raw.githubusercontent.com/UGRacing-Electronics/UGR_CANDBCFile/main",
            "https://raw.githubusercontent.com/UGRacing-Electronics/UGR_CANDBCFile/master"
        };

        // DBC file names
        private static readonly string[] DbcFileNames = new[]
        {
            "UGR_Main_Bus.dbc",
            "CAN_Config.dbc"
        };

        public DbcFileManager()
        {
            // Set timeout for HTTP requests
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Attempts to fetch DBC file from GitHub repo, falls back to local file if available
        /// </summary>
        public async Task<(bool Success, string FilePath, string ErrorMessage)> GetDbcFileAsync()
        {
            try
            {
                // Try to fetch from GitHub first
                var (gitHubSuccess, gitHubPath) = await FetchFromGitHubAsync();
                if (gitHubSuccess)
                {
                    return (true, gitHubPath, string.Empty);
                }

                // Fallback to local files - try both names
                foreach (var fileName in DbcFileNames)
                {
                    string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                    if (File.Exists(localPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ Using local DBC file: {localPath}");
                        return (true, localPath, string.Empty);
                    }
                }

                // If both fail, return error
                return (false, string.Empty, "Failed to load DBC file from GitHub or local storage. Please select a file manually.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Error in GetDbcFileAsync: {ex.Message}");
                return (false, string.Empty, $"Error loading DBC file: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to fetch DBC file from the GitHub repository
        /// </summary>
        private async Task<(bool Success, string FilePath)> FetchFromGitHubAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("⏳ Attempting to fetch DBC file from GitHub...");

                foreach (var repoUrl in GitHubRepoUrls)
                {
                    foreach (var fileName in DbcFileNames)
                    {
                        string fileUrl = $"{repoUrl}/{fileName}";
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"  Trying: {fileUrl}");
                            
                            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                            {
                                var response = await _httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseContentRead, cts.Token);
                                
                                if (response.IsSuccessStatusCode)
                                {
                                    var content = await response.Content.ReadAsStringAsync();
                                    
                                    // Verify it looks like a DBC file
                                    if (content.Contains("BO_") || content.Contains("SG_"))
                                    {
                                        // Save to local cache
                                        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultLocalPath);
                                        File.WriteAllText(localPath, content);
                                        
                                        System.Diagnostics.Debug.WriteLine($"✓ Successfully fetched DBC file from GitHub: {fileUrl}");
                                        return (true, localPath);
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"  HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                                }
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"  HTTP Error: {ex.Message}");
                            continue;
                        }
                        catch (OperationCanceledException)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Timeout fetching {fileName}");
                            continue;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Error fetching {fileName}: {ex.Message}");
                            continue;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("✗ Could not fetch DBC file from any GitHub URL");
                return (false, string.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Error fetching from GitHub: {ex.Message}");
                return (false, string.Empty);
            }
        }

        /// <summary>
        /// Downloads a specific DBC file from GitHub
        /// </summary>
        public async Task<(bool Success, string FilePath, string ErrorMessage)> DownloadDbcFileAsync(string fileName)
        {
            try
            {
                foreach (var repoUrl in GitHubRepoUrls)
                {
                    string fileUrl = $"{repoUrl}/{fileName}";
                    try
                    {
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                        {
                            var response = await _httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseContentRead, cts.Token);

                            if (response.IsSuccessStatusCode)
                            {
                                var content = await response.Content.ReadAsStringAsync();
                                string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultLocalPath);
                                File.WriteAllText(localPath, content);
                                
                                System.Diagnostics.Debug.WriteLine($"✓ Successfully downloaded {fileName} from GitHub");
                                return (true, localPath, string.Empty);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Trying {repoUrl}: {ex.Message}");
                        continue;
                    }
                }

                return (false, string.Empty, $"Failed to download {fileName} from any GitHub URL");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Error downloading {fileName}: {ex.Message}");
                return (false, string.Empty, $"Error downloading file: {ex.Message}");
            }
        }
    }
}
