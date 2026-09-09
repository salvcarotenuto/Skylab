namespace SkyLab.Web.Services;

public sealed class MicronoteServicePaths
{
    private static readonly string[] ServiceFolderNames =
    [
        "Documenti",
        "Excel",
        "Pdf",
        "Titoli"
    ];

    public MicronoteServicePaths(IConfiguration configuration)
    {
        CompanyKey = "0001";
        Root = configuration["SkyLab:DataRoot"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SkyLab");
        CompanyRoot = Path.Combine(Root, "Aziende", CompanyKey);
        Documenti = Path.Combine(CompanyRoot, "Documenti");
        FattureElettroniche = Path.Combine(Documenti, "FattureElettroniche");
        FEAcquisti = Path.Combine(FattureElettroniche, "Acquisti");
        FEAcquistiTransito = Path.Combine(FEAcquisti, "Transito");
        FEAcquistiArchivio = Path.Combine(FEAcquisti, "Archivio");
        FEVendite = Path.Combine(FattureElettroniche, "Vendite");
        Excel = Path.Combine(CompanyRoot, "Excel");
        Pdf = Path.Combine(CompanyRoot, "Pdf");
        Titoli = Path.Combine(CompanyRoot, "Titoli");
    }

    public string CompanyKey { get; }
    public string Root { get; }
    public string CompanyRoot { get; }
    public string Documenti { get; }
    public string FattureElettroniche { get; }
    public string FEAcquisti { get; }
    public string FEAcquistiTransito { get; }
    public string FEAcquistiArchivio { get; }
    public string FEVendite { get; }
    public string Excel { get; }
    public string Pdf { get; }
    public string Titoli { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(CompanyRoot);
        foreach (var folderName in ServiceFolderNames)
        {
            Directory.CreateDirectory(Path.Combine(CompanyRoot, folderName));
        }
        Directory.CreateDirectory(FattureElettroniche);
        Directory.CreateDirectory(FEAcquisti);
        Directory.CreateDirectory(FEAcquistiTransito);
        Directory.CreateDirectory(FEAcquistiArchivio);
        Directory.CreateDirectory(FEVendite);
    }
}
