using System.Windows;
using System.Windows.Controls;

namespace CarryIQ.App;

public partial class ClubManagerView : UserControl
{
    public ClubManagerView()
    {
        InitializeComponent();
    }

    private async void ClubsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ClubManagerViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedClub?.Id is Guid clubId)
        {
            await viewModel.SelectClubAsync(clubId);
        }
    }
}
