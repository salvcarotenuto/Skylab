namespace SkyLab.Web.Models;

public sealed record ArticleLineEditorModel(
    string Context,
    IReadOnlyList<CodeLookupItem> UnitMeasures,
    IReadOnlyList<VatCodeListItem> VatCodes,
    string Title = "Inserimento articolo");
