using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YtDlpGui.Models;

// ======================================================
// DownloadQueueItem
//
// ★ 分割リファクタリング
//
// 以前はMainViewModel.csの末尾に同居していたが、
// これはViewModelではなくデータモデルなので、
// Modelsフォルダーへ独立させた。
// 中身はそのまま（挙動の変更なし）。
// ======================================================

public class DownloadQueueItem :
    INotifyPropertyChanged
{
    // ==================================================
    // URL
    // ==================================================

    private string _url = "";

    public string Url
    {
        get => _url;

        set
        {
            if (_url == value)
                return;

            _url = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // タイトル
    // ==================================================

    private string _title = "";

    public string Title
    {
        get => _title;

        set
        {
            if (_title == value)
                return;

            _title = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // チャンネル
    // ==================================================

    private string _channel = "";

    public string Channel
    {
        get => _channel;

        set
        {
            if (_channel == value)
                return;

            _channel = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // プレイリスト名
    // ==================================================

    private string _playlistTitle = "";

    public string PlaylistTitle
    {
        get => _playlistTitle;

        set
        {
            if (_playlistTitle == value)
                return;

            _playlistTitle = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // 画質
    // ==================================================

    private string _quality = "最高";

    public string Quality
    {
        get => _quality;

        set
        {
            if (_quality == value)
                return;

            _quality = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // 音質
    // ==================================================

    private string _audioQuality =
        "最高";

    public string AudioQuality
    {
        get => _audioQuality;

        set
        {
            if (_audioQuality == value)
                return;

            _audioQuality = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // 形式
    // ==================================================

    private string _format =
        "MP4";

    public string Format
    {
        get => _format;

        set
        {
            if (_format == value)
                return;

            _format = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // プレイリストフォルダー
    // ==================================================

    private bool _playlistFolderEnabled =
        true;

    public bool PlaylistFolderEnabled
    {
        get => _playlistFolderEnabled;

        set
        {
            if (_playlistFolderEnabled == value)
                return;

            _playlistFolderEnabled =
                value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // ファイル名テンプレート
    // ==================================================

    private string _fileNameTemplate =
        "%(title)s.%(ext)s";

    public string FileNameTemplate
    {
        get => _fileNameTemplate;

        set
        {
            if (_fileNameTemplate == value)
                return;

            _fileNameTemplate =
                value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // 状態
    // ==================================================

    private string _status =
        "待機中";

    public string Status
    {
        get => _status;

        set
        {
            if (_status == value)
                return;

            _status = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(HasError));

            OnPropertyChanged(
                nameof(IsWaiting));

            OnPropertyChanged(
                nameof(IsDownloading));

            OnPropertyChanged(
                nameof(IsCompleted));
        }
    }


    // ==================================================
    // 状態バッジ用の判定
    //
    // MainView.axamlの状態バッジ（statusBadge）の
    // クラス切り替えに使う。
    // ==================================================

    public bool IsWaiting =>
        Status == "待機中";

    public bool IsDownloading =>
        Status == "ダウンロード中";

    public bool IsCompleted =>
        Status == "完了";


    // ==================================================
    // 進捗
    // ==================================================

    private double _progress;

    public double Progress
    {
        get => _progress;

        set
        {
            double newValue =
                Math.Clamp(
                    value,
                    0,
                    100);


            if (Math.Abs(
                    _progress - newValue)
                < 0.001)
            {
                return;
            }


            _progress =
                newValue;


            OnPropertyChanged();

            OnPropertyChanged(
                nameof(ProgressText));
        }
    }


    public string ProgressText =>
        $"{Progress:0}%";


    // ==================================================
    // エラー内容
    // ==================================================

    private string _errorMessage = "";

    public string ErrorMessage
    {
        get => _errorMessage;

        set
        {
            if (_errorMessage == value)
                return;

            _errorMessage = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(HasError));
        }
    }


    // ==================================================
    // エラー判定
    // ==================================================

    public bool HasError =>
        Status == "エラー" ||
        !string.IsNullOrWhiteSpace(
            ErrorMessage);


    // ==================================================
    // PropertyChanged
    // ==================================================

    public event PropertyChangedEventHandler?
        PropertyChanged;


    private void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?
            .Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
    }
}
