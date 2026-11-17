namespace QuizApp;

public sealed class MainForm : Form
{
    private sealed record PageEntry(UserControl Control, string Title);

    private readonly Panel contentPanel = new();
    private readonly TableLayoutPanel headerPanel = new();
    private readonly Button backButton = new() { Text = "← Quay lại", AutoSize = true, Visible = false };
    private readonly Button zoomOutButton = new() { Text = "A−", AutoSize = true };
    private readonly Button zoomInButton = new() { Text = "A+", AutoSize = true };
    private readonly Label zoomLabel = new() { AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label titleLabel = new()
    {
        AutoSize = true,
        Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Stack<PageEntry> history = new();
    private UserControl? currentPage;

    public MainForm()
    {
        Text = "Quiz App";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(850, 550);
        Size = new Size(1100, 760);
        Font = new Font("Microsoft Sans Serif", SettingsManager.GlobalFontSize);

        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = Math.Max(54, (int)(SettingsManager.GlobalFontSize * 3.5F + 14));
        titleLabel.Font = new Font(titleLabel.Font.FontFamily, SettingsManager.GlobalFontSize + 2, FontStyle.Bold);
        headerPanel.ColumnCount = 3;
        headerPanel.RowCount = 1;
        headerPanel.Padding = new Padding(10, 6, 10, 6);
        headerPanel.BackColor = SystemColors.Control;
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var header = headerPanel;

        var rightTools = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true
        };
        rightTools.Controls.AddRange(new Control[] { zoomInButton, zoomLabel, zoomOutButton });
        header.Controls.Add(backButton, 0, 0);
        header.Controls.Add(titleLabel, 1, 0);
        header.Controls.Add(rightTools, 2, 0);

        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Padding = new Padding(10);
        contentPanel.BackColor = SystemColors.Window;
        Controls.Add(contentPanel);
        Controls.Add(header);

        backButton.Click += (_, _) => GoBack();
        zoomInButton.Click += (_, _) => SettingsManager.ZoomIn();
        zoomOutButton.Click += (_, _) => SettingsManager.ZoomOut();
        SettingsManager.FontSizeChanged += SettingsManager_FontSizeChanged;
        zoomLabel.Text = $"{SettingsManager.GlobalFontSize:0} pt";

        ShowPage(new SubjectListControl(), "Môn học", pushHistory: false);
    }

    private void ShowPage(UserControl page, string title, bool pushHistory = true)
    {
        if (currentPage is not null)
        {
            if (pushHistory)
                history.Push(new PageEntry(currentPage, titleLabel.Text));
            contentPanel.Controls.Remove(currentPage);
            currentPage.Visible = false;
        }

        currentPage = page;
        page.Dock = DockStyle.Fill;
        page.Visible = true;
        WirePage(page);
        contentPanel.Controls.Add(page);
        SettingsManager.ApplyFont(page);
        titleLabel.Text = title;
        backButton.Visible = history.Count > 0;
    }

    private void WirePage(UserControl page)
    {
        if (page is INavigationPage navigationPage)
            navigationPage.NavigateRequested += (next, title) => ShowPage(next, title);
        if (page is IBackNavigationPage backPage)
            backPage.BackRequested += (_, _) => GoBack();
    }

    private void GoBack()
    {
        if (history.Count == 0)
            return;

        if (currentPage is not null)
        {
            contentPanel.Controls.Remove(currentPage);
            currentPage.Dispose();
        }

        var previous = history.Pop();
        currentPage = previous.Control;
        currentPage.Visible = true;
        contentPanel.Controls.Add(currentPage);
        SettingsManager.ApplyFont(currentPage);
        titleLabel.Text = previous.Title;
        backButton.Visible = history.Count > 0;
    }

    private void SettingsManager_FontSizeChanged(object? sender, EventArgs e)
    {
        Font = new Font(Font.FontFamily, SettingsManager.GlobalFontSize, Font.Style);
        headerPanel.Height = Math.Max(54, (int)(SettingsManager.GlobalFontSize * 3.5F + 14));
        SettingsManager.ApplyFont(this);
        titleLabel.Font = new Font(titleLabel.Font.FontFamily, SettingsManager.GlobalFontSize + 2, FontStyle.Bold);
        zoomLabel.Text = $"{SettingsManager.GlobalFontSize:0} pt";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SettingsManager.FontSizeChanged -= SettingsManager_FontSizeChanged;
            while (history.Count > 0)
                history.Pop().Control.Dispose();
        }
        base.Dispose(disposing);
    }
}
