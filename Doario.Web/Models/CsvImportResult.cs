namespace Doario.Web.Models;

public class CsvImportResult
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<CsvRowError> Errors { get; set; } = new();
}

public class CsvRowError
{
    public int Row { get; set; }
    public string Reason { get; set; }
}