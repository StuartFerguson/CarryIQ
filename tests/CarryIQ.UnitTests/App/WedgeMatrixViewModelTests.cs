using System.Globalization;
using CarryIQ.App;

namespace CarryIQ.UnitTests.App;

public class WedgeMatrixViewModelTests
{
    [Fact]
    public async Task InitializeAsyncLoadsActiveWedgeRowsAndFormatsCells()
    {
        var golferProfileId = Guid.NewGuid();
        var activeClub = CreateClub(golferProfileId, "Gap Wedge", ClubType.GapWedge, 1, true);
        var inactiveClub = CreateClub(golferProfileId, "Lob Wedge", ClubType.LobWedge, 2, false);
        var references = new[]
        {
            CreateReference(golferProfileId, activeClub.Id, "A1", 58m, 55m, 2.2m, 7),
            CreateReference(golferProfileId, activeClub.Id, "A2", 62m, 59m, 2.4m, 6),
            CreateReference(golferProfileId, inactiveClub.Id, "A1", 82m, 79m, 4.1m, 4),
        };

        var viewModel = CreateViewModel(golferProfileId, [activeClub, inactiveClub], references);

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal("Wedge Matrix", viewModel.Title);
        Assert.Single(viewModel.Rows);
        Assert.Equal("Gap Wedge", viewModel.Rows[0].ClubName);
        Assert.Equal("Active", viewModel.Rows[0].StatusText);
        Assert.Equal("58 yd", viewModel.Rows[0].A1.TargetDistanceText);
        Assert.Equal("55 yd", viewModel.Rows[0].A1.AverageCarryText);
        Assert.Equal("n=7", viewModel.Rows[0].A1.SampleSizeText);
        Assert.Equal("2.2 yd", viewModel.Rows[0].A1.StandardDeviationText);
        Assert.Equal("N/A", viewModel.Rows[0].A3.TargetDistanceText);
        Assert.False(viewModel.IncludeInactive);
    }

    [Fact]
    public async Task InitializeAsyncMarksManualOverrideCellsInTheMatrix()
    {
        var golferProfileId = Guid.NewGuid();
        var club = CreateClub(golferProfileId, "Sand Wedge", ClubType.SandWedge, 1, true);
        var references = new[]
        {
            CreateReference(golferProfileId, club.Id, "A1", 56m, 53m, 1.8m, 9, isManualOverride: true),
        };

        var viewModel = CreateViewModel(golferProfileId, [club], references);

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.True(viewModel.Rows[0].A1.IsManualOverride);
        Assert.Equal("Manual override", viewModel.Rows[0].A1.OverrideText);
    }

    [Fact]
    public async Task IncludeInactiveReloadsAndIncludesInactiveWedgeRows()
    {
        var golferProfileId = Guid.NewGuid();
        var activeClub = CreateClub(golferProfileId, "Gap Wedge", ClubType.GapWedge, 1, true);
        var inactiveClub = CreateClub(golferProfileId, "Lob Wedge", ClubType.LobWedge, 2, false);
        var references = new[]
        {
            CreateReference(golferProfileId, activeClub.Id, "A1", 58m, 55m, 2.2m, 7),
            CreateReference(golferProfileId, inactiveClub.Id, "A1", 82m, 79m, 4.1m, 4),
        };

        var viewModel = CreateViewModel(golferProfileId, [activeClub, inactiveClub], references);

        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.IncludeInactive = true;
        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Rows.Count);
        Assert.Equal("Gap Wedge", viewModel.Rows[0].ClubName);
        Assert.Equal("Lob Wedge", viewModel.Rows[1].ClubName);
        Assert.Equal("Inactive", viewModel.Rows[1].StatusText);
        Assert.Equal("82 yd", viewModel.Rows[1].A1.TargetDistanceText);
    }

    [Fact]
    public async Task SaveCommandPersistsSelectedRowEdits()
    {
        var golferProfileId = Guid.NewGuid();
        var club = CreateClub(golferProfileId, "Gap Wedge", ClubType.GapWedge, 1, true);
        var repository = new MutableWedgeSwingReferenceRepository(
            [
                CreateReference(golferProfileId, club.Id, "A1", 58m, 55m, 2.2m, 7, isManualOverride: false),
            ]);
        var viewModel = CreateViewModel(golferProfileId, [club], repository);

        await viewModel.InitializeAsync(CancellationToken.None);
        viewModel.SelectedRow = viewModel.Rows[0];
        viewModel.SelectedRowEditor!.A1.TargetDistanceText = "60";
        viewModel.SelectedRowEditor.A1.AverageCarryText = "57";
        viewModel.SelectedRowEditor.A1.StandardDeviationText = "2.0";
        viewModel.SelectedRowEditor.A1.SampleSizeText = "8";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Wedge references saved.", viewModel.StatusMessage);
        Assert.Single(repository.SavedReferences);
        Assert.Equal("A1", repository.SavedReferences[0].SwingLabel);
        Assert.True(repository.SavedReferences[0].IsManualOverride);
        Assert.Equal(60m, repository.SavedReferences[0].TargetDistance!.Value.Yards);
        Assert.Equal("60 yd", viewModel.Rows[0].A1.TargetDistanceText);
        Assert.True(viewModel.Rows[0].A1.IsManualOverride);
    }

    private static WedgeMatrixViewModel CreateViewModel(
        Guid golferProfileId,
        IReadOnlyList<ClubSummary> clubs,
        IReadOnlyList<WedgeSwingReference> references)
    {
        return new WedgeMatrixViewModel(
            new TestClubRepository(clubs),
            new TestWedgeSwingReferenceRepository(references),
            golferProfileId: golferProfileId);
    }

    private static WedgeMatrixViewModel CreateViewModel(
        Guid golferProfileId,
        IReadOnlyList<ClubSummary> clubs,
        MutableWedgeSwingReferenceRepository repository)
    {
        return new WedgeMatrixViewModel(
            new TestClubRepository(clubs),
            repository,
            golferProfileId: golferProfileId);
    }

    private static ClubSummary CreateClub(Guid golferProfileId, string name, ClubType clubType, int sortOrder, bool isActive) =>
        new(Guid.NewGuid(), name, clubType, sortOrder, isActive);

    private static WedgeSwingReference CreateReference(
        Guid golferProfileId,
        Guid clubId,
        string setupLabel,
        decimal targetDistanceYards,
        decimal averageCarryYards,
        decimal standardDeviationYards,
        int sampleSize) =>
        CreateReference(golferProfileId, clubId, setupLabel, targetDistanceYards, averageCarryYards, standardDeviationYards, sampleSize, isManualOverride: false);

    private static WedgeSwingReference CreateReference(
        Guid golferProfileId,
        Guid clubId,
        string setupLabel,
        decimal targetDistanceYards,
        decimal averageCarryYards,
        decimal standardDeviationYards,
        int sampleSize,
        bool isManualOverride) =>
        new()
        {
            Id = Guid.NewGuid(),
            GolferProfileId = golferProfileId,
            ClubId = clubId,
            SwingLabel = setupLabel,
            SwingType = SwingType.Full,
            ClockPosition = null,
            TargetDistance = Distance.FromYards(targetDistanceYards),
            AverageCarry = Distance.FromYards(averageCarryYards),
            CarryStandardDeviation = Distance.FromYards(standardDeviationYards),
            SampleSize = sampleSize,
            IsManualOverride = isManualOverride,
            UpdatedAt = DateTimeOffset.Parse("2026-07-20T10:00:00Z", CultureInfo.InvariantCulture),
        };

    private sealed class TestClubRepository(IReadOnlyList<ClubSummary> clubs) : IClubRepository
    {
        public Task<Club?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Club?>(null);

        public Task<IReadOnlyList<ClubSummary>> SearchAsync(ClubSearchCriteria criteria, CancellationToken cancellationToken)
        {
            IEnumerable<ClubSummary> results = clubs;
            if (criteria.ActiveOnly is true)
            {
                results = results.Where(club => club.IsActive);
            }
            else if (criteria.ActiveOnly is false)
            {
                results = results.Where(club => !club.IsActive);
            }

            return Task.FromResult<IReadOnlyList<ClubSummary>>(results.ToList());
        }

        public Task SaveAsync(Club club, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestWedgeSwingReferenceRepository(IReadOnlyList<WedgeSwingReference> references) : IWedgeSwingReferenceRepository
    {
        public Task<IReadOnlyList<WedgeSwingReference>> SearchAsync(Guid golferProfileId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<WedgeSwingReference>>(
                references.Where(reference => reference.GolferProfileId == golferProfileId).ToList());
        }

        public Task SaveAsync(WedgeSwingReference reference, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class MutableWedgeSwingReferenceRepository(IReadOnlyList<WedgeSwingReference> references) : IWedgeSwingReferenceRepository
    {
        public List<WedgeSwingReference> References { get; } = references.ToList();

        public List<WedgeSwingReference> SavedReferences { get; } = [];

        public Task<IReadOnlyList<WedgeSwingReference>> SearchAsync(Guid golferProfileId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<WedgeSwingReference>>(
                References.Where(reference => reference.GolferProfileId == golferProfileId).ToList());
        }

        public Task SaveAsync(WedgeSwingReference reference, CancellationToken cancellationToken)
        {
            SavedReferences.Add(reference);

            var existingIndex = References.FindIndex(item => item.Id == reference.Id);
            if (existingIndex >= 0)
            {
                References[existingIndex] = reference;
            }
            else
            {
                References.Add(reference);
            }

            return Task.CompletedTask;
        }
    }
}
