﻿#region

using System.Linq.Expressions;
using System.Windows.Input;

using ReactiveUI;

using Tefin.Core;
using Tefin.Utils;

#endregion

namespace Tefin.ViewModels;

public class ViewModelBase : ReactiveObject, IDisposable {
    private readonly List<IDisposable> _disposables = new();
    private bool _isBusy;

    public IOResolver Io { get; } = Resolver.value;

    public bool IsBusy {
        get => this._isBusy;
        set => this.RaiseAndSetIfChanged(ref this._isBusy, value);
    }

    public virtual void Dispose() {
        foreach (var d in this._disposables)
            d?.Dispose();
    }

    public void SubscribeTo<R>(Expression<Func<ViewModelBase, R>> prop, Action<ViewModelBase> onChanged) {
        var b = (MemberExpression)prop.Body;
        this.Subscribe(b.Member.Name, onChanged).Then(this.MarkForCleanup);
    }

    protected ICommand CreateCommand(Func<Task> doThis) {
        return ReactiveCommand.Create(async () => {
            try {
                await doThis();
            }
            catch (Exception exc) {
                Io.Log.Error(exc);
            }
        });
    }

    protected ICommand CreateCommand(Action doThis) {
        return ReactiveCommand.Create(() => {
            try {
                doThis();
            }
            catch (Exception exc) {
                Io.Log.Error(exc);
            }
        });
    }

    protected void MarkForCleanup(IDisposable d) {
        this._disposables.Add(d);
    }
}