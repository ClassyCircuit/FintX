#region

using System.Collections.ObjectModel;

using Avalonia.Threading;

using ReactiveUI;

using Tefin.Core.Infra.Actors;
using Tefin.Messages;

#endregion

namespace Tefin.ViewModels.Tabs;

public class TabHostViewModel : ViewModelBase {
    private ITabViewModel? _selectedItem;
   
    public TabHostViewModel() {
        GlobalHub.subscribe<OpenTabMessage>(this.OnReceiveTabOpenMessage);
        GlobalHub.subscribeTask<CloseTabMessage>(this.OnReceiveTabCloseMessage);
        GlobalHub.subscribeTask<FileChangeMessage>(this.OnReceiveFileChangeMessage);
    }

    public ObservableCollection<ITabViewModel> Items { get; } = new();
    public ITabViewModel? SelectedItem {
        get => this._selectedItem;
        set => this.RaiseAndSetIfChanged(ref this._selectedItem, value);
    }

    private async Task OnReceiveFileChangeMessage(FileChangeMessage msg) {
        if (msg.ChangeType == WatcherChangeTypes.Deleted) {
            var existingTab = this.Items.FirstOrDefault(t => t is PersistedTabViewModel && t.Id == msg.FullPath);
            await this.OnReceiveTabCloseMessage(new CloseTabMessage(existingTab));
        }

        if (msg.ChangeType == WatcherChangeTypes.Renamed) {
            var existingTab = this.Items.FirstOrDefault(t => t is PersistedTabViewModel && t.Id == msg.OldFullPath);
            if (existingTab is PersistedTabViewModel pt) {
                pt.UpdateTitle(msg.OldFullPath, msg.FullPath);
            }
        }
    }

    private async Task OnReceiveTabCloseMessage(CloseTabMessage obj) {
        await Dispatcher.UIThread.InvokeAsync(() => {
            if (this.Items.Count > 1) {
                if (this.SelectedItem == obj.Tab) {
                    this.SelectedItem = this.Items.Last();
                }
            }

            this.Items.Remove(obj.Tab);
        });
    }

    private void OnReceiveTabOpenMessage(OpenTabMessage obj) {
        obj.Tab.Init();        
        var existing = this.Items.FirstOrDefault(t => t.Id == obj.Tab.Id);
        if (existing != null) {
            this.SelectedItem = existing;
        }
        else {
            this.Items.Add(obj.Tab);
            this.SelectedItem = this.Items.Last();
        }


    }
}