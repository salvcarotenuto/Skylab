namespace SkyLab.Web.Services;

public sealed class SkyLabServicePaths
{
    private static readonly string[] ServiceFolderNames =
    [
        "FEAcquisti",
        "FEVendite",
        "Excel",
        "Pdf",
        "Titoli"
    ];

    public SkyLabServicePaths(IConfiguration configuration, SkyLab.Web.Data.SkyLabDatabaseOptions options)
    {
        CompanyKey = options.CompanyKey;
        Root = configuration["SkyLab:DataRoot"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SkyLab");
        CompanyRoot = Path.Combine(Root, "Aziende", CompanyKey);
        FEAcquisti = Path.Combine(CompanyRoot, "FEAcquisti");
        FEAcquistiTransito = Path.Combine(FEAcquisti, "Transito");
        FEAcquistiArchivio = Path.Combine(FEAcquisti, "Archivio");
        FEVendite = Path.Combine(CompanyRoot, "FEVendite");
        Excel = Path.Combine(CompanyRoot, "Excel");
        Pdf = Path.Combine(CompanyRoot, "Pdf");
        Titoli = Path.Combine(CompanyRoot, "Titoli");
    }

    public string CompanyKey { get; }
    public string Root { get; }
    public string CompanyRoot { get; }
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
        Directory.CreateDirectory(FEAcquisti);
        Directory.CreateDirectory(FEAcquistiTransito);
        Directory.CreateDirectory(FEAcquistiArchivio);
        Directory.CreateDirectory(FEVendite);
    }
}
