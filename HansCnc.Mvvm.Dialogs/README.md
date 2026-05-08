# HansCnc.Mvvm.Dialogs

Standalone WPF dialog service for MVVM applications using Autofac, Serilog, CommunityToolkit.Mvvm, and R3.

## Features

- Modal dialogs with `ShowDialog(...)`.
- Modeless dialogs with `Show(...)` and completion callbacks.
- Strongly typed input and result payloads.
- `RequestClosed` flow from view model to window.
- WPF lifecycle hooks: initialized, loaded, closing, closed.
- Autofac registrations for dialog service and typed dialog views.

## Project

```text
HansCnc.Mvvm.Dialogs/
  HansCnc.Mvvm.Dialogs.slnx
  src/HansCnc.Mvvm.Dialogs/HansCnc.Mvvm.Dialogs.csproj
```

The library targets `net8.0-windows` with WPF enabled.

## Register services

```csharp
using Autofac;
using HansCnc.Mvvm;
using Serilog;

var builder = new ContainerBuilder();

builder.RegisterInstance(Log.Logger).As<ILogger>();
builder.RegisterDialogService();
builder.RegisterType<UserEditorViewModel>();
builder.RegisterDialogView<UserEditorWindow, UserEditorViewModel>();
```

## Create a dialog view

```csharp
public partial class UserEditorWindow : DialogWindowFor<UserEditorViewModel>
{
    public UserEditorWindow()
    {
        InitializeComponent();
    }
}
```

If a window cannot inherit from `DialogWindowFor<TViewModel>`, implement `IViewFor<TViewModel>` manually and forward `ViewModel` to `DataContext`.

## Create a dialog view model

```csharp
using HansCnc.Mvvm;

public sealed class UserEditorViewModel
    : DialogViewModelBase<UserEditorInput, UserEditorResult>
{
    public void Save()
    {
        CloseWithResult(new UserEditorResult(Input.UserId));
    }

    public void Close()
    {
        Cancel();
    }
}
```

## Show dialogs

```csharp
RxDialogResult<UserEditorResult> result =
    dialogService.ShowDialog<UserEditorViewModel, UserEditorInput, UserEditorResult>(
        new UserEditorInput(userId));

if (result.Result)
{
    // Use result.ResultValue.
}
```

```csharp
dialogService.Show<UserEditorViewModel, UserEditorInput, UserEditorResult>(
    new UserEditorInput(userId),
    result =>
    {
        if (result.Result)
        {
            // Use result.ResultValue after the modeless window closes.
        }
    });
```

For dialogs without input or payload, use `R3.Unit` through the convenience overloads:

```csharp
bool accepted = dialogService.ShowDialog<AboutViewModel>();
dialogService.Show<AboutViewModel>(accepted => { });
```
