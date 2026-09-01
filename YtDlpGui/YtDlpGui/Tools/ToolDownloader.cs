using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace YtDlpGui.Tools;

public class ToolDownloader
{
    private readonly ToolManager _toolManager;

    private static readonly HttpClient HttpClient =
        CreateHttpClient();


    // =========================================================
    // ダウンロードURL
    // =========================================================

    private const string YtDlpDownloadUrl =
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";


    private const string FfmpegDownloadUrl =
        "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";


    private const string DenoDownloadUrl =
        "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";


    // =========================================================
    // 最新バージョン確認用（GitHub API）
    //
    // ※ GitHub APIはUser-Agentヘッダーが無いと
    //    リクエストを拒否するため、HttpClient生成時に
    //    設定する（CreateHttpClient参照）。
    // =========================================================

    private const string YtDlpLatestReleaseApiUrl =
        "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";


    private const string DenoLatestReleaseApiUrl =
        "https://api.github.com/repos/denoland/deno/releases/latest";


    // FFmpeg(BtbNビルド)は"latest"という固定タグを
    // 使い回すローリング配布のため、
    // releases/latest ではなく releases/tags/latest を使う
    private const string FfmpegLatestReleaseApiUrl =
        "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/tags/latest";


    private const string FfmpegAssetFileName =
        "ffmpeg-master-latest-win64-gpl.zip";


    private static HttpClient CreateHttpClient()
    {
        var client =
            new HttpClient();


        client.DefaultRequestHeaders
            .UserAgent
            .ParseAdd(
                "YtDlpGui/1.0");


        return client;
    }


    // =========================================================
    // コンストラクター
    // =========================================================

    public ToolDownloader(
        ToolManager toolManager)
    {
        _toolManager =
            toolManager
            ?? throw new ArgumentNullException(
                nameof(toolManager));
    }


    // =========================================================
    // yt-dlp更新
    // =========================================================

    public async Task DownloadYtDlpAsync(
        IProgress<double>? progress = null)
    {
        string destination =
            _toolManager.YtDlpPath;

        string temporaryFile =
            destination + ".download";

        string backupFile =
            destination + ".backup";


        try
        {
            // -------------------------------------------------
            // 1. 一時ファイル削除
            // -------------------------------------------------

            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }


            // -------------------------------------------------
            // 2. バックアップ
            // -------------------------------------------------

            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }


            if (File.Exists(destination))
            {
                File.Move(
                    destination,
                    backupFile);
            }


            // -------------------------------------------------
            // 3. ダウンロード
            // -------------------------------------------------

            using HttpResponseMessage response =
                await HttpClient.GetAsync(
                    YtDlpDownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();


            long? contentLength =
                response.Content.Headers.ContentLength;


            // -------------------------------------------------
            // 4. 一時ファイルへ保存
            // -------------------------------------------------

            await using Stream input =
                await response.Content
                    .ReadAsStreamAsync();

            await using FileStream output =
                new FileStream(
                    temporaryFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);


            byte[] buffer =
                new byte[81920];

            long totalBytes = 0;

            int bytesRead;


            while ((bytesRead =
                await input.ReadAsync(
                    buffer.AsMemory(
                        0,
                        buffer.Length))) > 0)
            {
                await output.WriteAsync(
                    buffer.AsMemory(
                        0,
                        bytesRead));

                totalBytes +=
                    bytesRead;


                if (contentLength.HasValue &&
                    contentLength.Value > 0)
                {
                    double percent =
                        (double)totalBytes /
                        contentLength.Value *
                        100.0;

                    progress?.Report(
                        Math.Clamp(
                            percent,
                            0,
                            100));
                }
            }


            await output.FlushAsync();


            // ==================================================
            // ★ バグ修正（FFmpeg/Deno更新と同じ問題）
            //
            // outputをFileShare.Noneで開いたまま
            // File.Move()で本体へ移動しようとすると、
            // 「別のプロセスが使用中」のIOExceptionが発生する。
            // 移動の前に明示的に閉じる。
            // ==================================================

            await output.DisposeAsync();


            // -------------------------------------------------
            // 5. 本体へ移動
            // -------------------------------------------------

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }


            File.Move(
                temporaryFile,
                destination);


            // -------------------------------------------------
            // 6. バックアップ削除
            // -------------------------------------------------

            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }


            // -------------------------------------------------
            // 7. 実行権限
            // -------------------------------------------------

            SetExecutablePermission(
                destination);


            progress?.Report(100);
        }
        catch
        {
            // -------------------------------------------------
            // 一時ファイル削除
            // -------------------------------------------------

            try
            {
                if (File.Exists(temporaryFile))
                {
                    File.Delete(
                        temporaryFile);
                }
            }
            catch
            {
            }


            // -------------------------------------------------
            // バックアップ復元
            // -------------------------------------------------

            try
            {
                if (File.Exists(backupFile))
                {
                    if (File.Exists(destination))
                    {
                        File.Delete(
                            destination);
                    }


                    File.Move(
                        backupFile,
                        destination);
                }
            }
            catch
            {
            }


            throw;
        }
    }


    // =========================================================
    // FFmpeg / FFprobe 更新
    // =========================================================

    public async Task DownloadFfmpegAsync(
        IProgress<double>? progress = null)
    {
        string toolsDirectory =
            Path.GetDirectoryName(
                _toolManager.YtDlpPath)!;


        string zipFile =
            Path.Combine(
                toolsDirectory,
                "ffmpeg.download.zip");


        string extractDirectory =
            Path.Combine(
                toolsDirectory,
                "ffmpeg_temp");


        string ffmpegPath =
            _toolManager.FfmpegPath;


        string ffprobePath =
            _toolManager.FfprobePath;


        string ffmpegBackup =
            ffmpegPath + ".backup";


        string ffprobeBackup =
            ffprobePath + ".backup";


        try
        {
            // -------------------------------------------------
            // 1. 一時ファイル削除
            // -------------------------------------------------

            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }


            if (Directory.Exists(
                extractDirectory))
            {
                Directory.Delete(
                    extractDirectory,
                    true);
            }


            // -------------------------------------------------
            // 2. バックアップ
            // -------------------------------------------------

            if (File.Exists(ffmpegBackup))
            {
                File.Delete(
                    ffmpegBackup);
            }


            if (File.Exists(ffprobeBackup))
            {
                File.Delete(
                    ffprobeBackup);
            }


            if (File.Exists(ffmpegPath))
            {
                File.Move(
                    ffmpegPath,
                    ffmpegBackup);
            }


            if (File.Exists(ffprobePath))
            {
                File.Move(
                    ffprobePath,
                    ffprobeBackup);
            }


            // -------------------------------------------------
            // 3. ZIPダウンロード
            // -------------------------------------------------

            using HttpResponseMessage response =
                await HttpClient.GetAsync(
                    FfmpegDownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead);


            response.EnsureSuccessStatusCode();


            long? contentLength =
                response.Content.Headers.ContentLength;


            await using Stream input =
                await response.Content
                    .ReadAsStreamAsync();


            await using FileStream output =
                new FileStream(
                    zipFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);


            byte[] buffer =
                new byte[1024 * 1024];


            long totalBytes = 0;

            int bytesRead;


            while ((bytesRead =
                await input.ReadAsync(
                    buffer.AsMemory(
                        0,
                        buffer.Length))) > 0)
            {
                await output.WriteAsync(
                    buffer.AsMemory(
                        0,
                        bytesRead));


                totalBytes +=
                    bytesRead;


                if (contentLength.HasValue &&
                    contentLength.Value > 0)
                {
                    double percent =
                        (double)totalBytes /
                        contentLength.Value *
                        90.0;


                    progress?.Report(
                        Math.Clamp(
                            percent,
                            0,
                            90));
                }
            }


            await output.FlushAsync();


            // ==================================================
            // ★ バグ修正
            //
            // outputは`await using`で宣言されているため、
            // このメソッドを抜けるまでファイルハンドルが
            // 閉じられない（FileShare.Noneで開いているため
            // 他のハンドルからは読み込めない）。
            // このままZipFile.ExtractToDirectory()を呼ぶと、
            // 「別のプロセスが使用中」のIOExceptionが発生し、
            // 進捗92%（展開直前）で更新が失敗していた。
            //
            // 展開の前に明示的に閉じる。
            // （`await using`によるDisposeは二重に呼ばれるが、
            //   FileStreamは多重Disposeしても安全）
            // ==================================================

            await output.DisposeAsync();


            // -------------------------------------------------
            // 4. 展開
            // -------------------------------------------------

            progress?.Report(92);


            ZipFile.ExtractToDirectory(
                zipFile,
                extractDirectory);


            // -------------------------------------------------
            // 5. 実行ファイル検索
            // -------------------------------------------------

            string? extractedFfmpeg =
                Directory.GetFiles(
                    extractDirectory,
                    "ffmpeg.exe",
                    SearchOption.AllDirectories)
                .FirstOrDefault();


            string? extractedFfprobe =
                Directory.GetFiles(
                    extractDirectory,
                    "ffprobe.exe",
                    SearchOption.AllDirectories)
                .FirstOrDefault();


            if (extractedFfmpeg == null)
            {
                throw new FileNotFoundException(
                    "FFmpegの展開後にffmpeg.exeが見つかりませんでした。");
            }


            if (extractedFfprobe == null)
            {
                throw new FileNotFoundException(
                    "FFmpegの展開後にffprobe.exeが見つかりませんでした。");
            }


            progress?.Report(95);


            // -------------------------------------------------
            // 6. コピー
            // -------------------------------------------------

            File.Copy(
                extractedFfmpeg,
                ffmpegPath,
                true);


            File.Copy(
                extractedFfprobe,
                ffprobePath,
                true);


            SetExecutablePermission(
                ffmpegPath);


            SetExecutablePermission(
                ffprobePath);


            progress?.Report(98);


            // -------------------------------------------------
            // 7. 一時ファイル削除
            // -------------------------------------------------

            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }


            if (Directory.Exists(
                extractDirectory))
            {
                Directory.Delete(
                    extractDirectory,
                    true);
            }


            // -------------------------------------------------
            // 8. バックアップ削除
            // -------------------------------------------------

            if (File.Exists(ffmpegBackup))
            {
                File.Delete(
                    ffmpegBackup);
            }


            if (File.Exists(ffprobeBackup))
            {
                File.Delete(
                    ffprobeBackup);
            }


            // -------------------------------------------------
            // 9. 導入日時を記録
            //
            // FFmpegはバージョン番号で最新判定ができないため、
            // 「いつ導入したか」を記録しておき、後で
            // GitHub側のアセット更新日時と比較する。
            // -------------------------------------------------

            await RecordFfmpegInstalledNowAsync();


            progress?.Report(100);
        }
        catch
        {
            // -------------------------------------------------
            // 一時ファイル削除
            // -------------------------------------------------

            try
            {
                if (File.Exists(zipFile))
                {
                    File.Delete(zipFile);
                }


                if (Directory.Exists(
                    extractDirectory))
                {
                    Directory.Delete(
                        extractDirectory,
                        true);
                }
            }
            catch
            {
            }


            // -------------------------------------------------
            // FFmpeg復元
            // -------------------------------------------------

            try
            {
                if (File.Exists(ffmpegBackup))
                {
                    if (File.Exists(ffmpegPath))
                    {
                        File.Delete(
                            ffmpegPath);
                    }


                    File.Move(
                        ffmpegBackup,
                        ffmpegPath);
                }
            }
            catch
            {
            }


            // -------------------------------------------------
            // FFprobe復元
            // -------------------------------------------------

            try
            {
                if (File.Exists(ffprobeBackup))
                {
                    if (File.Exists(ffprobePath))
                    {
                        File.Delete(
                            ffprobePath);
                    }


                    File.Move(
                        ffprobeBackup,
                        ffprobePath);
                }
            }
            catch
            {
            }


            throw;
        }
    }


    // =========================================================
    // Deno更新
    // =========================================================

    public async Task DownloadDenoAsync(
        IProgress<double>? progress = null)
    {
        string toolsDirectory =
            _toolManager.ToolDirectory;


        string zipFile =
            Path.Combine(
                toolsDirectory,
                "deno.download.zip");


        string extractDirectory =
            Path.Combine(
                toolsDirectory,
                "deno_temp");


        string denoPath =
            _toolManager.DenoPath;


        string backupFile =
            denoPath + ".backup";


        try
        {
            // -------------------------------------------------
            // 1. 一時ファイル削除
            // -------------------------------------------------

            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }


            if (Directory.Exists(
                extractDirectory))
            {
                Directory.Delete(
                    extractDirectory,
                    true);
            }


            // -------------------------------------------------
            // 2. バックアップ
            // -------------------------------------------------

            if (File.Exists(backupFile))
            {
                File.Delete(
                    backupFile);
            }


            if (File.Exists(denoPath))
            {
                File.Move(
                    denoPath,
                    backupFile);
            }


            // -------------------------------------------------
            // 3. Deno ZIPダウンロード
            // -------------------------------------------------

            using HttpResponseMessage response =
                await HttpClient.GetAsync(
                    DenoDownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead);


            response.EnsureSuccessStatusCode();


            long? contentLength =
                response.Content.Headers.ContentLength;


            await using Stream input =
                await response.Content
                    .ReadAsStreamAsync();


            await using FileStream output =
                new FileStream(
                    zipFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);


            byte[] buffer =
                new byte[1024 * 1024];


            long totalBytes = 0;

            int bytesRead;


            while ((bytesRead =
                await input.ReadAsync(
                    buffer.AsMemory(
                        0,
                        buffer.Length))) > 0)
            {
                await output.WriteAsync(
                    buffer.AsMemory(
                        0,
                        bytesRead));


                totalBytes +=
                    bytesRead;


                if (contentLength.HasValue &&
                    contentLength.Value > 0)
                {
                    double percent =
                        (double)totalBytes /
                        contentLength.Value *
                        90.0;


                    progress?.Report(
                        Math.Clamp(
                            percent,
                            0,
                            90));
                }
            }


            await output.FlushAsync();


            // ==================================================
            // ★ バグ修正（FFmpeg更新と同じ問題）
            //
            // outputをFileShare.Noneで開いたまま
            // ZipFile.ExtractToDirectory()を呼ぶと、
            // 「別のプロセスが使用中」のIOExceptionが発生する。
            // 展開の前に明示的に閉じる。
            // ==================================================

            await output.DisposeAsync();


            // -------------------------------------------------
            // 4. 展開
            // -------------------------------------------------

            progress?.Report(92);


            ZipFile.ExtractToDirectory(
                zipFile,
                extractDirectory);


            // -------------------------------------------------
            // 5. deno.exe検索
            // -------------------------------------------------

            string? extractedDeno =
                Directory.GetFiles(
                    extractDirectory,
                    "deno.exe",
                    SearchOption.AllDirectories)
                .FirstOrDefault();


            if (extractedDeno == null)
            {
                throw new FileNotFoundException(
                    "Denoの展開後にdeno.exeが見つかりませんでした。");
            }


            progress?.Report(96);


            // -------------------------------------------------
            // 6. toolsフォルダへコピー
            // -------------------------------------------------

            File.Copy(
                extractedDeno,
                denoPath,
                true);


            SetExecutablePermission(
                denoPath);


            progress?.Report(98);


            // -------------------------------------------------
            // 7. 一時ファイル削除
            // -------------------------------------------------

            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }


            if (Directory.Exists(
                extractDirectory))
            {
                Directory.Delete(
                    extractDirectory,
                    true);
            }


            // -------------------------------------------------
            // 8. バックアップ削除
            // -------------------------------------------------

            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }


            progress?.Report(100);
        }
        catch
        {
            // -------------------------------------------------
            // 一時ファイル削除
            // -------------------------------------------------

            try
            {
                if (File.Exists(zipFile))
                {
                    File.Delete(zipFile);
                }


                if (Directory.Exists(
                    extractDirectory))
                {
                    Directory.Delete(
                        extractDirectory,
                        true);
                }
            }
            catch
            {
            }


            // -------------------------------------------------
            // バックアップ復元
            // -------------------------------------------------

            try
            {
                if (File.Exists(backupFile))
                {
                    if (File.Exists(denoPath))
                    {
                        File.Delete(
                            denoPath);
                    }


                    File.Move(
                        backupFile,
                        denoPath);
                }
            }
            catch
            {
            }


            throw;
        }
    }


    // =========================================================
    // 実行権限
    // =========================================================

    private static void SetExecutablePermission(
        string filePath)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }


        try
        {
            File.SetUnixFileMode(
                filePath,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute |
                UnixFileMode.GroupRead |
                UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead |
                UnixFileMode.OtherExecute);
        }
        catch
        {
        }
    }


    // =========================================================
    // 最新バージョン確認（yt-dlp / Deno）
    //
    // yt-dlp / Denoは、GitHubリリースのタグ名が
    // そのままバージョン番号になっているため、
    // インストール済みバージョンと直接比較できる。
    // =========================================================

    public async Task<string?> GetLatestYtDlpVersionAsync()
    {
        return await GetLatestGitHubTagAsync(
            YtDlpLatestReleaseApiUrl);
    }


    public async Task<string?> GetLatestDenoVersionAsync()
    {
        string? tag =
            await GetLatestGitHubTagAsync(
                DenoLatestReleaseApiUrl);


        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }


        // "v2.1.4" -> "2.1.4"
        return tag.StartsWith(
            "v",
            StringComparison.OrdinalIgnoreCase)
            ? tag.Substring(1)
            : tag;
    }


    private async Task<string?> GetLatestGitHubTagAsync(
        string apiUrl)
    {
        try
        {
            using HttpResponseMessage response =
                await HttpClient.GetAsync(
                    apiUrl);


            if (!response.IsSuccessStatusCode)
            {
                return null;
            }


            string json =
                await response.Content
                    .ReadAsStringAsync();


            using JsonDocument document =
                JsonDocument.Parse(json);


            if (document.RootElement.TryGetProperty(
                    "tag_name",
                    out JsonElement tagElement))
            {
                return tagElement.GetString();
            }


            return null;
        }
        catch
        {
            // ネットワークエラー・レート制限等は
            // 「確認できない」として扱う
            // （更新自体は失敗させない）
            return null;
        }
    }


    // =========================================================
    // FFmpeg 最新版確認（日付ベース）
    //
    // BtbNビルドは"latest"タグを使い回すローリング配布で
    // 意味のあるバージョン番号を持たないため、
    // GitHub上のアセット更新日時と、こちらで記録した
    // 導入日時を比較する簡易判定を行う。
    // =========================================================

    public async Task<DateTimeOffset?>
        GetLatestFfmpegAssetUpdatedAtAsync()
    {
        try
        {
            using HttpResponseMessage response =
                await HttpClient.GetAsync(
                    FfmpegLatestReleaseApiUrl);


            if (!response.IsSuccessStatusCode)
            {
                return null;
            }


            string json =
                await response.Content
                    .ReadAsStringAsync();


            using JsonDocument document =
                JsonDocument.Parse(json);


            if (!document.RootElement.TryGetProperty(
                    "assets",
                    out JsonElement assets) ||
                assets.ValueKind != JsonValueKind.Array)
            {
                return null;
            }


            foreach (JsonElement asset
                in assets.EnumerateArray())
            {
                if (!asset.TryGetProperty(
                        "name",
                        out JsonElement nameElement))
                {
                    continue;
                }


                if (!string.Equals(
                        nameElement.GetString(),
                        FfmpegAssetFileName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }


                if (asset.TryGetProperty(
                        "updated_at",
                        out JsonElement updatedAtElement) &&
                    updatedAtElement.ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(
                        updatedAtElement.GetString(),
                        out DateTimeOffset updatedAt))
                {
                    return updatedAt;
                }
            }


            return null;
        }
        catch
        {
            return null;
        }
    }


    // ==================================================
    // FFmpeg導入日時の取得
    // ==================================================

    public async Task<DateTimeOffset?>
        GetFfmpegInstalledAtAsync()
    {
        ToolMetadata metadata =
            await ToolMetadata.LoadAsync(
                _toolManager.ToolDirectory);


        return metadata.FfmpegInstalledAtUtc;
    }


    // ==================================================
    // FFmpeg導入日時の記録（更新成功時に呼ぶ）
    // ==================================================

    private async Task RecordFfmpegInstalledNowAsync()
    {
        ToolMetadata metadata =
            await ToolMetadata.LoadAsync(
                _toolManager.ToolDirectory);


        metadata.FfmpegInstalledAtUtc =
            DateTimeOffset.UtcNow;


        await metadata.SaveAsync(
            _toolManager.ToolDirectory);
    }
}