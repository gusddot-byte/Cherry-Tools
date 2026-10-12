namespace EulerSSTools;

internal enum ToolGroup { Programs, Commands }
internal enum ToolKind { Executable, PowerShellScript }

internal sealed record ToolInfo(
    string Id,
    string Title,
    string Description,
    string Category,
    ToolGroup Group,
    ToolKind Kind,
    string SourceUrl,
    string? DownloadUrl = null,
    string? DownloadFileName = null,
    string? GitBlobSha1 = null);

internal static class ToolCatalog
{
    // Los scripts/ejecutables descargables se fijan a commits concretos de GitHub.
    // GitBlobSha1 es la huella del objeto Git del archivo para rechazar cambios inesperados.
    internal static readonly ToolInfo[] All =
    [
        new("journaltrace", "JournalTrace", "Diario USN y actividad de archivos NTFS.", "Forense",
            ToolGroup.Programs, ToolKind.Executable, "https://github.com/spokwn/JournalTrace/releases"),
        new("systeminformer", "System Informer Canary", "Procesos, servicios, módulos y memoria.", "Sistema",
            ToolGroup.Programs, ToolKind.Executable, "https://www.systeminformer.com/canary"),
        new("winprefetch", "WinPrefetchView", "Información de ejecución almacenada en Prefetch.", "Windows",
            ToolGroup.Programs, ToolKind.Executable, "https://www.nirsoft.net/utils/win_prefetch_view.html"),
        new("everything", "Everything", "Búsqueda de archivos y carpetas en Windows.", "Archivos",
            ToolGroup.Programs, ToolKind.Executable, "https://www.voidtools.com/downloads/"),
        new("lastactivity", "LastActivityView", "Historial de actividades y eventos del sistema.", "Registros",
            ToolGroup.Programs, ToolKind.Executable, "https://www.nirsoft.net/utils/computer_activity_view.html"),
        new("regscanner", "RegScanner", "Búsqueda de claves y valores del registro.", "Registro",
            ToolGroup.Programs, ToolKind.Executable, "https://www.nirsoft.net/utils/regscanner.html"),
        new("cachedprograms", "CachedProgramsList", "Referencias de programas en el registro.", "Registro",
            ToolGroup.Programs, ToolKind.Executable, "https://github.com/ponei/CachedProgramsList/releases"),

        new("meownovo", "Meow Novoware Fucker", "Detector de Novoware: memoria y diario NTFS.", "CMD · EXE · ADMIN",
            ToolGroup.Commands, ToolKind.Executable, "https://github.com/MeowTonynoh/MeowNovowareFucker",
            "https://raw.githubusercontent.com/MeowTonynoh/MeowNovowareFucker/0c8278dd65aa7d555df8dc0d4e27d23a703a62a9/MeowNovowareFucker.exe",
            "MeowNovowareFucker.exe", "ec5c0c18deae4866cb5507b8dcfdb9240d573c1c"),
        new("meowdoom", "Meow Doomsday Fucker", "Detector de Doomsday: análisis de memoria.", "CMD · EXE · ADMIN",
            ToolGroup.Commands, ToolKind.Executable, "https://github.com/MeowTonynoh/MeowDoomsdayFucker",
            "https://raw.githubusercontent.com/MeowTonynoh/MeowDoomsdayFucker/6c13d91278ff877567a4939b06103664bfaf6fc4/MeowDoomsdayFucker.exe",
            "MeowDoomsdayFucker.exe", "f0162169e6f134742ae25d7598d2c0a6bf54e8f1"),
        // Este repositorio no incluye un .exe publicable. Se vincula manualmente.
        new("meowimports", "Meow Imports Checker", "Inspección de imports y cadenas en archivos.", "CMD · EXE · ADMIN",
            ToolGroup.Commands, ToolKind.Executable, "https://github.com/MeowTonynoh/MeowImportsChecker"),
        new("meowmods", "Meow Mod Analyzer", "Análisis de archivos .jar mediante PowerShell.", "CMD · PS1 · ADMIN",
            ToolGroup.Commands, ToolKind.PowerShellScript, "https://github.com/MeowTonynoh/MeowModAnalyzer",
            "https://raw.githubusercontent.com/MeowTonynoh/MeowModAnalyzer/5221bfd6ae78b9dd1d3ce1057ee46cc83da3deff/MeowModAnalyzer.ps1",
            "MeowModAnalyzer.ps1", "2a8528163f0ffaad5173c9b5449b9536a4902507"),
        new("lilithservices", "Lilith Services", "Comprobación de servicios, eventos y Prefetch.", "CMD · PS1 · ADMIN",
            ToolGroup.Commands, ToolKind.PowerShellScript, "https://github.com/praiselily/lilith-ps",
            "https://raw.githubusercontent.com/praiselily/lilith-ps/ecf7280a1239f9a14560f07c9e6925715c2d74e3/Services.ps1",
            "LilithServices.ps1", "bafbec239bf6b1dc195a92d286448f564c7ae45b")
    ];
}
