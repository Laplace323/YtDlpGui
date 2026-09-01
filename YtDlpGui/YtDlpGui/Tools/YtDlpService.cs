using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YtDlpGui.Tools;

public class YtDlpService
{
    private readonly ToolManager _toolManager;

    private Process? _currentProcess;

    private readonly object _processLock = new();


    // ==================================================
    // コンストラクタ
    // ==================================================

    public YtDlpService(
        ToolManager toolManager)
    {
        _toolManager = toolManager;
    }


    // ==================================================
    // 動画情報取得
    // ==================================================

    public async Task<VideoInfo?> GetVideoInfoAsync(
        string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException(
                "URLが指定されていません。",
                nameof(url));
        }


        string ytDlpPath =
            _toolManager.YtDlpPath;


        if (string.IsNullOrWhiteSpace(ytDlpPath) ||
            !File.Exists(ytDlpPath))
        {
            throw new FileNotFoundException(
                "yt-dlpが見つかりません。",
                ytDlpPath);
        }


        var psi =
            CreateProcessStartInfo(
                ytDlpPath);


        psi.ArgumentList.Add(
            "--dump-single-json");

        psi.ArgumentList.Add(
            "--no-download");

        psi.ArgumentList.Add(
            "--no-warnings");

        psi.ArgumentList.Add(
            "--no-playlist");

        psi.ArgumentList.Add(
            url);


        string stdout;

        string stderr;


        using var process =
            new Process
            {
                StartInfo = psi
            };


        lock (_processLock)
        {
            _currentProcess = process;
        }


        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "yt-dlpを起動できませんでした。");
            }


            Task<string> stdoutTask =
                process.StandardOutput
                    .ReadToEndAsync();


            Task<string> stderrTask =
                process.StandardError
                    .ReadToEndAsync();


            await process.WaitForExitAsync();


            stdout =
                await stdoutTask;

            stderr =
                await stderrTask;


            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(stderr)
                        ? $"yt-dlpが終了コード {process.ExitCode} を返しました。"
                        : stderr.Trim());
            }
        }
        finally
        {
            lock (_processLock)
            {
                if (ReferenceEquals(
                    _currentProcess,
                    process))
                {
                    _currentProcess = null;
                }
            }
        }


        if (string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException(
                "yt-dlpから動画情報を取得できませんでした。");
        }


        try
        {
            using JsonDocument document =
                JsonDocument.Parse(stdout);


            JsonElement root =
                document.RootElement;


            return ParseVideoInfo(root);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "yt-dlpのJSON解析に失敗しました。",
                ex);
        }
    }


    // ==================================================
    // プレイリスト情報取得
    //
    // ※明示的にプレイリストとして取得するときだけ使用
    // ==================================================

    public async Task<PlaylistInfo?> GetPlaylistInfoAsync(
        string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException(
                "URLが指定されていません。",
                nameof(url));
        }


        string ytDlpPath =
            _toolManager.YtDlpPath;


        if (string.IsNullOrWhiteSpace(ytDlpPath) ||
            !File.Exists(ytDlpPath))
        {
            throw new FileNotFoundException(
                "yt-dlpが見つかりません。",
                ytDlpPath);
        }


        var psi =
            CreateProcessStartInfo(
                ytDlpPath);


        psi.ArgumentList.Add(
            "--flat-playlist");

        psi.ArgumentList.Add(
            "--dump-single-json");

        psi.ArgumentList.Add(
            "--no-download");

        psi.ArgumentList.Add(
            "--no-warnings");

        psi.ArgumentList.Add(
            url);


        string stdout;

        string stderr;


        using var process =
            new Process
            {
                StartInfo = psi
            };


        lock (_processLock)
        {
            _currentProcess = process;
        }


        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "yt-dlpを起動できませんでした。");
            }


            Task<string> stdoutTask =
                process.StandardOutput
                    .ReadToEndAsync();


            Task<string> stderrTask =
                process.StandardError
                    .ReadToEndAsync();


            await process.WaitForExitAsync();


            stdout =
                await stdoutTask;

            stderr =
                await stderrTask;


            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(stderr)
                        ? $"yt-dlpが終了コード {process.ExitCode} を返しました。"
                        : stderr.Trim());
            }
        }
        finally
        {
            lock (_processLock)
            {
                if (ReferenceEquals(
                    _currentProcess,
                    process))
                {
                    _currentProcess = null;
                }
            }
        }


        if (string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException(
                "yt-dlpからプレイリスト情報を取得できませんでした。");
        }


        try
        {
            using JsonDocument document =
                JsonDocument.Parse(stdout);


            JsonElement root =
                document.RootElement;


            return ParsePlaylistInfo(root);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "プレイリスト情報のJSON解析に失敗しました。",
                ex);
        }
    }


    // ==================================================
    // ダウンロード
    // ==================================================

    public async Task DownloadAsync(
        string url,
        string quality,
        string audioQuality,
        string format,
        string outputDirectory,
        bool thumbnailEnabled,
        bool subtitleEnabled,
        bool autoGeneratedSubtitleEnabled,
        IProgress<double>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException(
                "URLが指定されていません。",
                nameof(url));
        }


        if (string.IsNullOrWhiteSpace(
            outputDirectory))
        {
            throw new ArgumentException(
                "保存先が指定されていません。",
                nameof(outputDirectory));
        }


        string ytDlpPath =
            _toolManager.YtDlpPath;


        if (string.IsNullOrWhiteSpace(ytDlpPath) ||
            !File.Exists(ytDlpPath))
        {
            throw new FileNotFoundException(
                "yt-dlpが見つかりません。",
                ytDlpPath);
        }


        Directory.CreateDirectory(
            outputDirectory);


        string normalizedFormat =
            NormalizeFormat(format);


        var psi =
            CreateProcessStartInfo(
                ytDlpPath);


        // ==================================================
        // 基本設定
        // ==================================================

        psi.ArgumentList.Add(
            "--newline");

        psi.ArgumentList.Add(
            "--no-warnings");


        // ==================================================
        // 単体URLとしてダウンロード
        //
        // URLに &list=xxxxx が付いていても
        // プレイリストとして扱わない
        // ==================================================

        psi.ArgumentList.Add(
            "--no-playlist");

        psi.ArgumentList.Add(
            "--ignore-errors");


        // ==================================================
        // YouTubeクライアント
        //
        // 一部の動画で通常のクライアントでは
        // HTTP 403 Forbiddenになるため、
        // web_embeddedクライアントを使用する。
        // ==================================================

        psi.ArgumentList.Add(
            "--extractor-args");

        psi.ArgumentList.Add(
            "youtube:player_client=web_embedded");


        // ==================================================
        // メタデータ
        //
        // Title       = 動画タイトル
        // Artist      = チャンネル名
        // Album       = 今回は設定しない
        // Genre       = 空欄
        // Date        = 投稿日
        // Description = 動画説明
        // Comment     = 動画URL
        // Cover       = サムネイル
        //
        // ※ playlist_title → Album は使用しない
        // ==================================================

        psi.ArgumentList.Add(
            "--embed-metadata");


        // Artist
        psi.ArgumentList.Add(
            "--parse-metadata");

        psi.ArgumentList.Add(
            "%(uploader)s:%(meta_artist)s");


        // Date
        psi.ArgumentList.Add(
            "--parse-metadata");

        psi.ArgumentList.Add(
            "%(upload_date|)s:%(meta_date)s");


        // Description
        psi.ArgumentList.Add(
            "--parse-metadata");

        psi.ArgumentList.Add(
            "%(description|)s:%(meta_description)s");


        // Comment
        psi.ArgumentList.Add(
            "--parse-metadata");

        psi.ArgumentList.Add(
            "%(webpage_url)s:%(meta_comment)s");


        // ==================================================
        // FFmpeg
        // ==================================================

        string? toolDirectory =
            Path.GetDirectoryName(
                ytDlpPath);


        if (!string.IsNullOrWhiteSpace(
            toolDirectory))
        {
            psi.ArgumentList.Add(
                "--ffmpeg-location");

            psi.ArgumentList.Add(
                toolDirectory);
        }


        // ==================================================
        // 出力先
        // ==================================================

        string outputTemplate =
            Path.Combine(
                outputDirectory,
                "%(title)s.%(ext)s");


        psi.ArgumentList.Add(
            "-o");

        psi.ArgumentList.Add(
            outputTemplate);


        // ==================================================
        // 形式
        // ==================================================

        bool isVideoFormat =
            normalizedFormat == "mp4" ||
            normalizedFormat == "mkv" ||
            normalizedFormat == "webm";


        bool isAudioFormat =
            normalizedFormat == "mp3" ||
            normalizedFormat == "m4a" ||
            normalizedFormat == "flac" ||
            normalizedFormat == "wav";


        // ==================================================
        // 映像形式
        // ==================================================

        if (isVideoFormat)
        {
            string formatSelector =
                BuildVideoFormatSelector(
                    quality);


            psi.ArgumentList.Add(
                "-f");

            psi.ArgumentList.Add(
                formatSelector);


            psi.ArgumentList.Add(
                "--merge-output-format");

            psi.ArgumentList.Add(
                normalizedFormat);


            // -------------------------------------------------
            // 字幕
            // -------------------------------------------------

            if (subtitleEnabled)
            {
                if (autoGeneratedSubtitleEnabled)
                {
                    psi.ArgumentList.Add(
                        "--write-auto-subs");
                }
                else
                {
                    psi.ArgumentList.Add(
                        "--write-subs");
                }


                psi.ArgumentList.Add(
                    "--sub-langs");

                psi.ArgumentList.Add(
                    "ja");


                psi.ArgumentList.Add(
                    "--embed-subs");
            }
        }


        // ==================================================
        // 音声形式
        // ==================================================

        if (isAudioFormat)
        {
            psi.ArgumentList.Add(
                "-f");

            psi.ArgumentList.Add(
                "ba/b");


            psi.ArgumentList.Add(
                "-x");


            psi.ArgumentList.Add(
                "--audio-format");

            psi.ArgumentList.Add(
                normalizedFormat);


            string? audioQualityValue =
                ConvertAudioQuality(
                    audioQuality);


            if (!string.IsNullOrWhiteSpace(
                audioQualityValue))
            {
                psi.ArgumentList.Add(
                    "--audio-quality");

                psi.ArgumentList.Add(
                    audioQualityValue);
            }
        }


        // ==================================================
        // サムネイル
        // ==================================================

        if (thumbnailEnabled)
        {
            psi.ArgumentList.Add(
                "--write-thumbnail");


            psi.ArgumentList.Add(
                "--embed-thumbnail");
        }


        // ==================================================
        // URL
        // ==================================================

        psi.ArgumentList.Add(
            url);


        // ==================================================
        // 実行
        // ==================================================

        using var process =
            new Process
            {
                StartInfo = psi
            };


        lock (_processLock)
        {
            _currentProcess = process;
        }


        var stderrBuilder =
            new StringBuilder();


        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "yt-dlpを起動できませんでした。");
            }


            // -------------------------------------------------
            // stdout / stderr を並列で読む
            // -------------------------------------------------

            Task stdoutTask =
                ReadOutputAsync(
                    process.StandardOutput,
                    progress);


            Task stderrTask =
                ReadErrorAsync(
                    process.StandardError,
                    stderrBuilder);


            await Task.WhenAll(
                stdoutTask,
                stderrTask);


            await process.WaitForExitAsync();


            if (process.ExitCode != 0)
            {
                string error =
                    stderrBuilder
                        .ToString()
                        .Trim();


                if (string.IsNullOrWhiteSpace(
                    error))
                {
                    error =
                        $"yt-dlpが終了コード " +
                        $"{process.ExitCode} を返しました。";
                }


                throw new InvalidOperationException(
                    error);
            }


            // ==================================================
            // yt-dlp終了後の不要なWebPサムネイル削除
            //
            // --embed-thumbnail で動画へ埋め込んだ後、
            // 元の .webp ファイルが残っている場合がある。
            //
            // 動画本体は削除せず、
            // ダウンロード先直下の .webp のみ削除する。
            // ==================================================

            if (thumbnailEnabled)
            {
                DeleteWebpThumbnails(
                    outputDirectory);
            }


            progress?.Report(
                100);
        }
        catch
        {
            throw;
        }
        finally
        {
            lock (_processLock)
            {
                if (ReferenceEquals(
                    _currentProcess,
                    process))
                {
                    _currentProcess = null;
                }
            }
        }
    }


    // ==================================================
    // WebPサムネイル削除
    // ==================================================

    private static void DeleteWebpThumbnails(
        string outputDirectory)
    {
        try
        {
            if (!Directory.Exists(
                outputDirectory))
            {
                return;
            }


            string[] files =
                Directory.GetFiles(
                    outputDirectory,
                    "*.webp",
                    SearchOption.TopDirectoryOnly);


            foreach (string file in files)
            {
                try
                {
                    File.Delete(
                        file);
                }
                catch
                {
                    // サムネイル削除失敗は
                    // ダウンロード成功扱いを維持する。
                }
            }
        }
        catch
        {
            // ディレクトリ検索自体の失敗も
            // ダウンロード成功扱いを維持する。
        }
    }


    // ==================================================
    // yt-dlpプロセスキャンセル
    // ==================================================

    public void CancelDownload()
    {
        Process? process;


        lock (_processLock)
        {
            process =
                _currentProcess;
        }


        if (process == null)
        {
            return;
        }


        try
        {
            if (!process.HasExited)
            {
                process.Kill(
                    true);
            }
        }
        catch
        {
            // キャンセル時のKill失敗は無視
        }
    }


    // ==================================================
    // ProcessStartInfo
    // ==================================================

    private static ProcessStartInfo
        CreateProcessStartInfo(
            string executable)
    {
        return new ProcessStartInfo
        {
            FileName =
                executable,

            UseShellExecute =
                false,

            RedirectStandardOutput =
                true,

            RedirectStandardError =
                true,

            CreateNoWindow =
                true,

            StandardOutputEncoding =
                Encoding.UTF8,

            StandardErrorEncoding =
                Encoding.UTF8
        };
    }


    // ==================================================
    // stdout読み取り
    // ==================================================

    private static async Task ReadOutputAsync(
        StreamReader reader,
        IProgress<double>? progress)
    {
        while (true)
        {
            string? line =
                await reader.ReadLineAsync();


            if (line == null)
            {
                break;
            }


            double? value =
                ParseProgress(
                    line);


            if (value.HasValue)
            {
                progress?.Report(
                    value.Value);
            }
        }
    }


    // ==================================================
    // stderr読み取り
    // ==================================================

    private static async Task ReadErrorAsync(
        StreamReader reader,
        StringBuilder errorBuilder)
    {
        while (true)
        {
            string? line =
                await reader.ReadLineAsync();


            if (line == null)
            {
                break;
            }


            if (!string.IsNullOrWhiteSpace(line))
            {
                errorBuilder
                    .AppendLine(line);
            }
        }
    }


    // ==================================================
    // 進捗解析
    // ==================================================

    private static double? ParseProgress(
        string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }


        Match match =
            Regex.Match(
                line,
                @"\b(\d+(?:\.\d+)?)%");


        if (!match.Success)
        {
            return null;
        }


        if (!double.TryParse(
            match.Groups[1].Value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double value))
        {
            return null;
        }


        return Math.Clamp(
            value,
            0,
            100);
    }


    // ==================================================
    // 動画形式の画質指定
    // ==================================================

    private static string
        BuildVideoFormatSelector(
            string quality)
    {
        return quality switch
        {
            "2160p (4K)" =>
                "bv*[height<=2160]+ba/b[height<=2160]",

            "1440p (2K)" =>
                "bv*[height<=1440]+ba/b[height<=1440]",

            "1080p" =>
                "bv*[height<=1080]+ba/b[height<=1080]",

            "720p" =>
                "bv*[height<=720]+ba/b[height<=720]",

            "480p" =>
                "bv*[height<=480]+ba/b[height<=480]",

            "360p" =>
                "bv*[height<=360]+ba/b[height<=360]",

            _ =>
                "bv*+ba/b"
        };
    }


    // ==================================================
    // 音質変換
    // ==================================================

    private static string?
        ConvertAudioQuality(
            string quality)
    {
        return quality switch
        {
            "320kbps" => "320K",
            "256kbps" => "256K",
            "192kbps" => "192K",
            "128kbps" => "128K",

            _ => null
        };
    }


    // ==================================================
    // 形式正規化
    // ==================================================

    private static string
        NormalizeFormat(
            string format)
    {
        return format
            .Trim()
            .ToLowerInvariant() switch
        {
            "mp4" => "mp4",
            "mkv" => "mkv",
            "webm" => "webm",

            "mp3" => "mp3",
            "m4a" => "m4a",
            "flac" => "flac",
            "wav" => "wav",

            _ => "mp4"
        };
    }


    // ==================================================
    // VideoInfo解析
    // ==================================================

    private static VideoInfo
        ParseVideoInfo(
            JsonElement root)
    {
        var info =
            new VideoInfo();


        info.Title =
            GetString(
                root,
                "title");


        info.Channel =
            GetString(
                root,
                "channel");


        if (string.IsNullOrWhiteSpace(
            info.Channel))
        {
            info.Channel =
                GetString(
                    root,
                    "uploader");
        }


        // --------------------------------------------------
        // 単体URLではプレイリスト情報を使用しない
        // --------------------------------------------------

        info.PlaylistTitle = "";


        info.Description =
            GetString(
                root,
                "description");


        info.WebpageUrl =
            GetString(
                root,
                "webpage_url");


        info.UploadDate =
            GetString(
                root,
                "upload_date");


        info.Thumbnail =
            GetString(
                root,
                "thumbnail");


        info.Duration =
            GetDouble(
                root,
                "duration");


        info.Width =
            GetInt(
                root,
                "width");


        info.Height =
            GetInt(
                root,
                "height");


        info.DurationText =
            FormatDuration(
                info.Duration);


        info.ResolutionText =
            FormatResolution(
                info.Width,
                info.Height);


        info.Formats =
            ReadFormats(
                root);


        return info;
    }


    // ==================================================
    // PlaylistInfo解析
    // ==================================================

    private static PlaylistInfo
        ParsePlaylistInfo(
            JsonElement root)
    {
        var playlist =
            new PlaylistInfo();


        playlist.Title =
            GetString(
                root,
                "title");


        if (root.TryGetProperty(
            "entries",
            out JsonElement entries) &&
            entries.ValueKind ==
            JsonValueKind.Array)
        {
            foreach (
                JsonElement entry
                in entries.EnumerateArray())
            {
                if (entry.ValueKind !=
                    JsonValueKind.Object)
                {
                    continue;
                }


                string url =
                    GetString(
                        entry,
                        "webpage_url");


                if (string.IsNullOrWhiteSpace(
                    url))
                {
                    url =
                        GetString(
                            entry,
                            "url");
                }


                if (string.IsNullOrWhiteSpace(
                    url))
                {
                    string? id =
                        GetString(
                            entry,
                            "id");


                    if (!string.IsNullOrWhiteSpace(
                        id))
                    {
                        url =
                            $"https://www.youtube.com/watch?v={id}";
                    }
                }


                if (string.IsNullOrWhiteSpace(
                    url))
                {
                    continue;
                }


                string title =
                    GetString(
                        entry,
                        "title");


                playlist.Entries.Add(
                    new PlaylistEntry
                    {
                        Url =
                            url,

                        Title =
                            title
                    });
            }
        }


        return playlist;
    }


    // ==================================================
    // Format解析
    // ==================================================

    private static List<FormatInfo>
        ReadFormats(
            JsonElement root)
    {
        var result =
            new List<FormatInfo>();


        if (!root.TryGetProperty(
            "formats",
            out JsonElement formats) ||
            formats.ValueKind !=
            JsonValueKind.Array)
        {
            return result;
        }


        foreach (
            JsonElement format
            in formats.EnumerateArray())
        {
            if (format.ValueKind !=
                JsonValueKind.Object)
            {
                continue;
            }


            var item =
                new FormatInfo
                {
                    FormatId =
                        GetString(
                            format,
                            "format_id"),

                    VideoCodec =
                        GetString(
                            format,
                            "vcodec"),

                    AudioCodec =
                        GetString(
                            format,
                            "acodec"),

                    Width =
                        GetInt(
                            format,
                            "width"),

                    Height =
                        GetInt(
                            format,
                            "height"),

                    FPS =
                        GetDouble(
                            format,
                            "fps"),

                    FileSize =
                        GetLongNullable(
                            format,
                            "filesize"),

                    FileSizeApprox =
                        GetLongNullable(
                            format,
                            "filesize_approx"),

                    Bitrate =
                        GetDouble(
                            format,
                            "tbr"),

                    AudioBitrate =
                        GetDouble(
                            format,
                            "abr"),

                    Extension =
                        GetString(
                            format,
                            "ext")
                };


            item.HasVideo =
                !string.IsNullOrWhiteSpace(
                    item.VideoCodec) &&
                !item.VideoCodec.Equals(
                    "none",
                    StringComparison.OrdinalIgnoreCase);


            item.HasAudio =
                !string.IsNullOrWhiteSpace(
                    item.AudioCodec) &&
                !item.AudioCodec.Equals(
                    "none",
                    StringComparison.OrdinalIgnoreCase);


            item.HasVideoAndAudio =
                item.HasVideo &&
                item.HasAudio;


            item.ResolutionText =
                FormatResolution(
                    item.Width,
                    item.Height);


            long? size =
                item.FileSize ??
                item.FileSizeApprox;


            item.FileSizeText =
                FormatFileSize(
                    size);


            result.Add(
                item);
        }


        return result;
    }


    // ==================================================
    // JSON文字列取得
    // ==================================================

    private static string
        GetString(
            JsonElement root,
            string propertyName)
    {
        if (!root.TryGetProperty(
            propertyName,
            out JsonElement value))
        {
            return "";
        }


        if (value.ValueKind ==
            JsonValueKind.String)
        {
            return value.GetString() ?? "";
        }


        return "";
    }


    // ==================================================
    // JSON int取得
    // ==================================================

    private static int
        GetInt(
            JsonElement root,
            string propertyName)
    {
        if (!root.TryGetProperty(
            propertyName,
            out JsonElement value))
        {
            return 0;
        }


        if (value.ValueKind ==
            JsonValueKind.Number &&
            value.TryGetInt32(
                out int result))
        {
            return result;
        }


        return 0;
    }


    // ==================================================
    // JSON double取得
    // ==================================================

    private static double
        GetDouble(
            JsonElement root,
            string propertyName)
    {
        if (!root.TryGetProperty(
            propertyName,
            out JsonElement value))
        {
            return 0;
        }


        if (value.ValueKind ==
            JsonValueKind.Number &&
            value.TryGetDouble(
                out double result))
        {
            return result;
        }


        return 0;
    }


    // ==================================================
    // JSON long?取得
    // ==================================================

    private static long?
        GetLongNullable(
            JsonElement root,
            string propertyName)
    {
        if (!root.TryGetProperty(
            propertyName,
            out JsonElement value))
        {
            return null;
        }


        if (value.ValueKind ==
            JsonValueKind.Number &&
            value.TryGetInt64(
                out long result))
        {
            return result;
        }


        return null;
    }


    // ==================================================
    // 再生時間
    // ==================================================

    private static string
        FormatDuration(
            double seconds)
    {
        if (seconds <= 0)
        {
            return "再生時間: -";
        }


        TimeSpan time =
            TimeSpan.FromSeconds(
                seconds);


        if (time.TotalHours >= 1)
        {
            return
                $"再生時間: " +
                $"{(int)time.TotalHours:00}:" +
                $"{time.Minutes:00}:" +
                $"{time.Seconds:00}";
        }


        return
            $"再生時間: " +
            $"{time.Minutes:00}:" +
            $"{time.Seconds:00}";
    }


    // ==================================================
    // 解像度
    // ==================================================

    private static string
        FormatResolution(
            int width,
            int height)
    {
        if (width <= 0 ||
            height <= 0)
        {
            return "解像度: -";
        }


        return
            $"解像度: {width}x{height}";
    }


    // ==================================================
    // ファイルサイズ
    // ==================================================

    private static string
        FormatFileSize(
            long? bytes)
    {
        if (!bytes.HasValue ||
            bytes.Value <= 0)
        {
            return "-";
        }


        double size =
            bytes.Value;


        string[] units =
        {
            "B",
            "KB",
            "MB",
            "GB",
            "TB"
        };


        int unitIndex =
            0;


        while (
            size >= 1024 &&
            unitIndex <
            units.Length - 1)
        {
            size /= 1024;

            unitIndex++;
        }


        return
            $"{size:0.##} " +
            $"{units[unitIndex]}";
    }
}


// ======================================================
// VideoInfo
// ======================================================

public class VideoInfo
{
    public string Title
    {
        get;
        set;
    } = "";


    public string Channel
    {
        get;
        set;
    } = "";


    // ==================================================
    // プレイリスト名
    //
    // 単体URLでは使用しない
    // ==================================================

    public string PlaylistTitle
    {
        get;
        set;
    } = "";


    // ==================================================
    // 動画説明
    // ==================================================

    public string Description
    {
        get;
        set;
    } = "";


    // ==================================================
    // 動画URL
    // ==================================================

    public string WebpageUrl
    {
        get;
        set;
    } = "";


    // ==================================================
    // 投稿日
    // ==================================================

    public string UploadDate
    {
        get;
        set;
    } = "";


    // ==================================================
    // サムネイルURL
    // ==================================================

    public string Thumbnail
    {
        get;
        set;
    } = "";


    public double Duration
    {
        get;
        set;
    }


    public int Width
    {
        get;
        set;
    }


    public int Height
    {
        get;
        set;
    }


    public string DurationText
    {
        get;
        set;
    } = "再生時間: -";


    public string ResolutionText
    {
        get;
        set;
    } = "解像度: -";


    public List<FormatInfo> Formats
    {
        get;
        set;
    } = new();
}


// ======================================================
// FormatInfo
// ======================================================

public class FormatInfo
{
    public string FormatId
    {
        get;
        set;
    } = "";


    public string VideoCodec
    {
        get;
        set;
    } = "";


    public string AudioCodec
    {
        get;
        set;
    } = "";


    public int Width
    {
        get;
        set;
    }


    public int Height
    {
        get;
        set;
    }


    public double FPS
    {
        get;
        set;
    }


    public long? FileSize
    {
        get;
        set;
    }


    public long? FileSizeApprox
    {
        get;
        set;
    }


    public double Bitrate
    {
        get;
        set;
    }


    public double AudioBitrate
    {
        get;
        set;
    }


    public string Extension
    {
        get;
        set;
    } = "";


    public bool HasVideo
    {
        get;
        set;
    }


    public bool HasAudio
    {
        get;
        set;
    }


    public bool HasVideoAndAudio
    {
        get;
        set;
    }


    public string ResolutionText
    {
        get;
        set;
    } = "-";


    public string FileSizeText
    {
        get;
        set;
    } = "-";
}


// ======================================================
// PlaylistInfo
// ======================================================

public class PlaylistInfo
{
    public string Title
    {
        get;
        set;
    } = "";


    public List<PlaylistEntry> Entries
    {
        get;
        set;
    } = new();
}


// ======================================================
// PlaylistEntry
// ======================================================

public class PlaylistEntry
{
    public string Url
    {
        get;
        set;
    } = "";


    public string Title
    {
        get;
        set;
    } = "";
}