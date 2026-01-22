using System.Diagnostics;

namespace PEI_Doctors.Services;

public class ViteDevServerService : IHostedService, IDisposable
{
    private Process? _viteProcess;
    private readonly ILogger<ViteDevServerService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _clientAppPath;

    public ViteDevServerService(ILogger<ViteDevServerService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _clientAppPath = Path.Combine(environment.ContentRootPath, "ClientApp");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return;
        }

        // Check if Vite dev server is already running
        if (IsViteServerRunning())
        {
            _logger.LogInformation("Vite dev server is already running on port 5173");
            return;
        }

        _logger.LogInformation("Starting Vite dev server...");

        try
        {
            // On Windows, use cmd.exe to run npm (most reliable way to find npm in PATH)
            // On other platforms, try to find npm directly
            string fileName;
            string arguments;
            
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Use cmd.exe which will use the shell's PATH to find npm
                fileName = "cmd.exe";
                arguments = "/c npm run dev";
            }
            else
            {
                // On Unix-like systems, try to find npm or use it directly
                var npmPath = FindNpmPath();
                fileName = npmPath ?? "npm";
                arguments = "run dev";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = _clientAppPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _viteProcess = new Process { StartInfo = startInfo };
            _viteProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogDebug("Vite: {Output}", e.Data);
                }
            };
            _viteProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogWarning("Vite: {Error}", e.Data);
                }
            };

            _viteProcess.Start();
            _viteProcess.BeginOutputReadLine();
            _viteProcess.BeginErrorReadLine();

            // Wait a bit for the server to start
            await Task.Delay(3000, cancellationToken);

            // Check if it's running now
            var retries = 0;
            while (!IsViteServerRunning() && retries < 10)
            {
                await Task.Delay(1000, cancellationToken);
                retries++;
            }

            if (IsViteServerRunning())
            {
                _logger.LogInformation("Vite dev server started successfully");
            }
            else
            {
                _logger.LogWarning("Vite dev server may not have started. Check the logs above for errors.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Vite dev server");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_viteProcess != null && !_viteProcess.HasExited)
        {
            _logger.LogInformation("Stopping Vite dev server...");
            try
            {
                _viteProcess.Kill(true);
                _viteProcess.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Vite dev server");
            }
        }

        return Task.CompletedTask;
    }

    private bool IsViteServerRunning()
    {
        try
        {
            using var tcpClient = new System.Net.Sockets.TcpClient();
            var result = tcpClient.BeginConnect("127.0.0.1", 5173, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
            if (success)
            {
                tcpClient.EndConnect(result);
                return true;
            }
        }
        catch
        {
            // Server is not running
        }
        return false;
    }

    private string? FindNpmPath()
    {
        // Try to find npm using where.exe (Windows) or which (Unix)
        try
        {
            var whereCommand = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where.exe" : "which";
            var startInfo = new ProcessStartInfo
            {
                FileName = whereCommand,
                Arguments = "npm",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    // Get the first path
                    var paths = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (paths.Length > 0)
                    {
                        return paths[0].Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to find npm using where/which command");
        }

        // Fallback: Try common Node.js installation paths on Windows
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            var possiblePaths = new[]
            {
                Path.Combine(programFiles, "nodejs", "npm.cmd"),
                Path.Combine(programFiles, "nodejs", "npm"),
                Path.Combine(programFilesX86, "nodejs", "npm.cmd"),
                Path.Combine(programFilesX86, "nodejs", "npm"),
                Path.Combine(appData, "npm", "npm.cmd"),
                Path.Combine(appData, "npm", "npm")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        // Last resort: try just "npm" - might work if it's in PATH but where.exe failed
        return "npm";
    }

    public void Dispose()
    {
        _viteProcess?.Dispose();
    }
}
