using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace YtDlpGui.Tools;

// ==================================================
// ツール内部メタデータ
//
// AppSettings（ユーザーが変更する設定）とは別に、
// アプリ内部だけで使う情報を保持する。
//
// 現状はFFmpegの「導入日時」のみ。
// FFmpeg(BtbNビルド)はセマンティックなバージョン番号を
// 持たないローリング配布のため、「最新かどうか」を
// yt-dlp/Denoのようにバージョン番号同士で比較できない。
// 代わりに「自分がいつ導入したか」と
// 「GitHub側のアセットがいつ更新されたか」を
// 比較する日付ベースの簡易判定に使う。
// ==================================================

public class ToolMetadata
{
    public DateTimeOffset? FfmpegInstalledAtUtc
    {
        get;
        set;
    }


    // ==================================================
    // ファイルパス
    // ==================================================

    private static string GetMetadataFilePath(
        string toolDirectory)
    {
        return Path.Combine(
            toolDirectory,
            "tool-metadata.json");
    }


    // ==================================================
    // 読み込み
    // ==================================================

    public static async Task<ToolMetadata> LoadAsync(
        string toolDirectory)
    {
        try
        {
            string path =
                GetMetadataFilePath(
                    toolDirectory);


            if (!File.Exists(path))
            {
                return new ToolMetadata();
            }


            string json =
                await File.ReadAllTextAsync(
                    path);


            if (string.IsNullOrWhiteSpace(json))
            {
                return new ToolMetadata();
            }


            var metadata =
                JsonSerializer.Deserialize<ToolMetadata>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });


            return metadata
                ?? new ToolMetadata();
        }
        catch
        {
            // メタデータが壊れていても
            // アプリ自体は起動できるようにする
            return new ToolMetadata();
        }
    }


    // ==================================================
    // 保存
    // ==================================================

    public async Task SaveAsync(
        string toolDirectory)
    {
        Directory.CreateDirectory(
            toolDirectory);


        var options =
            new JsonSerializerOptions
            {
                WriteIndented = true
            };


        string json =
            JsonSerializer.Serialize(
                this,
                options);


        await File.WriteAllTextAsync(
            GetMetadataFilePath(
                toolDirectory),
            json);
    }
}
