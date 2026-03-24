using Avalonia.Controls;
using Grimoire.Desktop.ViewModels;

namespace Grimoire.Desktop.Views;

public partial class GameLibraryView : UserControl
{
    public GameLibraryView()
    {
        InitializeComponent();
    }

    protected override async void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is GameLibraryViewModel vm)
        {
            await vm.LoadGamesCommand.ExecuteAsync(null);
        }
    }
}
