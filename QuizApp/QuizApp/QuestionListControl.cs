namespace QuizApp;

internal sealed class QuestionListControl : UserControl, INavigationPage
{
    private readonly Quiz quiz;
    private readonly DataGridView grid = new();
    private readonly Button editButton = new() { Text = "Sửa câu hỏi", AutoSize = true, Enabled = false };
    private readonly Button deleteButton = new() { Text = "Xóa câu hỏi", AutoSize = true, Enabled = false };
    private List<Question> loadedQuestions = new();

    public event Action<UserControl, string>? NavigateRequested;

    public QuestionListControl(Quiz quiz)
    {
        this.quiz = quiz;
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
        var addButton = new Button { Text = "Thêm câu hỏi", AutoSize = true };
        toolbar.Controls.AddRange(new Control[] { addButton, editButton, deleteButton });

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoGenerateColumns = false;
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "QuestionNumber", HeaderText = "Câu", Width = 75,
            ReadOnly = true
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Question.Id), HeaderText = "Mã", Width = 70
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Question.TextContent), HeaderText = "Nội dung",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Question.CorrectAnswer), HeaderText = "Đáp án đúng", Width = 120
        });

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        Controls.Add(root);

        addButton.Click += (_, _) => OpenEditor(null);
        editButton.Click += (_, _) => OpenEditor(SelectedQuestion);
        deleteButton.Click += (_, _) => DeleteSelected();
        grid.SelectionChanged += (_, _) => UpdateActions();
        grid.CellDoubleClick += (_, _) => OpenEditor(SelectedQuestion);
        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "QuestionNumber")
                e.Value = $"Câu {e.RowIndex + 1}";
        };
        LoadQuestions();
    }

    private Question? SelectedQuestion => grid.CurrentRow?.DataBoundItem as Question;

    private void LoadQuestions()
    {
        loadedQuestions = Database.GetQuestions(quiz.Id);
        grid.DataSource = loadedQuestions;
        UpdateActions();
    }

    private void UpdateActions()
    {
        editButton.Enabled = deleteButton.Enabled = SelectedQuestion is not null;
    }

    private void OpenEditor(Question? question)
    {
        var ordinal = question is null
            ? loadedQuestions.Count + 1
            : loadedQuestions.FindIndex(item => item.Id == question.Id) + 1;
        var editor = new QuestionEditorControl(quiz.Id, question, ordinal, loadedQuestions.Count);
        editor.QuestionSaved += (_, _) => LoadQuestions();
        NavigateRequested?.Invoke(editor, question is null ? "Thêm câu hỏi" : "Sửa câu hỏi");
    }

    private void DeleteSelected()
    {
        if (SelectedQuestion is not { } question)
            return;
        if (MessageBox.Show(this, "Xóa câu hỏi đã chọn?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        Database.DeleteQuestion(question.Id);
        LoadQuestions();
    }
}
