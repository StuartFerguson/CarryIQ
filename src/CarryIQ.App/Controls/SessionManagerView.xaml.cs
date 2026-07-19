using System.Windows;
using System.Windows.Controls;

namespace CarryIQ.App;

public partial class SessionManagerView : UserControl
{
    public SessionManagerView()
    {
        InitializeComponent();
    }

    private async void SessionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not SessionManagerViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedSession?.Id is Guid sessionId)
        {
            await viewModel.SelectSessionAsync(sessionId);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SessionManagerViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedSession is null)
        {
            return;
        }

        var confirmed = MessageBox.Show(
            $"Delete '{viewModel.SelectedSession.Name}' and its shots?",
            "Delete session",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmed != MessageBoxResult.Yes)
        {
            return;
        }

        await viewModel.DeleteCommand.ExecuteAsync(null);
    }
}
