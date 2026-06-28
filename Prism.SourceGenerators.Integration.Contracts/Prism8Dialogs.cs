namespace Prism.Services.Dialogs
{
    public interface IDialogParameters
    {
        bool TryGetValue<T>(string key, out T value);
    }

    public interface IDialogResult
    {
        ButtonResult Result { get; }
    }

    public enum ButtonResult
    {
        None,
        OK,
        Cancel,
        Abort,
        Retry,
        Ignore,
        Yes,
        No,
    }

    public sealed class DialogResult : IDialogResult
    {
        public DialogResult(ButtonResult result) => Result = result;

        public ButtonResult Result { get; }
    }

    public interface IDialogAware
    {
        string Title { get; set; }
        event System.Action<IDialogResult> RequestClose;
        bool CanCloseDialog();
        void OnDialogClosed();
        void OnDialogOpened(IDialogParameters parameters);
    }

    public interface IDialogService
    {
        void ShowDialog(string name, IDialogParameters parameters, System.Action<IDialogResult> callback);
    }
}
