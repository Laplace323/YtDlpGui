using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using YtDlpGui.ViewModels;
using YtDlpGui.Views;

namespace YtDlpGui.Services;

// ==================================================
// Windows(デスクトップ)向けナビゲーション実装
//
// 現状唯一の実装。Window.Contentの差し替えという
// デスクトップ固有の処理は、すべてこのクラスの中に
// 閉じ込める。
//
// Android版を作る際は、この実装だけを
// Android向けのものに置き換えれば、
// MainViewModel / SettingsViewModel は
// 一切変更不要になる想定。
// ==================================================

public class DesktopNavigationService : INavigationService
{
    // ==================================================
    // メイン画面表示
    //
    // 重要：ここでは絶対に new MainViewModel() しない。
    // 渡された既存インスタンス(owner)をそのまま使う。
    // これにより、設定画面から戻ってきても
    // ダウンロードキューの状態がそのまま保たれる。
    // ==================================================

    public void ShowMain(
        MainViewModel owner)
    {
        Window? window =
            GetMainWindow();

        if (window == null)
        {
            return;
        }

        window.Content =
            new MainView
            {
                DataContext =
                    owner
            };
    }


    // ==================================================
    // 設定画面表示
    // ==================================================

    public void ShowSettings(
        MainViewModel owner)
    {
        Window? window =
            GetMainWindow();

        if (window == null)
        {
            return;
        }

        var settingsViewModel =
            new SettingsViewModel(
                owner.ToolManager,
                this,
                owner);

        window.Content =
            new SettingsView
            {
                DataContext =
                    settingsViewModel
            };
    }


    // ==================================================
    // メインウィンドウ取得
    // ==================================================

    private static Window? GetMainWindow()
    {
        return Application.Current
                ?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }
}
