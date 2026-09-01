using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using YtDlpGui.Commands;
using YtDlpGui.Models;
using YtDlpGui.Services;
using YtDlpGui.Tools;

namespace YtDlpGui.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    // ==================================================
    // フィールド
    // ==================================================

    private readonly YtDlpService _ytDlpService;

    private readonly ToolManager _toolManager;

    private readonly ToolDownloader _toolDownloader;

    private readonly INavigationService _navigationService;


    // ==================================================
    // ToolManager
    //
    // 設定画面(SettingsViewModel)から同じインスタンスを
    // 参照させるために公開する。
    // これまでは設定画面が独自にnew ToolManager()して
    // いたため、ツールの状態把握が2系統に分裂していた。
    // ==================================================

    public ToolManager ToolManager =>
        _toolManager;


    // ==================================================
    // QueueManager
    //
    // ★ 分割リファクタリング
    //
    // キューへの追加・ダウンロード処理・進捗計算・
    // キャンセル/再試行/削除は、すべてここへ移設した。
    // MainViewModelはStatus/Progress/ErrorMessage/
    // IsProcessingQueueといったバインド用プロパティの
    // 「窓口」として残り、実処理はQueueManagerへ委譲する。
    // ==================================================

    private readonly QueueManager _queueManager;

    private bool _isProcessingQueue;

    private AppSettings _settings;

    private bool _settingsLoaded;


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

            RaiseCommandStates();
        }
    }


    // ==================================================
    // 画質
    // ==================================================

    private string _selectedQuality = "最高";

    public string SelectedQuality
    {
        get => _selectedQuality;

        set
        {
            if (_selectedQuality == value)
                return;

            _selectedQuality = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(Quality));
        }
    }


    // ==================================================
    // 既存コードとの互換性
    // ==================================================

    public string Quality
    {
        get => _selectedQuality;

        set
        {
            if (_selectedQuality == value)
                return;

            _selectedQuality = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(SelectedQuality));
        }
    }


    // ==================================================
    // 画質選択肢
    // ==================================================

    public ObservableCollection<string>
        QualityOptions
    {
        get;
    }


    // ==================================================
    // 音質
    // ==================================================

    private string _selectedAudioQuality =
        "最高";

    public string SelectedAudioQuality
    {
        get => _selectedAudioQuality;

        set
        {
            if (_selectedAudioQuality == value)
                return;

            _selectedAudioQuality = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(AudioQuality));
        }
    }


    // ==================================================
    // 既存コードとの互換性
    // ==================================================

    public string AudioQuality
    {
        get => _selectedAudioQuality;

        set
        {
            if (_selectedAudioQuality == value)
                return;

            _selectedAudioQuality = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(SelectedAudioQuality));
        }
    }


    // ==================================================
    // 音質選択肢
    // ==================================================

    public ObservableCollection<string>
        AudioQualityOptions
    {
        get;
    }


    // ==================================================
    // 保存形式
    // ==================================================

    private string _selectedFormat =
        "MP4";

    public string SelectedFormat
    {
        get => _selectedFormat;

        set
        {
            if (_selectedFormat == value)
                return;

            _selectedFormat = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(Format));
        }
    }


    // ==================================================
    // 既存コードとの互換性
    // ==================================================

    public string Format
    {
        get =>
            _selectedFormat.ToLowerInvariant();

        set
        {
            string normalized =
                value?.ToUpperInvariant()
                ?? "MP4";

            if (_selectedFormat == normalized)
                return;

            _selectedFormat =
                normalized;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(SelectedFormat));
        }
    }


    // ==================================================
    // 保存形式選択肢
    // ==================================================

    public ObservableCollection<string>
        FormatOptions
    {
        get;
    }


    // ==================================================
    // 保存先
    // ==================================================

    private string _outputDirectory =
        Environment.GetFolderPath(
            Environment.SpecialFolder.MyVideos);

    public string OutputDirectory
    {
        get => _outputDirectory;

        set
        {
            if (_outputDirectory == value)
                return;

            _outputDirectory = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // サムネイル
    // ==================================================

    private bool _thumbnailEnabled = true;

    public bool ThumbnailEnabled
    {
        get => _thumbnailEnabled;

        set
        {
            if (_thumbnailEnabled == value)
                return;

            _thumbnailEnabled = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // 字幕
    // ==================================================

    private bool _subtitleEnabled = true;

    public bool SubtitleEnabled
    {
        get => _subtitleEnabled;

        set
        {
            if (_subtitleEnabled == value)
                return;

            _subtitleEnabled = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // 自動生成字幕
    // ==================================================

    private bool _autoGeneratedSubtitleEnabled;

    public bool AutoGeneratedSubtitleEnabled
    {
        get => _autoGeneratedSubtitleEnabled;

        set
        {
            if (_autoGeneratedSubtitleEnabled == value)
                return;

            _autoGeneratedSubtitleEnabled = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // プレイリストフォルダー
    // ==================================================

    private bool _playlistFolderEnabled = true;

    public bool PlaylistFolderEnabled
    {
        get => _playlistFolderEnabled;

        set
        {
            if (_playlistFolderEnabled == value)
                return;

            _playlistFolderEnabled = value;

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

            _fileNameTemplate = value;

            OnPropertyChanged();
        }
    }


    // ==================================================
    // 動画情報
    // ==================================================

    private VideoInfo? _videoInfo;

    public VideoInfo? VideoInfo
    {
        get => _videoInfo;

        private set
        {
            if (ReferenceEquals(
                    _videoInfo,
                    value))
            {
                return;
            }

            _videoInfo = value;

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

        private set
        {
            if (_status == value)
                return;

            _status = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(StatusText));
        }
    }


    // ==================================================
    // MainView用ステータス
    // ==================================================

    public string StatusText =>
        Status;


    // ==================================================
    // 全体進捗
    // ==================================================

    private double _progress;

    public double Progress
    {
        get => _progress;

        private set
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


    // ==================================================
    // 全体進捗文字列
    // ==================================================

    public string ProgressText =>
        $"{Progress:0}%";


    // ==================================================
    // エラー
    // ==================================================

    private string _errorMessage = "";

    public string ErrorMessage
    {
        get => _errorMessage;

        private set
        {
            if (_errorMessage == value)
                return;

            _errorMessage = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(HasError));

            RaiseCommandStates();
        }
    }


    public bool HasError =>
        !string.IsNullOrWhiteSpace(
            ErrorMessage);


    // ==================================================
    // ツール状態
    // ==================================================

    public string ToolStatus
    {
        get
        {
            if (_toolManager.AreAllToolsInstalled)
            {
                return
                    "yt-dlp / FFmpeg / FFprobe / Deno : OK";
            }

            return
                "必要なツールが未インストールです";
        }
    }


    // ==================================================
    // キュー
    //
    // 実体はQueueManagerが保持している。参照は
    // インスタンス生成後に変わらないため、
    // MainView.axaml側のバインディングは変更不要。
    // ==================================================

    public ObservableCollection<DownloadQueueItem>
        DownloadQueue =>
        _queueManager.DownloadQueue;


    // ==================================================
    // キュー処理中
    // ==================================================

    public bool IsProcessingQueue
    {
        get => _isProcessingQueue;

        private set
        {
            if (_isProcessingQueue == value)
                return;

            _isProcessingQueue = value;

            OnPropertyChanged();

            RaiseCommandStates();
        }
    }


    // ==================================================
    // Commands
    // ==================================================

    public ICommand GetInfoCommand
    {
        get;
    }

    public ICommand AddToQueueCommand
    {
        get;
    }

    public ICommand DownloadCommand
    {
        get;
    }

    public ICommand StartQueueCommand
    {
        get;
    }

    public ICommand CancelCommand
    {
        get;
    }

    public ICommand CancelQueueCommand
    {
        get;
    }

    public ICommand BrowseCommand
    {
        get;
    }

    public ICommand ClearQueueCommand
    {
        get;
    }

    public ICommand RemoveQueueItemCommand
    {
        get;
    }

    public ICommand RetryQueueItemCommand
    {
        get;
    }

    public ICommand CopyErrorCommand
    {
        get;
    }

    public ICommand SettingsCommand
    {
        get;
    }


    // ==================================================
    // コンストラクター
    // ==================================================

    public MainViewModel()
        : this(
            new ToolManager(),
            new DesktopNavigationService())
    {
    }


    public MainViewModel(
        ToolManager toolManager)
        : this(
            toolManager,
            new DesktopNavigationService())
    {
    }


    public MainViewModel(
        ToolManager toolManager,
        INavigationService navigationService)
    {
        _toolManager =
            toolManager
            ?? throw new ArgumentNullException(
                nameof(toolManager));

        _navigationService =
            navigationService
            ?? throw new ArgumentNullException(
                nameof(navigationService));


        _ytDlpService =
            new YtDlpService(
                _toolManager);

        _toolDownloader =
            new ToolDownloader(
                _toolManager);


        // ==================================================
        // QueueManager生成とコールバック配線
        //
        // QueueManager側の処理結果を、MainViewModel自身の
        // バインド用プロパティ（private setterのまま）へ
        // 反映する。ErrorMessage/IsProcessingQueueの
        // setterは元々RaiseCommandStates()を呼ぶ実装のため、
        // ここで改めて呼ぶ必要はない。
        // ==================================================

        _queueManager =
            new QueueManager(
                _ytDlpService);

        _queueManager.OnStatusChanged =
            value => Status = value;

        _queueManager.OnProgressChanged =
            value => Progress = value;

        _queueManager.OnErrorChanged =
            value => ErrorMessage = value;

        _queueManager.OnProcessingChanged =
            value => IsProcessingQueue = value;


        _settings =
            AppSettings.CreateDefault();


        // ==================================================
        // 選択肢
        // ==================================================

        QualityOptions =
            new ObservableCollection<string>
            {
                "最高",
                "2160p (4K)",
                "1440p (2K)",
                "1080p",
                "720p",
                "480p",
                "360p"
            };


        AudioQualityOptions =
            new ObservableCollection<string>
            {
                "最高",
                "320kbps",
                "256kbps",
                "192kbps",
                "128kbps"
            };


        FormatOptions =
            new ObservableCollection<string>
            {
                "MP4",
                "MKV",
                "WebM",
                "MP3",
                "M4A",
                "FLAC",
                "WAV"
            };


        // ==================================================
        // コマンド
        // ==================================================

        GetInfoCommand =
            new RelayCommand(
                async _ =>
                    await GetInfoAsync(),
                _ =>
                    IsToolsReady &&
                    !IsProcessingQueue &&
                    !string.IsNullOrWhiteSpace(
                        Url));


        AddToQueueCommand =
            new RelayCommand(
                async _ =>
                    await AddToQueueAsync(),
                _ =>
                    IsToolsReady &&
                    !IsProcessingQueue &&
                    !string.IsNullOrWhiteSpace(
                        Url));


        DownloadCommand =
            new RelayCommand(
                async _ =>
                    await StartQueueAsync(),
                _ =>
                    IsToolsReady &&
                    !IsProcessingQueue &&
                    DownloadQueue.Count > 0);


        StartQueueCommand =
            DownloadCommand;


        CancelCommand =
            new RelayCommand(
                _ =>
                    CancelQueue(),
                _ =>
                    IsProcessingQueue);


        CancelQueueCommand =
            CancelCommand;


        BrowseCommand =
            new RelayCommand(
                async _ =>
                    await BrowseAsync(),
                _ =>
                    !IsProcessingQueue);


        ClearQueueCommand =
            new RelayCommand(
                _ =>
                    ClearQueue(),
                _ =>
                    !IsProcessingQueue &&
                    DownloadQueue.Count > 0);


        RemoveQueueItemCommand =
            new RelayCommand(
                parameter =>
                    RemoveQueueItem(
                        parameter),
                parameter =>
                    !IsProcessingQueue &&
                    parameter
                        is DownloadQueueItem);


        RetryQueueItemCommand =
            new RelayCommand(
                parameter =>
                    RetryQueueItem(
                        parameter),
                parameter =>
                    !IsProcessingQueue &&
                    parameter
                        is DownloadQueueItem item &&
                    item.HasError);


        CopyErrorCommand =
            new RelayCommand(
                async _ =>
                    await CopyErrorAsync(),
                _ =>
                    HasError);


        SettingsCommand =
            new RelayCommand(
                _ =>
                    _navigationService
                        .ShowSettings(this),
                _ =>
                    !IsProcessingQueue);


        // ==================================================
        // 起動時に設定を読み込む
        // ==================================================

        _ = ReloadSettingsAsync();


        // ==================================================
        // 起動時に必須ツール(yt-dlp/FFmpeg/FFprobe/Deno)の
        // 有無を確認し、不足していれば自動でダウンロードする
        // ==================================================

        _ = EnsureToolsInstalledAsync();
    }


    // ==================================================
    // 必須ツール確認
    //
    // ツールが揃うまで、情報取得・キュー追加・
    // ダウンロード開始を行えないようにする
    // （IsToolsReadyがfalseの間はコマンドが無効化される）。
    // ==================================================

    private bool _isToolsReady;

    public bool IsToolsReady
    {
        get => _isToolsReady;

        private set
        {
            if (_isToolsReady == value)
                return;

            _isToolsReady = value;

            OnPropertyChanged();

            RaiseCommandStates();
        }
    }


    private async Task EnsureToolsInstalledAsync()
    {
        if (_toolManager.AreAllToolsInstalled)
        {
            IsToolsReady =
                true;

            return;
        }


        IsToolsReady =
            false;


        try
        {
            Status =
                "必須ファイルを確認しています...";


            var downloadProgress =
                new Progress<double>(
                    value =>
                        Progress =
                            value);


            if (!_toolManager.IsYtDlpInstalled)
            {
                Status =
                    "yt-dlpをダウンロード中...";

                await _toolDownloader
                    .DownloadYtDlpAsync(
                        downloadProgress);
            }


            if (!_toolManager.IsFfmpegInstalled ||
                !_toolManager.IsFfprobeInstalled)
            {
                Status =
                    "FFmpegをダウンロード中...";

                await _toolDownloader
                    .DownloadFfmpegAsync(
                        downloadProgress);
            }


            if (!_toolManager.IsDenoInstalled)
            {
                Status =
                    "Denoをダウンロード中...";

                await _toolDownloader
                    .DownloadDenoAsync(
                        downloadProgress);
            }


            Progress =
                0;


            if (_toolManager.AreAllToolsInstalled)
            {
                IsToolsReady =
                    true;

                Status =
                    "準備が完了しました。";
            }
            else
            {
                IsToolsReady =
                    false;

                Status =
                    "必須ファイルの準備に失敗しました。" +
                    "設定画面から手動で更新してください。";
            }
        }
        catch (Exception ex)
        {
            Progress =
                0;

            IsToolsReady =
                false;

            Status =
                "必須ファイルの準備に失敗しました" +
                $"（{ex.Message}）。" +
                "設定画面から手動で更新してください。";
        }
    }


    // ==================================================
    // 設定読み込み
    //
    // 起動時だけでなく、設定画面から戻ってきた際にも
    // 同じMainViewModelインスタンスに対して呼び出せるよう
    // publicにしている（インスタンスを作り直さないため）。
    // ==================================================

    public async Task ReloadSettingsAsync()
    {
        try
        {
            Status =
                "設定を読み込んでいます...";


            AppSettings settings =
                await AppSettings.LoadAsync();


            _settings =
                settings;


            ApplySettingsToViewModel();


            _settingsLoaded =
                true;


            Status =
                "設定を読み込みました";
        }
        catch
        {
            // 設定読み込みに失敗しても
            // デフォルト値でアプリを使用できるようにする

            _settings =
                AppSettings.CreateDefault();


            ApplySettingsToViewModel();


            _settingsLoaded =
                true;
        }


        RaiseCommandStates();
    }


    // ==================================================
    // 設定をViewModelへ反映
    // ==================================================

    private void ApplySettingsToViewModel()
    {
        SelectedQuality =
            NormalizeQuality(
                _settings.DefaultQuality);


        SelectedAudioQuality =
            NormalizeAudioQuality(
                _settings.DefaultAudioQuality);


        SelectedFormat =
            NormalizeFormat(
                _settings.DefaultFormat);


        string configuredDirectory =
            _settings.OutputDirectory;


        if (string.IsNullOrWhiteSpace(
                configuredDirectory))
        {
            OutputDirectory =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyVideos);
        }
        else
        {
            OutputDirectory =
                configuredDirectory;
        }


        ThumbnailEnabled =
            _settings.ThumbnailEnabled;


        SubtitleEnabled =
            _settings.SubtitleEnabled;


        AutoGeneratedSubtitleEnabled =
            _settings.AutoGeneratedSubtitleEnabled;


        PlaylistFolderEnabled =
            _settings.PlaylistFolderEnabled;


        FileNameTemplate =
            string.IsNullOrWhiteSpace(
                _settings.FileNameTemplate)
                ? "%(title)s.%(ext)s"
                : _settings.FileNameTemplate;
    }


    // ==================================================
    // 設定値正規化
    // ==================================================

    private string NormalizeQuality(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "最高";


        if (QualityOptions.Contains(value))
            return value;


        return "最高";
    }


    private string NormalizeAudioQuality(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "最高";


        if (AudioQualityOptions.Contains(value))
            return value;


        return "最高";
    }


    private string NormalizeFormat(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "MP4";


        // ==================================================
        // ★ バグ修正
        //
        // 以前はToUpperInvariant()した値を
        // FormatOptions.Contains()で（大文字小文字を
        // 区別して）比較していたため、"WebM"のように
        // 大文字小文字が混在する項目だけ一致せず、
        // 常にMP4へフォールバックしてしまっていた。
        //
        // FormatOptions側の正しい表記（大文字小文字含む）を
        // そのまま返すようにする。
        // ==================================================

        foreach (string option in FormatOptions)
        {
            if (string.Equals(
                    option,
                    value,
                    StringComparison.OrdinalIgnoreCase))
            {
                return option;
            }
        }


        return "MP4";
    }


    // ==================================================
    // 動画情報取得
    // ==================================================

    private async Task GetInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(
                Url))
        {
            Status =
                "URLを入力してください。";

            return;
        }


        try
        {
            ErrorMessage =
                "";


            Status =
                "動画情報を取得中...";


            VideoInfo =
                await _ytDlpService
                    .GetVideoInfoAsync(
                        Url.Trim());


            if (VideoInfo == null)
            {
                Status =
                    "動画情報を取得できませんでした。";

                return;
            }


            Status =
                "動画情報を取得しました。";
        }
        catch (Exception ex)
        {
            VideoInfo =
                null;


            ErrorMessage =
                ex.Message;


            Status =
                $"情報取得エラー: {ex.Message}";
        }


        RaiseCommandStates();
    }


    // ==================================================
    // キュー追加
    //
    // 実処理はQueueManagerへ委譲する
    // （通常URL/プレイリストの判定・展開もQueueManager側）
    // ==================================================

    private async Task AddToQueueAsync()
    {
        string url =
            Url.Trim();


        if (string.IsNullOrWhiteSpace(
                url))
        {
            return;
        }


        VideoInfo =
            await _queueManager
                .AddToQueueAsync(
                    url,
                    BuildAddOptions());


        Url =
            "";


        RaiseCommandStates();
    }


    // ==================================================
    // キュー追加設定の組み立て
    //
    // UIで選択中の画質・音質・形式などを
    // QueueManagerへ渡すためのオプションにまとめる。
    // ==================================================

    private QueueAddOptions BuildAddOptions()
    {
        return new QueueAddOptions
        {
            Quality =
                SelectedQuality,

            AudioQuality =
                SelectedAudioQuality,

            Format =
                SelectedFormat,

            PlaylistFolderEnabled =
                PlaylistFolderEnabled,

            FileNameTemplate =
                FileNameTemplate
        };
    }




    // ==================================================
    // キュー開始
    //
    // 実処理はQueueManagerへ委譲する
    // ==================================================

    private async Task StartQueueAsync()
    {
        await _queueManager
            .StartQueueAsync(
                BuildJobOptions());

        RaiseCommandStates();
    }


    // ==================================================
    // キュー処理設定の組み立て
    // ==================================================

    private DownloadJobOptions BuildJobOptions()
    {
        return new DownloadJobOptions
        {
            OutputDirectory =
                OutputDirectory,

            ThumbnailEnabled =
                ThumbnailEnabled,

            SubtitleEnabled =
                SubtitleEnabled,

            AutoGeneratedSubtitleEnabled =
                AutoGeneratedSubtitleEnabled
        };
    }




    // ==================================================
    // キャンセル
    //
    // 実処理はQueueManagerへ委譲する
    // ==================================================

    private void CancelQueue()
    {
        _queueManager.CancelQueue();
    }


    // ==================================================
    // 再試行
    //
    // 実処理はQueueManagerへ委譲する
    // ==================================================

    private void RetryQueueItem(
        object? parameter)
    {
        _queueManager.RetryQueueItem(
            parameter as DownloadQueueItem);

        RaiseCommandStates();
    }




    // ==================================================
    // 保存先選択
    // ==================================================

    private async Task BrowseAsync()
    {
        try
        {
            TopLevel? topLevel =
                Application.Current?
                    .ApplicationLifetime
                    is Avalonia.Controls
                        .ApplicationLifetimes
                        .IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;


            if (topLevel == null)
            {
                return;
            }


            IReadOnlyList<IStorageFolder>
                folders =
                    await topLevel
                        .StorageProvider
                        .OpenFolderPickerAsync(
                            new FolderPickerOpenOptions
                            {
                                Title =
                                    "保存先フォルダーを選択",

                                AllowMultiple =
                                    false
                            });


            if (folders.Count == 0)
            {
                return;
            }


            string? path =
                folders[0]
                    .TryGetLocalPath();


            if (!string.IsNullOrWhiteSpace(
                    path))
            {
                OutputDirectory =
                    path;


                Status =
                    "保存先を変更しました。";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;


            Status =
                $"保存先選択エラー: {ex.Message}";
        }
    }


    // ==================================================
    // キュー全削除
    //
    // 実処理はQueueManagerへ委譲する
    // ==================================================

    private void ClearQueue()
    {
        _queueManager.ClearQueue();

        RaiseCommandStates();
    }


    // ==================================================
    // キュー項目削除
    //
    // 実処理はQueueManagerへ委譲する
    // ==================================================

    private void RemoveQueueItem(
        object? parameter)
    {
        _queueManager.RemoveQueueItem(
            parameter as DownloadQueueItem);

        RaiseCommandStates();
    }




    // ==================================================
    // エラーコピー
    // ==================================================

    private async Task CopyErrorAsync()
    {
        if (!HasError)
        {
            return;
        }


        try
        {
            var desktop =
                Application.Current?
                    .ApplicationLifetime
                    as Avalonia.Controls
                        .ApplicationLifetimes
                        .IClassicDesktopStyleApplicationLifetime;


            var window =
                desktop?.MainWindow;


            if (window?.Clipboard == null)
            {
                Status =
                    "クリップボードを取得できませんでした。";

                return;
            }


            await window.Clipboard
                .SetTextAsync(
                    ErrorMessage);


            Status =
                "エラー内容をコピーしました。";
        }
        catch (Exception ex)
        {
            Status =
                $"コピーに失敗しました: {ex.Message}";
        }
    }


    // ==================================================
    // コマンド状態更新
    // ==================================================

    private void RaiseCommandStates()
    {
        if (GetInfoCommand
            is RelayCommand getInfo)
        {
            getInfo.RaiseCanExecuteChanged();
        }


        if (AddToQueueCommand
            is RelayCommand add)
        {
            add.RaiseCanExecuteChanged();
        }


        if (DownloadCommand
            is RelayCommand download)
        {
            download.RaiseCanExecuteChanged();
        }


        if (StartQueueCommand
            is RelayCommand start)
        {
            start.RaiseCanExecuteChanged();
        }


        if (CancelCommand
            is RelayCommand cancel)
        {
            cancel.RaiseCanExecuteChanged();
        }


        if (CancelQueueCommand
            is RelayCommand cancelQueue)
        {
            cancelQueue.RaiseCanExecuteChanged();
        }


        if (BrowseCommand
            is RelayCommand browse)
        {
            browse.RaiseCanExecuteChanged();
        }


        if (ClearQueueCommand
            is RelayCommand clear)
        {
            clear.RaiseCanExecuteChanged();
        }


        if (RemoveQueueItemCommand
            is RelayCommand remove)
        {
            remove.RaiseCanExecuteChanged();
        }


        if (RetryQueueItemCommand
            is RelayCommand retry)
        {
            retry.RaiseCanExecuteChanged();
        }


        if (CopyErrorCommand
            is RelayCommand copy)
        {
            copy.RaiseCanExecuteChanged();
        }


        if (SettingsCommand
            is RelayCommand settings)
        {
            settings.RaiseCanExecuteChanged();
        }
    }


    // ==================================================
    // PropertyChanged
    // ==================================================

    public event PropertyChangedEventHandler?
        PropertyChanged;


    protected void OnPropertyChanged(
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