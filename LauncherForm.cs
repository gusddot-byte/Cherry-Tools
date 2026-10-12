using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EulerSSTools;

public sealed class LauncherForm : Form
{
    private static readonly Color Background = Color.FromArgb(17, 16, 19);
    private static readonly Color CardColor = Color.FromArgb(28, 23, 28);
    private static readonly Color HeaderColor = Color.FromArgb(25, 22, 27);
    private static readonly Color ButtonColor = Color.FromArgb(42, 36, 43);
    private static readonly Color BorderColor = Color.FromArgb(57, 46, 56);
    private static readonly Color MainText = Color.FromArgb(241, 235, 239);
    private static readonly Color MutedText = Color.FromArgb(172, 156, 171);
    private static readonly Color Accent = Color.FromArgb(209, 104, 134);
    private static readonly Color AccentMuted = Color.FromArgb(60, 36, 47);

    private readonly string _dataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EulerSSTools");
    private readonly Dictionary<string, string> _paths = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _discovered = new(StringComparer.OrdinalIgnoreCase);

    private readonly BorderPanel _chrome = new(HeaderColor, BorderColor, 24);
    private readonly RoundedButton _programTab = new("Programas", false);
    private readonly RoundedButton _commandTab = new("CMD", false);
    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly Label _count = new();
    private readonly TextBox _search = new();
    private readonly Panel _viewport = new() { BackColor = Background, Dock = DockStyle.Fill, TabStop = true };
    private readonly Panel _cardHost = new() { BackColor = Background };
    private readonly Panel _scrollbar = new() { BackColor = Background, Dock = DockStyle.Right, Width = 14 };
    private readonly List<Control> _cards = [];
    private ToolGroup _page = ToolGroup.Programs;
    private int _scrollOffset;
    private int _contentHeight;
    private bool _draggingScroll;
    private int _dragStartY;
    private int _dragStartOffset;
    private bool _busy;
    private string? _runningId;

    public LauncherForm()
    {
        Text = "Cherrybox — Euler SS Tools";
        try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { /* Icono predeterminado. */ }
        BackColor = Background;
        ForeColor = MainText;
        Font = new Font("Segoe UI", 9.5f);
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(850, 650);
        Size = new Size(1420, 850);
        StartPosition = FormStartPosition.CenterScreen;
        LoadSettings();
        DiscoverTools();
        BuildShell();
        RenderPage();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Barra de título de Windows oscura (si la versión de Windows lo permite).
        try
        {
            int enabled = 1;
            DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int));
        }
        catch { /* En sistemas anteriores se conserva la barra del sistema. */ }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private void BuildShell()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            BackColor = Background, Margin = Padding.Empty, Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        Controls.Add(root);

        var top = new Panel { Dock = DockStyle.Fill, BackColor = Background };
        root.Controls.Add(top, 0, 0);
        top.Controls.Add(_chrome);
        _chrome.Location = new Point(22, 14);
        _chrome.Size = new Size(1200, 70);
        top.Resize += (_, _) => { _chrome.Width = Math.Max(200, top.ClientSize.Width - 44); ArrangeChrome(); };

        var logo = new BorderPanel(AccentMuted, AccentMuted, 14)
        {
            Location = new Point(17, 13), Size = new Size(44, 44)
        };
        logo.Controls.Add(new Label
        {
            Text = "CB", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Accent, Font = new Font("Segoe UI", 13, FontStyle.Bold)
        });
        _chrome.Controls.Add(logo);
        _chrome.Controls.Add(new Label
        {
            Text = "Cherrybox", Location = new Point(72, 16), Size = new Size(180, 21),
            Font = new Font("Segoe UI Semibold", 11.8f, FontStyle.Bold), ForeColor = MainText
        });
        _chrome.Controls.Add(new Label
        {
            Text = "Euler SS Tools", Location = new Point(73, 39), Size = new Size(180, 17),
            Font = new Font("Segoe UI", 8.1f), ForeColor = MutedText
        });
        _programTab.Size = new Size(117, 35);
        _commandTab.Size = new Size(92, 35);
        _programTab.Click += (_, _) => SwitchPage(ToolGroup.Programs);
        _commandTab.Click += (_, _) => SwitchPage(ToolGroup.Commands);
        _chrome.Controls.Add(_programTab);
        _chrome.Controls.Add(_commandTab);
        var version = new Label
        {
            Name = "VersionLabel", Text = "ScreenShare  /  2.0", Size = new Size(154, 23),
            ForeColor = MutedText, TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 8.5f)
        };
        _chrome.Controls.Add(version);
        _chrome.Resize += (_, _) => ArrangeChrome();

        var section = new Panel { Dock = DockStyle.Fill, BackColor = Background };
        root.Controls.Add(section, 0, 1);
        _title.Location = new Point(31, 13);
        _title.Size = new Size(600, 41);
        _title.Font = new Font("Segoe UI Semibold", 22, FontStyle.Bold);
        _title.ForeColor = MainText;
        section.Controls.Add(_title);
        _subtitle.Location = new Point(32, 55);
        _subtitle.Size = new Size(850, 23);
        _subtitle.ForeColor = MutedText;
        section.Controls.Add(_subtitle);
        _count.Location = new Point(32, 96);
        _count.Size = new Size(280, 19);
        _count.Font = new Font("Segoe UI", 8.7f);
        _count.ForeColor = MutedText;
        section.Controls.Add(_count);
        _search.PlaceholderText = "Buscar herramienta...";
        _search.BackColor = HeaderColor;
        _search.ForeColor = MainText;
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.Size = new Size(245, 29);
        _search.Location = new Point(850, 86);
        _search.TextChanged += (_, _) => { _scrollOffset = 0; RenderPage(); };
        section.Controls.Add(_search);
        section.Resize += (_, _) => _search.Left = Math.Max(350, section.ClientSize.Width - _search.Width - 30);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Background };
        root.Controls.Add(footer, 0, 3);
        footer.Paint += (_, e) => { using var p = new Pen(BorderColor); e.Graphics.DrawLine(p, 22, 0, footer.Width - 22, 0); };
        footer.Controls.Add(new Label
        {
            Text = "Cherrybox · ScreenShare", Location = new Point(28, 13), Size = new Size(280, 18),
            ForeColor = MutedText, Font = new Font("Segoe UI", 8.5f)
        });
        var credits = new Label
        {
            Text = "Hecho por Sr. Agustín", Size = new Size(240, 18),
            TextAlign = ContentAlignment.MiddleRight, ForeColor = MainText,
            Font = new Font("Segoe UI", 8.5f)
        };
        footer.Controls.Add(credits);
        footer.Resize += (_, _) => credits.Location = new Point(footer.ClientSize.Width - credits.Width - 28, 13);

        root.Controls.Add(_viewport, 0, 2);
        _viewport.Controls.Add(_cardHost);
        _viewport.Controls.Add(_scrollbar);
        _viewport.Resize += (_, _) => LayoutCards();
        _viewport.MouseWheel += ScrollWheel;
        _cardHost.MouseWheel += ScrollWheel;
        _scrollbar.Paint += ScrollbarPaint;
        _scrollbar.MouseDown += ScrollbarMouseDown;
        _scrollbar.MouseMove += ScrollbarMouseMove;
        _scrollbar.MouseUp += (_, _) => _draggingScroll = false;
        _scrollbar.MouseWheel += ScrollWheel;
        ArrangeChrome();
    }

    private void ArrangeChrome()
    {
        int width = _chrome.ClientSize.Width;
        int left = (width - 217) / 2;
        _programTab.Location = new Point(left, 17);
        _commandTab.Location = new Point(left + 123, 17);
        if (_chrome.Controls["VersionLabel"] is Control version)
            version.Location = new Point(width - version.Width - 18, 24);
    }

    private void SwitchPage(ToolGroup page)
    {
        _page = page;
        _search.Text = "";
        _scrollOffset = 0;
        RenderPage();
    }

    private void RenderPage()
    {
        _title.Text = _page == ToolGroup.Programs ? "Programas" : "CMD";
        _subtitle.Text = _page == ToolGroup.Programs
            ? "Aplicaciones de análisis y consulta para ScreenShare."
            : "Scripts y utilidades · ejecución desde CMD con permisos de administrador.";
        _programTab.SetSelected(_page == ToolGroup.Programs);
        _commandTab.SetSelected(_page == ToolGroup.Commands);

        string search = _search.Text.Trim();
        var matching = ToolCatalog.All.Where(t => t.Group == _page &&
            (search.Length == 0 || t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
             t.Description.Contains(search, StringComparison.OrdinalIgnoreCase))).ToArray();
        _count.Text = $"{matching.Length} herramienta{(matching.Length == 1 ? "" : "s")}";

        _cardHost.SuspendLayout();
        try
        {
            foreach (var control in _cards) control.Dispose();
            _cards.Clear();
            _cardHost.Controls.Clear();
            foreach (ToolInfo tool in matching)
            {
                Control card = CreateCard(tool);
                _cards.Add(card);
                _cardHost.Controls.Add(card);
            }
            if (matching.Length == 0)
            {
                var empty = new Label { Text = "No se encontraron herramientas.", ForeColor = MutedText,
                    Font = new Font("Segoe UI", 11), AutoSize = true };
                _cards.Add(empty);
                _cardHost.Controls.Add(empty);
            }
        }
        finally { _cardHost.ResumeLayout(); }
        LayoutCards();
    }

    private Control CreateCard(ToolInfo t)
    {
        var card = new BorderPanel(CardColor, BorderColor, 19) { Size = new Size(380, 223) };
        var glyph = new BorderPanel(AccentMuted, AccentMuted, 12)
        {
            Location = new Point(18, 17), Size = new Size(41, 41)
        };
        glyph.Controls.Add(new Label
        {
            Text = t.Group == ToolGroup.Commands ? ">_" : GetGlyph(t.Id), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter, ForeColor = Accent,
            Font = new Font("Consolas", 12.5f, FontStyle.Bold)
        });
        card.Controls.Add(glyph);
        card.Controls.Add(new Label
        {
            Text = t.Category, Location = new Point(72, 24), Size = new Size(290, 25),
            ForeColor = MutedText, Font = new Font("Segoe UI", 8.8f)
        });
        var name = new Label
        {
            Text = t.Title, Location = new Point(19, 72), Size = new Size(340, 24),
            ForeColor = MainText, Font = new Font("Segoe UI Semibold", 11.3f, FontStyle.Bold),
            AutoEllipsis = true
        };
        card.Controls.Add(name);
        var description = new Label
        {
            Text = t.Description, Location = new Point(19, 104), Size = new Size(340, 35),
            ForeColor = MutedText, Font = new Font("Segoe UI", 8.9f), AutoEllipsis = true
        };
        card.Controls.Add(description);
        var status = new Label
        {
            Text = StatusText(t), Location = new Point(19, 147), Size = new Size(340, 19),
            ForeColor = HasPath(t) ? Accent : MutedText, AutoEllipsis = true,
            Font = new Font("Segoe UI", 8.2f)
        };
        card.Controls.Add(status);
        var source = new RoundedButton("Origen", false) { Height = 32 };
        source.Click += (_, _) => OpenSite(t.SourceUrl);
        var select = new RoundedButton("Vincular", false) { Height = 32 };
        select.Click += (_, _) => { SelectFile(t); RenderPage(); };
        var launch = new RoundedButton("Ejecutar", true) { Height = 32 };
        launch.Click += async (_, _) => await RunToolAsync(t);
        card.Controls.Add(source);
        card.Controls.Add(select);
        card.Controls.Add(launch);
        card.Resize += (_, _) =>
        {
            int usable = card.ClientSize.Width - 38;
            int gap = 8;
            int piece = (usable - gap * 2) / 3;
            source.Bounds = new Rectangle(19, card.ClientSize.Height - 48, piece, 32);
            select.Bounds = new Rectangle(source.Right + gap, source.Top, piece, 32);
            launch.Bounds = new Rectangle(select.Right + gap, source.Top, usable - piece * 2 - gap * 2, 32);
            name.Width = description.Width = status.Width = usable;
        };
        HookWheel(card);
        return card;
    }

    private static string GetGlyph(string id) => id switch
    {
        "everything" or "regscanner" => "⌕",
        "systeminformer" => "∿",
        _ => "▤"
    };

    private void LayoutCards()
    {
        if (_viewport.Width < 50) return;
        int width = _viewport.ClientSize.Width - _scrollbar.Width;
        int usable = Math.Max(300, width - 50);
        int cols = usable >= 1130 ? 3 : usable >= 730 ? 2 : 1;
        int gap = 15;
        int cardWidth = Math.Max(280, (usable - gap * (cols - 1)) / cols);
        int x0 = 25, y0 = 8;
        const int cardHeight = 223, rowGap = 15;
        for (int i = 0; i < _cards.Count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            _cards[i].Bounds = new Rectangle(x0 + col * (cardWidth + gap), y0 + row * (cardHeight + rowGap),
                cardWidth, _cards[i] is Label ? 28 : cardHeight);
        }
        int rows = (_cards.Count + cols - 1) / cols;
        _contentHeight = y0 + rows * (cardHeight + rowGap) + 12;
        _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _contentHeight - _viewport.ClientSize.Height));
        _cardHost.Size = new Size(width, Math.Max(_viewport.ClientSize.Height, _contentHeight));
        _cardHost.Location = new Point(0, -_scrollOffset);
        _scrollbar.Invalidate();
    }

    private void HookWheel(Control c)
    {
        c.MouseWheel += ScrollWheel;
        foreach (Control child in c.Controls) HookWheel(child);
    }

    private void ScrollWheel(object? sender, MouseEventArgs e)
    {
        int max = Math.Max(0, _contentHeight - _viewport.ClientSize.Height);
        _scrollOffset = Math.Clamp(_scrollOffset - (e.Delta / 120) * 80, 0, max);
        _cardHost.Top = -_scrollOffset;
        _scrollbar.Invalidate();
    }

    private Rectangle ScrollThumb()
    {
        int visible = Math.Max(1, _viewport.ClientSize.Height);
        int total = Math.Max(visible, _contentHeight);
        if (total <= visible) return Rectangle.Empty;
        int h = Math.Max(32, (int)((double)visible / total * visible));
        int travel = visible - h - 12;
        int maxOffset = Math.Max(1, total - visible);
        int y = 6 + (int)((double)_scrollOffset / maxOffset * travel);
        return new Rectangle(4, y, 6, h);
    }

    private void ScrollbarPaint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var thumb = ScrollThumb();
        if (thumb == Rectangle.Empty) return;
        using var brush = new SolidBrush(Color.FromArgb(90, 74, 88));
        using var path = Shape.RoundRect(thumb, 3);
        e.Graphics.FillPath(brush, path);
    }

    private void ScrollbarMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var thumb = ScrollThumb();
        if (thumb == Rectangle.Empty) return;
        if (!thumb.Contains(e.Location))
        {
            int max = Math.Max(0, _contentHeight - _viewport.Height);
            _scrollOffset = Math.Clamp(_scrollOffset + (e.Y < thumb.Top ? -_viewport.Height : _viewport.Height), 0, max);
            _cardHost.Top = -_scrollOffset;
            _scrollbar.Invalidate();
        }
        _draggingScroll = true;
        _dragStartY = e.Y;
        _dragStartOffset = _scrollOffset;
        _scrollbar.Capture = true;
    }

    private void ScrollbarMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_draggingScroll) return;
        var thumb = ScrollThumb();
        int travel = Math.Max(1, _viewport.Height - Math.Max(32, thumb.Height) - 12);
        int maxOffset = Math.Max(0, _contentHeight - _viewport.Height);
        _scrollOffset = Math.Clamp(_dragStartOffset + (int)((double)(e.Y - _dragStartY) * maxOffset / travel), 0, maxOffset);
        _cardHost.Top = -_scrollOffset;
        _scrollbar.Invalidate();
    }

    private string? GetPath(ToolInfo tool)
    {
        if (_paths.TryGetValue(tool.Id, out string? path) && File.Exists(path)) return path;
        if (tool.DownloadFileName is not null)
        {
            string cache = Path.Combine(_dataDir, "Tools", tool.DownloadFileName);
            if (File.Exists(cache)) return cache;
        }
        return null;
    }

    private bool HasPath(ToolInfo t) => GetPath(t) is not null;

    private string StatusText(ToolInfo t)
    {
        if (_busy && _runningId == t.Id) return "Descargando y comprobando archivo...";
        if (GetPath(t) is string path)
            return (_discovered.Contains(t.Id) ? "Detectado: " : "Disponible: ") + Path.GetFileName(path);
        return t.DownloadUrl is null ? "Sin vincular · selecciona un archivo" : "Descarga automática al ejecutar";
    }

    private string? SelectFile(ToolInfo t)
    {
        using var dialog = new OpenFileDialog
        {
            Title = $"Selecciona el archivo de {t.Title}",
            Filter = t.Kind == ToolKind.PowerShellScript ? "Script PowerShell (*.ps1)|*.ps1" : "Aplicación Windows (*.exe)|*.exe",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return null;
        _paths[t.Id] = dialog.FileName;
        _discovered.Remove(t.Id);
        SaveSettings();
        return dialog.FileName;
    }

    private async Task<string> DownloadAsync(ToolInfo t)
    {
        if (t.DownloadUrl is null || t.DownloadFileName is null || t.GitBlobSha1 is null)
            throw new InvalidOperationException("La herramienta no tiene una descarga verificable configurada.");
        string dir = Path.Combine(_dataDir, "Tools");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, t.DownloadFileName);
        string temp = path + ".tmp";
        const long limit = 50L * 1024 * 1024;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(4) };
            using var res = await client.GetAsync(t.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            res.EnsureSuccessStatusCode();
            if (res.Content.Headers.ContentLength > limit)
                throw new InvalidOperationException("El archivo supera el límite de descarga.");
            await using (var input = await res.Content.ReadAsStreamAsync())
            await using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[81920];
                long bytes = 0;
                int read;
                while ((read = await input.ReadAsync(buffer)) > 0)
                {
                    bytes += read;
                    if (bytes > limit) throw new InvalidOperationException("Archivo demasiado grande.");
                    await output.WriteAsync(buffer.AsMemory(0, read));
                }
            }
            if (!string.Equals(ComputeGitBlobSha1(temp), t.GitBlobSha1, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("El archivo no coincide con la versión fijada del repositorio. No se ejecutará.");
            File.Move(temp, path, overwrite: true);
            _paths[t.Id] = path;
            _discovered.Remove(t.Id);
            SaveSettings();
            return path;
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { /* archivo bloqueado */ }
        }
    }

    private async Task RunToolAsync(ToolInfo t)
    {
        if (_busy) return;
        string? path = GetPath(t);
        bool needsDownload = path is null && t.DownloadUrl is not null;
        if (path is null && !needsDownload)
        {
            if (MessageBox.Show(this,
                $"No encontramos instalado {t.Title}.\n\nElige un archivo local para abrirlo como administrador.\nSi no lo tienes, pulsa Origen para ir a su página oficial.",
                "Seleccionar herramienta", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return;
            path = SelectFile(t);
            if (path is null) return;
        }
        string msg = needsDownload
            ? $"{t.Title}\n\nSe descargará automáticamente desde esta versión fija:\n{t.DownloadUrl}\n\nSe comprobará que el archivo coincida con el publicado, y después se ejecutará como administrador.\n\nLas herramientas de terceros no han sido auditadas completamente. Windows puede impedir su ejecución. ¿Continuar?"
            : $"Se ejecutará {t.Title} como administrador.\n\nArchivo:\n{path}\n\nLos programas de terceros pueden leer información del equipo. Úsalos solo con consentimiento. ¿Continuar?";
        if (MessageBox.Show(this, msg, "Confirmar ejecución con administrador",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        _busy = true;
        _runningId = t.Id;
        RenderPage();
        try
        {
            if (needsDownload)
                path = await DownloadAsync(t);
            if (path is null || !File.Exists(path)) throw new FileNotFoundException("No se encuentra el archivo.");
            if (t.Kind == ToolKind.Executable && !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("El archivo no es un .exe.");
            if (t.Kind == ToolKind.PowerShellScript && !path.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("El archivo no es un .ps1.");

            // Protege el caché de alteraciones posteriores a la descarga.
            if (t.GitBlobSha1 is not null && t.DownloadFileName is not null &&
                string.Equals(Path.GetFullPath(path), Path.GetFullPath(Path.Combine(_dataDir, "Tools", t.DownloadFileName)),
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ComputeGitBlobSha1(path), t.GitBlobSha1, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("El archivo descargado fue modificado. Ejecución cancelada.");

            if (t.Group == ToolGroup.Commands) OpenInAdminCmd(path, t.Kind);
            else OpenAsAdministrator(path);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // El usuario canceló la solicitud UAC, no es un error de la aplicación.
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo abrir la herramienta:\n\n" + ex.Message +
                "\n\nNo desactives Windows Defender ni Smart App Control para forzar su ejecución.",
                "Error al ejecutar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
            _runningId = null;
            RenderPage();
        }
    }

    private static void OpenAsAdministrator(string path)
    {
        Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true, Verb = "runas", WorkingDirectory = Path.GetDirectoryName(path)!
        });
    }

    private static void OpenInAdminCmd(string path, ToolKind kind)
    {
        // Comando PowerShell codificado para impedir que una ruta con caracteres de CMD
        // (&, %, |, espacios, etc.) se interprete como una instrucción adicional.
        string escaped = path.Replace("'", "''");
        string script = $"& '{escaped}'";
        string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        string policy = kind == ToolKind.PowerShellScript ? " -ExecutionPolicy Bypass" : "";
        string arguments = "/d /k powershell.exe -NoLogo -NoProfile" + policy + " -EncodedCommand " + encoded;
        Process.Start(new ProcessStartInfo("cmd.exe")
        {
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = Path.GetDirectoryName(path)!
        });
    }

    private static string ComputeGitBlobSha1(string path)
    {
        // Coincide con `git hash-object <archivo>`; no es el SHA-1 del contenido aislado.
        byte[] body = File.ReadAllBytes(path);
        byte[] prefix = Encoding.UTF8.GetBytes($"blob {body.Length}\0");
        byte[] full = new byte[prefix.Length + body.Length];
        Buffer.BlockCopy(prefix, 0, full, 0, prefix.Length);
        Buffer.BlockCopy(body, 0, full, prefix.Length, body.Length);
        return Convert.ToHexString(SHA1.HashData(full)).ToLowerInvariant();
    }

    private void DiscoverTools()
    {
        foreach (ToolInfo tool in ToolCatalog.All.Where(t => t.Kind == ToolKind.Executable))
        {
            if (GetPath(tool) is not null) continue;
            string? local = ToolDiscovery.Find(tool.Id);
            if (local is null) continue;
            _paths[tool.Id] = local;
            _discovered.Add(tool.Id);
        }
    }

    private string SettingsFile => Path.Combine(_dataDir, "settings.json");

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return;
            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SettingsFile));
            if (config is not null)
                foreach (var kv in config) _paths[kv.Key] = kv.Value;
        }
        catch { /* Opciones iniciales si settings.json no es válido. */ }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(_dataDir);
            File.WriteAllText(SettingsFile,
                JsonSerializer.Serialize(_paths, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudieron guardar los ajustes:\n" + ex.Message,
                "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void OpenSite(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show("No se pudo abrir la página:\n" + ex.Message); }
    }
}

internal static class Shape
{
    internal static GraphicsPath RoundRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(2, Math.Min(radius * 2, Math.Min(rect.Width, rect.Height)));
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class BorderPanel : Panel
{
    private readonly Color _fill;
    private readonly Color _border;
    private readonly int _radius;

    internal BorderPanel(Color fill, Color border, int radius)
    {
        _fill = fill;
        _border = border;
        _radius = radius;
        DoubleBuffered = true;
        BackColor = Color.Transparent;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null) e.Graphics.Clear(Parent.BackColor);
        else e.Graphics.Clear(_fill);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var path = Shape.RoundRect(rect, _radius);
        using var brush = new SolidBrush(_fill);
        using var pen = new Pen(_border);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
        base.OnPaint(e);
    }
}

internal sealed class RoundedButton : Button
{
    private bool _active;
    private bool _hover;
    private readonly bool _primary;
    private static readonly Color TextMain = Color.FromArgb(240, 230, 236);

    internal RoundedButton(string text, bool primary)
    {
        Text = text;
        _primary = primary;
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = new Font("Segoe UI Semibold", 8.6f, FontStyle.Bold);
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    internal void SetSelected(bool selected) { _active = selected; Invalidate(); }
    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var accent = Color.FromArgb(209, 104, 134);
        Color bg = (_active || _primary) ? accent : _hover ? Color.FromArgb(66, 50, 60) : Color.FromArgb(42, 36, 43);
        if (!Enabled) bg = Color.FromArgb(46, 40, 45);
        using var path = Shape.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 11);
        using var brush = new SolidBrush(bg);
        e.Graphics.FillPath(brush, path);
        using var pen = new Pen(Color.FromArgb(66, 53, 65));
        if (!_active && !_primary) e.Graphics.DrawPath(pen, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle,
            (_active || _primary) ? Color.FromArgb(22, 17, 22) : Enabled ? TextMain : Color.FromArgb(135, 127, 135),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues)
        {
            using var focusPen = new Pen(accent) { DashStyle = DashStyle.Dot };
            e.Graphics.DrawRectangle(focusPen, 3, 3, Width - 7, Height - 7);
        }
    }
}
