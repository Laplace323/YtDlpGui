using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace YtDlpGui.Tools;

public class ToolManager
{
    private readonly string _toolDirectory;


    public ToolManager()
    {
        _toolDirectory = GetToolDirectory();

        Directory.CreateDirectory(
            _toolDirectory);
    }


    // =========================================================
    // ツールディレクトリ
    // =========================================================

    /// <summary>
    /// ツールを格納するディレクトリ
    /// </summary>
    public string ToolDirectory =>
        _toolDirectory;


    // =========================================================
    // OS判定
    // =========================================================

    public bool IsWindows =>
        RuntimeInformation.IsOSPlatform(
            OSPlatform.Windows);


    public bool IsLinux =>
        RuntimeInformation.IsOSPlatform(
            OSPlatform.Linux);


    public bool IsAndroid =>
        OperatingSystem.IsAndroid();


    // =========================================================
    // 実行ファイル名
    // =========================================================

    public string YtDlpFileName =>
        IsWindows
            ? "yt-dlp.exe"
            : "yt-dlp";


    public string FfmpegFileName =>
        IsWindows
            ? "ffmpeg.exe"
            : "ffmpeg";


    public string FfprobeFileName =>
        IsWindows
            ? "ffprobe.exe"
            : "ffprobe";


    public string DenoFileName =>
        IsWindows
            ? "deno.exe"
            : "deno";


    // =========================================================
    // 実行ファイルのパス
    // =========================================================

    public string YtDlpPath =>
        Path.Combine(
            _toolDirectory,
            YtDlpFileName);


    public string FfmpegPath =>
        Path.Combine(
            _toolDirectory,
            FfmpegFileName);


    public string FfprobePath =>
        Path.Combine(
            _toolDirectory,
            FfprobeFileName);


    public string DenoPath =>
        Path.Combine(
            _toolDirectory,
            DenoFileName);


    // =========================================================
    // 存在確認
    // =========================================================

    public bool IsYtDlpInstalled =>
        File.Exists(
            YtDlpPath);


    public bool IsFfmpegInstalled =>
        File.Exists(
            FfmpegPath);


    public bool IsFfprobeInstalled =>
        File.Exists(
            FfprobePath);


    public bool IsDenoInstalled =>
        File.Exists(
            DenoPath);


    public bool AreAllToolsInstalled =>
        IsYtDlpInstalled &&
        IsFfmpegInstalled &&
        IsFfprobeInstalled &&
        IsDenoInstalled;


    // =========================================================
    // バージョン取得
    // =========================================================

    public async Task<string>
        GetYtDlpVersionAsync()
    {
        if (!IsYtDlpInstalled)
            return "未インストール";


        return await RunCommandAsync(
            YtDlpPath,
            "--version");
    }


    public async Task<string>
        GetFfmpegVersionAsync()
    {
        if (!IsFfmpegInstalled)
            return "未インストール";


        string result =
            await RunCommandAsync(
                FfmpegPath,
                "-version");


        return ExtractFirstLine(
            result);
    }


    public async Task<string>
        GetFfprobeVersionAsync()
    {
        if (!IsFfprobeInstalled)
            return "未インストール";


        string result =
            await RunCommandAsync(
                FfprobePath,
                "-version");


        return ExtractFirstLine(
            result);
    }


    public async Task<string>
        GetDenoVersionAsync()
    {
        if (!IsDenoInstalled)
            return "未インストール";


        string result =
            await RunCommandAsync(
                DenoPath,
                "--version");


        return ExtractFirstLine(
            result);
    }


    // =========================================================
    // ツール実行
    // =========================================================

    public async Task<string>
        RunYtDlpAsync(
            string arguments)
    {
        if (!IsYtDlpInstalled)
        {
            throw new FileNotFoundException(
                "yt-dlpが見つかりません。",
                YtDlpPath);
        }


        return await RunCommandAsync(
            YtDlpPath,
            arguments);
    }


    public async Task<string>
        RunFfmpegAsync(
            string arguments)
    {
        if (!IsFfmpegInstalled)
        {
            throw new FileNotFoundException(
                "FFmpegが見つかりません。",
                FfmpegPath);
        }


        return await RunCommandAsync(
            FfmpegPath,
            arguments);
    }


    public async Task<string>
        RunDenoAsync(
            string arguments)
    {
        if (!IsDenoInstalled)
        {
            throw new FileNotFoundException(
                "Denoが見つかりません。",
                DenoPath);
        }


        return await RunCommandAsync(
            DenoPath,
            arguments);
    }


    // =========================================================
    // 共通プロセス実行
    // =========================================================

    private async Task<string>
        RunCommandAsync(
            string executable,
            string arguments)
    {
        var output =
            new StringBuilder();


        var startInfo =
            new ProcessStartInfo
            {
                FileName =
                    executable,

                Arguments =
                    arguments,

                UseShellExecute =
                    false,

                RedirectStandardOutput =
                    true,

                RedirectStandardError =
                    true,

                CreateNoWindow =
                    true
            };


        // UTF-8を優先
        startInfo.StandardOutputEncoding =
            Encoding.UTF8;

        startInfo.StandardErrorEncoding =
            Encoding.UTF8;


        using var process =
            new Process();


        process.StartInfo =
            startInfo;


        process.OutputDataReceived +=
            (_, e) =>
            {
                if (!string.IsNullOrEmpty(
                    e.Data))
                {
                    output.AppendLine(
                        e.Data);
                }
            };


        process.ErrorDataReceived +=
            (_, e) =>
            {
                if (!string.IsNullOrEmpty(
                    e.Data))
                {
                    output.AppendLine(
                        e.Data);
                }
            };


        if (!process.Start())
        {
            throw new Exception(
                $"実行できませんでした: {executable}");
        }


        process.BeginOutputReadLine();

        process.BeginErrorReadLine();


        await process.WaitForExitAsync();


        return output
            .ToString()
            .Trim();
    }


    // =========================================================
    // ツールディレクトリ
    // =========================================================

    private static string
        GetToolDirectory()
    {
        string appData =
            Environment.GetFolderPath(
                Environment.SpecialFolder
                    .LocalApplicationData);


        return Path.Combine(
            appData,
            "YtDlpGui",
            "tools");
    }


    // =========================================================
    // 先頭行取得
    // =========================================================

    private static string
        ExtractFirstLine(
            string text)
    {
        if (string.IsNullOrWhiteSpace(
            text))
        {
            return "不明";
        }


        using var reader =
            new StringReader(
                text);


        return reader
            .ReadLine()?
            .Trim()
            ?? "不明";
    }
}