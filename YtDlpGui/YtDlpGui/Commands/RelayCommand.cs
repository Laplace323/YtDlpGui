using System;
using System.Windows.Input;

namespace YtDlpGui.Commands;

// ======================================================
// RelayCommand
//
// ★ 分割リファクタリング
//
// 以前はMainViewModel.csの末尾に同居していたが、
// MainViewModel/SettingsViewModel双方から使われる
// 汎用インフラなので、独立したファイルへ移動した。
// 中身はそのまま（挙動の変更なし）。
// ======================================================

public class RelayCommand :
    ICommand
{
    private readonly Action<object?> _execute;

    private readonly Predicate<object?>?
        _canExecute;


    public RelayCommand(
        Action<object?> execute,
        Predicate<object?>? canExecute = null)
    {
        _execute =
            execute
            ?? throw new ArgumentNullException(
                nameof(execute));

        _canExecute =
            canExecute;
    }


    public bool CanExecute(
        object? parameter)
    {
        return _canExecute == null ||
               _canExecute(parameter);
    }


    public void Execute(
        object? parameter)
    {
        _execute(parameter);
    }


    public event EventHandler?
        CanExecuteChanged;


    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?
            .Invoke(
                this,
                EventArgs.Empty);
    }
}


// ======================================================
// CommandManager
// ======================================================

public static class CommandManager
{
    public static void
        RaiseRequerySuggested()
    {
        // AvaloniaではWPFの
        // CommandManagerを使用しない。
    }
}
