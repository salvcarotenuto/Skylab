namespace SkyLab.Web.Models;

public sealed record ArticleLineEditorModel(
    string Context,
    IReadOnlyList<CodeLookupItem> UnitMeasures,
    string Title = "Inserimento articolo");
