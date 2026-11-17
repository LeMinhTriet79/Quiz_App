namespace QuizApp;

internal sealed class SubjectListControl : UserControl, INavigationPage
{
    private readonly DataGridView grid = new();
    private readonly TextBox nameInput = new() { Width = 230, Visible = false };
    private readonly Button addButton = new() { Text = "Thêm môn học", AutoSize = true };
    private readonly Button editButton = new() { Text = "Sửa", AutoSize = true, Enabled = false };
    private readonly Button deleteButton = new() { Text = "Xóa", AutoSize = true, Enabled = false };
    private readonly Button openButton = new() { Text = "Mở môn học", AutoSize = true, Enabled = false };
    private readonly Button saveNameButton = new() { Text = "Lưu", AutoSize = true, Visible = false };
    private readonly Button cancelNameButton = new() { Text = "Hủy", AutoSize = true, Visible = false };
    private Subject? editingSubject;

    public event Action<UserControl, string>? NavigateRequested;

    public SubjectListControl()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(4)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(4),
            Margin = new Padding(0, 0, 0, 6)
        };
        toolbar.Controls.AddRange(new Control[]
        {
            addButton, editButton, deleteButton, openButton, nameInput, saveNameButton, cancelNameButton
        });

        ConfigureGrid();
        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        Controls.Add(root);

        addButton.Click += (_, _) => BeginEdit(null);
        editButton.Click += (_, _) =>
        {
            if (SelectedSubject is { } subject)
                BeginEdit(subject);
        };
        deleteButton.Click += (_, _) => DeleteSelected();
        openButton.Click += (_, _) => OpenSelected();
        saveNameButton.Click += (_, _) => SaveName();
        cancelNameButton.Click += (_, _) => EndEdit();
        grid.SelectionChanged += (_, _) => UpdateActions();
        grid.CellDoubleClick += (_, _) => OpenSelected();
        LoadSubjects();
    }

    private Subject? SelectedSubject => grid.CurrentRow?.DataBoundItem as Subject;

    private void ConfigureGrid()
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoGenerateColumns = false;
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Subject.Id), HeaderText = "Mã", Width = 70
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Subject.Name), HeaderText = "Tên môn học",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Subject.CreatedAt), HeaderText = "Ngày tạo", Width = 180,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });
    }

    private void LoadSubjects()
    {
        grid.DataSource = Database.GetSubjects();
        UpdateActions();
    }

    private void UpdateActions()
    {
        var selected = SelectedSubject is not null;
        editButton.Enabled = selected && editingSubject is null;
        deleteButton.Enabled = selected && editingSubject is null;
        openButton.Enabled = selected && editingSubject is null;
        addButton.Enabled = editingSubject is null;
    }

    private void BeginEdit(Subject? subject)
    {
        editingSubject = subject;
        nameInput.Text = subject?.Name ?? "";
        nameInput.Visible = true;
        saveNameButton.Visible = true;
        cancelNameButton.Visible = true;
        addButton.Visible = false;
        editButton.Visible = false;
        deleteButton.Visible = false;
        openButton.Visible = false;
        nameInput.Focus();
        UpdateActions();
    }

    private void EndEdit()
    {
        editingSubject = null;
        nameInput.Clear();
        nameInput.Visible = false;
        saveNameButton.Visible = false;
        cancelNameButton.Visible = false;
        addButton.Visible = true;
        editButton.Visible = true;
        deleteButton.Visible = true;
        openButton.Visible = true;
        UpdateActions();
    }

    private void SaveName()
    {
        var name = nameInput.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show(this, "Vui lòng nhập tên môn học.", "Thiếu thông tin",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (editingSubject is null)
            Database.AddSubject(name);
        else
            Database.UpdateSubject(editingSubject.Id, name);

        EndEdit();
        LoadSubjects();
    }

    private void DeleteSelected()
    {
        if (SelectedSubject is not { } subject)
            return;
        if (MessageBox.Show(this, $"Xóa môn học \"{subject.Name}\" cùng toàn bộ bộ đề và câu hỏi?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        Database.DeleteSubject(subject.Id);
        LoadSubjects();
    }

    private void OpenSelected()
    {
        if (SelectedSubject is { } subject)
            NavigateRequested?.Invoke(new QuizListControl(subject), $"Bộ đề - {subject.Name}");
    }
}
