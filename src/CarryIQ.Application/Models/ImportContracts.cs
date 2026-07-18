namespace CarryIQ.Application;

public sealed record ImportFileContext(string FilePath, IReadOnlyList<string> Headers)
{
    public string FileName => Path.GetFileName(FilePath);

    public Stream OpenRead() => File.OpenRead(FilePath);
}

public sealed record ImportPreview(
    IReadOnlyList<ImportPreviewRow> Rows,
    IReadOnlyList<string> UnsupportedColumns,
    IReadOnlyList<string> Warnings);

public sealed record ImportPreviewRow(
    int RowNumber,
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyList<string> Issues);

public sealed record ImportRequest(
    ImportFileContext File,
    IReadOnlyDictionary<string, string> ColumnMappings,
    Guid? PracticeSessionId = null);

public sealed record ImportResult(
    int RowsRead,
    int RowsImported,
    int RowsSkipped,
    int RowsFailed,
    int DuplicateRows,
    IReadOnlyList<ImportIssue> Issues,
    Guid? PracticeSessionId = null);

public sealed record ImportIssue(
    int RowNumber,
    string FieldName,
    string Message);
