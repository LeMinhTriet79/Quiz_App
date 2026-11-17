namespace QuizApp;

internal sealed class QuestionEditorControl : UserControl, IBackNavigationPage
{
    private readonly int quizId;
    private int questionId;
    private int questionNumber;
    private int totalQuestionCount;
    private readonly Label questionHeading = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold)
    };
    private readonly TextBox contentTextBox = new()
    {
        Multiline = true,
        AcceptsReturn = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill
    };
    private readonly TopAlignedPictureBox questionPicture = new() { Dock = DockStyle.Fill };
    private readonly TextBox[] optionTextBoxes = { new(), new(), new(), new() };
    private readonly RadioButton[] correctRadioButtons =
    {
        new() { Text = "A" }, new() { Text = "B" }, new() { Text = "C" }, new() { Text = "D" }
    };
    private byte[]? imageContent;

    public event EventHandler? BackRequested;
    public event EventHandler? QuestionSaved;

    public QuestionEditorControl(int quizId, Question? question, int questionNumber, int totalQuestionCount)
    {
        this.quizId = quizId;
        questionId = question?.Id ?? 0;
        this.questionNumber = questionNumber;
        this.totalQuestionCount = totalQuestionCount;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.Controls.Add(questionHeading, 0, 0);

        var questionPanel = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Size = new Size(900, 350),
            SplitterDistance = 450
        };
        questionPanel.Panel1.Controls.Add(contentTextBox);
        questionPanel.Panel2.Controls.Add(questionPicture);
        root.Controls.Add(questionPanel, 0, 1);

        var optionsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(0, 6, 0, 0)
        };
        optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        optionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        optionsPanel.Controls.Add(new Label
        {
            Text = "Đáp án đúng", AutoSize = true, Anchor = AnchorStyles.Left
        }, 0, 0);
        optionsPanel.Controls.Add(new Label
        {
            Text = "Nội dung đáp án", AutoSize = true, Anchor = AnchorStyles.Left
        }, 1, 0);
        for (var i = 0; i < 4; i++)
        {
            optionTextBoxes[i].Dock = DockStyle.Fill;
            optionTextBoxes[i].Margin = new Padding(3, 3, 3, 3);
            optionsPanel.Controls.Add(correctRadioButtons[i], 0, i + 1);
            optionsPanel.Controls.Add(optionTextBoxes[i], 1, i + 1);
        }
        root.Controls.Add(optionsPanel, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var saveButton = new Button { Text = "Lưu / Quay lại", AutoSize = true };
        var saveAndAddButton = new Button { Text = "Lưu & tạo câu tiếp", AutoSize = true };
        var backButton = new Button { Text = "Hủy / Quay lại", AutoSize = true };
        var clearImageButton = new Button { Text = "Xóa ảnh", AutoSize = true };
        buttons.Controls.AddRange(new Control[] { saveButton, saveAndAddButton, backButton, clearImageButton });
        root.Controls.Add(buttons, 0, 3);
        Controls.Add(root);

        contentTextBox.KeyDown += ContentTextBox_KeyDown;
        saveButton.Click += (_, _) =>
        {
            if (SaveQuestion())
            {
                QuestionSaved?.Invoke(this, EventArgs.Empty);
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
        };
        saveAndAddButton.Click += (_, _) => SaveAndPrepareNext();
        backButton.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);
        clearImageButton.Click += (_, _) => SetImage(null);
        LoadQuestion(question);
        UpdateQuestionHeading();
    }

    private void LoadQuestion(Question? question)
    {
        if (question is null)
            return;

        contentTextBox.Text = question.TextContent ?? "";
        optionTextBoxes[0].Text = question.OptionA ?? "";
        optionTextBoxes[1].Text = question.OptionB ?? "";
        optionTextBoxes[2].Text = question.OptionC ?? "";
        optionTextBoxes[3].Text = question.OptionD ?? "";
        if (question.CorrectAnswer.Length == 1 &&
            question.CorrectAnswer[0] is >= 'A' and <= 'D')
            correctRadioButtons[question.CorrectAnswer[0] - 'A'].Checked = true;
        imageContent = question.ImageContent;
        SetPictureFromBytes(imageContent);
    }

    private void ContentTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.Control || e.KeyCode != Keys.V)
            return;

        if (Clipboard.ContainsImage())
        {
            using var clipboardImage = Clipboard.GetImage();
            if (clipboardImage is not null)
            {
                imageContent = ImageToBytes(clipboardImage);
                SetPictureFromBytes(imageContent);
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }
        else if (Clipboard.ContainsText())
        {
            contentTextBox.SelectedText = Clipboard.GetText();
            e.SuppressKeyPress = true;
            e.Handled = true;
        }
    }

    private bool SaveQuestion()
    {
        if (string.IsNullOrWhiteSpace(contentTextBox.Text) && imageContent is null)
        {
            MessageBox.Show(this, "Nhập nội dung hoặc dán hình ảnh cho câu hỏi.",
                "Thiếu nội dung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var correctIndex = Array.FindIndex(correctRadioButtons, radio => radio.Checked);
        if (correctIndex < 0)
        {
            MessageBox.Show(this, "Vui lòng chọn đáp án đúng.", "Thiếu đáp án đúng",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        Database.SaveQuestion(new Question
        {
            Id = questionId,
            QuizId = quizId,
            TextContent = NullIfEmpty(contentTextBox.Text),
            ImageContent = imageContent,
            OptionA = NullIfEmpty(optionTextBoxes[0].Text),
            OptionB = NullIfEmpty(optionTextBoxes[1].Text),
            OptionC = NullIfEmpty(optionTextBoxes[2].Text),
            OptionD = NullIfEmpty(optionTextBoxes[3].Text),
            CorrectAnswer = ((char)('A' + correctIndex)).ToString()
        });
        return true;
    }

    private void SaveAndPrepareNext()
    {
        var savedQuestionWasNew = questionId == 0;
        if (!SaveQuestion())
            return;

        QuestionSaved?.Invoke(this, EventArgs.Empty);
        if (savedQuestionWasNew)
        {
            questionNumber++;
            totalQuestionCount++;
        }
        else
        {
            questionNumber = totalQuestionCount + 1;
        }
        questionId = 0;
        contentTextBox.Clear();
        foreach (var option in optionTextBoxes)
            option.Clear();
        foreach (var radio in correctRadioButtons)
            radio.Checked = false;
        SetImage(null);
        UpdateQuestionHeading();
        contentTextBox.Focus();
    }

    private void UpdateQuestionHeading()
    {
        questionHeading.Text =
            $"Câu {questionNumber} - Nội dung câu hỏi (Ctrl+V để dán chữ hoặc hình ảnh):";
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static byte[] ImageToBytes(Image image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    private void SetPictureFromBytes(byte[]? bytes)
    {
        questionPicture.Image?.Dispose();
        questionPicture.Image = null;
        if (bytes is null)
            return;

        using var stream = new MemoryStream(bytes);
        using var image = Image.FromStream(stream);
        questionPicture.Image = new Bitmap(image);
    }

    private void SetImage(byte[]? bytes)
    {
        imageContent = bytes;
        SetPictureFromBytes(bytes);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            questionPicture.Image?.Dispose();
        base.Dispose(disposing);
    }
}
