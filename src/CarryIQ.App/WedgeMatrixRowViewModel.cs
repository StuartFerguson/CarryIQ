namespace CarryIQ.App;

public sealed class WedgeMatrixRowViewModel
{
    public WedgeMatrixRowViewModel(WedgeMatrixRow row)
    {
        ClubId = row.Club.Id;
        ClubName = row.Club.Name;
        ClubType = row.Club.ClubType;
        IsActive = row.Club.IsActive;
        SortOrder = row.Club.SortOrder;
        A1 = row.A1 is null ? WedgeMatrixCellViewModel.Missing(WedgeSetupType.A1) : WedgeMatrixCellViewModel.From(row.A1);
        A2 = row.A2 is null ? WedgeMatrixCellViewModel.Missing(WedgeSetupType.A2) : WedgeMatrixCellViewModel.From(row.A2);
        A3 = row.A3 is null ? WedgeMatrixCellViewModel.Missing(WedgeSetupType.A3) : WedgeMatrixCellViewModel.From(row.A3);
    }

    public Guid ClubId { get; }

    public string ClubName { get; }

    public ClubType ClubType { get; }

    public bool IsActive { get; }

    public int SortOrder { get; }

    public WedgeMatrixCellViewModel A1 { get; }

    public WedgeMatrixCellViewModel A2 { get; }

    public WedgeMatrixCellViewModel A3 { get; }

    public string StatusText => IsActive ? "Active" : "Inactive";
}
