using CommunityToolkit.Mvvm.ComponentModel;

namespace CarryIQ.App;

public sealed partial class WedgeMatrixRowEditorViewModel : ObservableObject
{
    public WedgeMatrixRowEditorViewModel(Guid clubId, string clubName, ClubType clubType, bool isActive)
    {
        ClubId = clubId;
        ClubName = clubName;
        ClubType = clubType;
        IsActive = isActive;
        A1 = new WedgeMatrixCellEditorViewModel(WedgeSetupType.A1);
        A2 = new WedgeMatrixCellEditorViewModel(WedgeSetupType.A2);
        A3 = new WedgeMatrixCellEditorViewModel(WedgeSetupType.A3);
    }

    public Guid ClubId { get; }

    public string ClubName { get; }

    public ClubType ClubType { get; }

    public bool IsActive { get; }

    public WedgeMatrixCellEditorViewModel A1 { get; }

    public WedgeMatrixCellEditorViewModel A2 { get; }

    public WedgeMatrixCellEditorViewModel A3 { get; }

    public string StatusText => IsActive ? "Active" : "Inactive";

    public void Load(IReadOnlyList<WedgeSwingReference> references)
    {
        A1.Load(FindReference(references, WedgeSetupType.A1));
        A2.Load(FindReference(references, WedgeSetupType.A2));
        A3.Load(FindReference(references, WedgeSetupType.A3));
    }

    public IReadOnlyList<WedgeSwingReference> ToReferences(Guid golferProfileId)
    {
        var results = new List<WedgeSwingReference>();

        AddReference(results, A1, golferProfileId);
        AddReference(results, A2, golferProfileId);
        AddReference(results, A3, golferProfileId);

        return results;
    }

    private WedgeSwingReference? FindReference(IReadOnlyList<WedgeSwingReference> references, WedgeSetupType setupType)
    {
        var setupLabel = setupType.ToString();
        return references.FirstOrDefault(reference =>
            string.Equals(reference.SwingLabel, setupLabel, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(reference.ClockPosition, setupLabel, StringComparison.OrdinalIgnoreCase));
    }

    private void AddReference(List<WedgeSwingReference> results, WedgeMatrixCellEditorViewModel editor, Guid golferProfileId)
    {
        var reference = editor.ToReference(golferProfileId, ClubId);
        if (reference is not null)
        {
            results.Add(reference);
        }
    }
}
