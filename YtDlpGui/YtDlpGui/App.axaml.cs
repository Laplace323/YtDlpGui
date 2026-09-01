using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using YtDlpGui.Tools;
using YtDlpGui.ViewModels;
using YtDlpGui.Views;

namespace YtDlpGui;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // ==================================================
        // ToolManagerを1つ生成
        // ==================================================

        var toolManager =
            new ToolManager();


        // ==================================================
        // MainViewModelを生成
        // ==================================================

        var mainViewModel =
            new MainViewModel(
                toolManager);


        // ==================================================
        // デスクトップ
        // ==================================================

        if (ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow =
                new MainWindow
                {
                    DataContext =
                        mainViewModel
                };
        }


        // ==================================================
        // Android / Mobile系
        // ==================================================

        else if (ApplicationLifetime
            is IActivityApplicationLifetime
                singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory =
                () =>
                    new MainView
                    {
                        DataContext =
                            new MainViewModel(
                                toolManager)
                    };
        }


        // ==================================================
        // Single View
        // ==================================================

        else if (ApplicationLifetime
            is ISingleViewApplicationLifetime
                singleViewPlatform)
        {
            singleViewPlatform.MainView =
                new MainView
                {
                    DataContext =
                        mainViewModel
                };
        }


        base.OnFrameworkInitializationCompleted();
    }
}