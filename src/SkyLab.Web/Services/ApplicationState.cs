namespace SkyLab.Web.Services;

public sealed class ApplicationState
{
    public int Esercizio { get; set; } = DateTime.Today.Year;
}