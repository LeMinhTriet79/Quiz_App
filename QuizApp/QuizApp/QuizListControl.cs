namespace QuizApp;

internal sealed class QuizListControl : UserControl, INavigationPage
{
    private readonly Subject subject;
    private readonly DataGridView grid = new();
    private readonly TextBox titleInput = new() { Width = 230, Visible = false };
    private readonly Button addButton = new() { Text = "Thêm đề", AutoSize = true };
    private readonly Button editButton = new() { Text = "Sửa đề", AutoSize = true };
    private readonly Button deleteButton = new() { Text = "Xóa đề", AutoSize = true };
    private readonly Button manageButton = new() { Text = "Quản lý câu hỏi", AutoSize = true };
    private readonly Button practiceButton = new() { Text = "Làm trắc nghiệm", AutoSize = true };
    private readonly Button reviewButton = new() { Text = "Ôn bài", AutoSize = true };
    private readonly Button saveButton = new() { Text = "Lưu", AutoSize = true, Visible = false };
    private readonly Button cancelButton = new() { Text = "Hủy", AutoSize = true, Visible = false };
    private Quiz? editingQuiz;

    public event Action<UserControl, string>? NavigateRequested;

    public QuizListControl(Subject subject)
    {
        this.subject = subject;
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
            addButton, editButton, deleteButton, manageButton, practiceButton, reviewButton,
            titleInput, saveButton, cancelButton
        });
        ConfigureGrid();
        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        Controls.Add(root);

        addButton.Click += (_, _) => BeginEdit(null);
        editButton.Click += (_, _) =>
        {
            if (SelectedQuiz is { } quiz)
                BeginEdit(quiz);
        };
        deleteButton.Click += (_, _) => DeleteSelected();
        manageButton.Click += (_, _) => OpenQuestions();
        practiceButton.Click += (_, _) => OpenQuizMode(review: false);
        reviewButton.Click += (_, _) => OpenQuizMode(review: true);
        saveButton.Click += (_, _) => SaveQuiz();
        cancelButton.Click += (_, _) => EndEdit();
        grid.SelectionChanged += (_, _) => UpdateActions();
        grid.CellDoubleClick += (_, _) => OpenQuestions();
        LoadQuizzes();
    }

    private Quiz? SelectedQuiz => grid.CurrentRow?.DataBoundItem as Quiz;

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
            DataPropertyName = nameof(Quiz.Id), HeaderText = "Mã", Width = 70
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Quiz.Title), HeaderText = "Tên bộ đề",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Quiz.CreatedAt), HeaderText = "Ngày tạo", Width = 180,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });
    }

    private void LoadQuizzes()
    {
        grid.DataSource = Database.GetQuizzes(subject.Id);
        UpdateActions();
    }

    private void UpdateActions()
    {
        var selected = SelectedQuiz is not null;
        editButton.Enabled = selected && editingQuiz is null;
        deleteButton.Enabled = selected && editingQuiz is null;
        manageButton.Enabled = selected && editingQuiz is null;
        practiceButton.Enabled = selected && editingQuiz is null;
        reviewButton.Enabled = selected && editingQuiz is null;
        addButton.Enabled = editingQuiz is null;
    }

    private void BeginEdit(Quiz? quiz)
    {
        editingQuiz = quiz;
        titleInput.Text = quiz?.Title ?? "";
        titleInput.Visible = true;
        saveButton.Visible = true;
        cancelButton.Visible = true;
        addButton.Visible = editButton.Visible = deleteButton.Visible = false;
        manageButton.Visible = practiceButton.Visible = reviewButton.Visible = false;
        titleInput.Focus();
        UpdateActions();
    }

    private void EndEdit()
    {
        editingQuiz = null;
        titleInput.Clear();
        titleInput.Visible = false;
        saveButton.Visible = cancelButton.Visible = false;
        addButton.Visible = editButton.Visible = deleteButton.Visible = true;
        manageButton.Visible = practiceButton.Visible = reviewButton.Visible = true;
        UpdateActions();
    }

    private void SaveQuiz()
    {
        var title = titleInput.Text.Trim();
        if (title.Length == 0)
        {
            MessageBox.Show(this, "Vui lòng nhập tên bộ đề.", "Thiếu thông tin",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (editingQuiz is null)
            Database.AddQuiz(subject.Id, title);
        else
            Database.UpdateQuiz(editingQuiz.Id, title);

        EndEdit();
        LoadQuizzes();
    }

    private void DeleteSelected()
    {
        if (SelectedQuiz is not { } quiz)
            return;
        if (MessageBox.Show(this, $"Xóa bộ đề \"{quiz.Title}\" và toàn bộ câu hỏi?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        Database.DeleteQuiz(quiz.Id);
        LoadQuizzes();
    }

    private void OpenQuestions()
    {
        if (SelectedQuiz is { } quiz)
            NavigateRequested?.Invoke(new QuestionListControl(quiz), $"Câu hỏi - {quiz.Title}");
    }

    private void OpenQuizMode(bool review)
    {
        if (SelectedQuiz is not { } quiz)
            return;
        var questions = Database.GetQuestions(quiz.Id);
        if (questions.Count == 0)
        {
            MessageBox.Show(this, "Bộ đề chưa có câu hỏi nào.", "Quiz App",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        NavigateRequested?.Invoke(
            new QuizModeControl(quiz, questions, review),
            $"{(review ? "Ôn bài" : "Làm bài")} - {quiz.Title}");
    }
}
