using YtDlpGui.ViewModels;

namespace YtDlpGui.Services;

// ==================================================
// ナビゲーションサービス
//
// これまでMainViewModel/SettingsViewModelが
// それぞれWindow.Contentを直接書き換えていたが、
// それをここへ一本化する。
//
// 目的：
// 1. ViewModelがView(Views.MainView等)を直接
//    newしないようにする（MVVM違反の解消）
// 2. 設定画面へ行って戻ってきても、既存の
//    MainViewModel（＝ダウンロードキュー）を
//    破棄せず再利用する
// 3. Windows依存のWindow操作をこのクラス1つに
//    閉じ込め、将来Android版を作る際は
//    このインターフェースだけ別実装に差し替えれば
//    ViewModel側は一切変更不要にする
// ==================================================

public interface INavigationService
{
    // ==================================================
    // メイン画面を表示する。
    //
    // owner: 表示するMainViewModelのインスタンス。
    //        必ず「既存のインスタンス」を渡すこと。
    //        ここで新しいMainViewModelを作ってはいけない
    //        （ダウンロードキューが消えるため）。
    // ==================================================

    void ShowMain(
        MainViewModel owner);


    // ==================================================
    // 設定画面を表示する。
    //
    // owner: 設定画面を呼び出した側のMainViewModel。
    //        設定画面から戻る際にこのインスタンスへ
    //        設定を反映しなおす。
    // ==================================================

    void ShowSettings(
        MainViewModel owner);
}
