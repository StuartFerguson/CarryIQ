namespace CarryIQ.Application;

/// <summary>
/// Imports shots from a specific file format.
/// </summary>
public interface IShotFileImporter
{
    string Name { get; }

    bool CanImport(ImportFileContext context);

    Task<ImportPreview> PreviewAsync(
        ImportFileContext context,
        CancellationToken cancellationToken);

    Task<ImportResult> ImportAsync(
        ImportRequest request,
        CancellationToken cancellationToken);
}
