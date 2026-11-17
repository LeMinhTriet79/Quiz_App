namespace QuizApp;

internal sealed class QuizModeControl : UserControl
{
    private readonly IReadOnlyList<Question> questions;
    private readonly bool reviewMode;
    private readonly RadioButton[] answerRadios =
    {
        new() { Text = "A" }, new() { Text = "B" }, new() { Text = "C" }, new() { Text = "D" }
    };
    private readonly Label progressLabel = new()
    {
        TextAlign = ContentAlignment.MiddleLeft,
        Dock = DockStyle.Fill,
        AutoEllipsis = true
    };
    private readonly ProgressBar progressBar = new()
    {
        Dock = DockStyle.Fill,
        Minimum = 0,
        Maximum = 100,
        Style = ProgressBarStyle.Continuous
    };
    private readonly RichTextBox questionText = new()
    {
        ReadOnly = true,
        DetectUrls = false,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = SystemColors.Window,
        ScrollBars = RichTextBoxScrollBars.Vertical,
        WordWrap = true,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 0, 0, 4)
    };
    private readonly TopAlignedPictureBox questionImage = new()
    {
        Location = Point.Empty,
        SizeMode = PictureBoxSizeMode.Normal,
        Margin = new Padding(0)
    };
    private readonly Panel imageScrollPanel = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        BackColor = SystemColors.Window,
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(0)
    };
    private readonly FlowLayoutPanel zoomBar = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = true,
        Padding = new Padding(6, 4, 6, 4),
        BackColor = Color.FromArgb(246, 248, 250),
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(0, 0, 0, 4)
    };
    private readonly Label zoomTitleLabel = new()
    {
        Text = "🔍 Phóng to ảnh:",
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font(Control.DefaultFont.FontFamily, 9F, FontStyle.Bold),
        Margin = new Padding(2, 6, 4, 2)
    };
    private readonly Button zoomOutButton = new()
    {
        Text = "−",
        AutoSize = true,
        Margin = new Padding(2, 2, 2, 2)
    };
    private readonly TrackBar zoomSlider = new()
    {
        Minimum = SettingsManager.MinimumImageZoom,
        Maximum = SettingsManager.MaximumImageZoom,
        Value = SettingsManager.ImageZoomPercent,
        TickFrequency = 25,
        SmallChange = 5,
        LargeChange = 25,
        Width = 180,
        Height = 32,
        AutoSize = false,
        Margin = new Padding(0, 0, 2, 0)
    };
    private readonly Button zoomInButton = new()
    {
        Text = "+",
        AutoSize = true,
        Margin = new Padding(2, 2, 2, 2)
    };
    private readonly Label zoomPercentLabel = new()
    {
        Text = $"{SettingsManager.ImageZoomPercent}%",
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font(Control.DefaultFont.FontFamily, 9F, FontStyle.Bold),
        Margin = new Padding(2, 6, 6, 2),
        MinimumSize = new Size(46, 0)
    };
    private readonly Button fitButton = new()
    {
        Text = "100% (Vừa khung)",
        AutoSize = true,
        Margin = new Padding(4, 2, 2, 2)
    };
    private readonly Button fitWidthButton = new()
    {
        Text = "Vừa chiều rộng",
        AutoSize = true,
        Margin = new Padding(2, 2, 2, 2)
    };
    private readonly TableLayoutPanel questionContainer = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 3,
        Padding = new Padding(4),
        BackColor = SystemColors.Window
    };
    private readonly SplitContainer bodySplit = new()
    {
        Dock = DockStyle.Fill,
        Orientation = Orientation.Vertical,
        BorderStyle = BorderStyle.FixedSingle,
        Size = new Size(900, 600),
        SplitterDistance = 225,
        SplitterWidth = 6
    };
    private readonly Button backQuestionButton = new() { Text = "Back", AutoSize = true };
    private readonly Button nextQuestionButton = new() { Text = "Next", AutoSize = true };
    private readonly Button submitButton = new() { Text = "Submit", AutoSize = true, Visible = false };
    private readonly Label resultLabel = new()
    {
        Dock = DockStyle.Bottom,
        AutoSize = false,
        Height = 36,
        TextAlign = ContentAlignment.MiddleLeft,
        Visible = false
    };
    private readonly Dictionary<int, string> selections = new();
    private int currentIndex;
    private bool rendering;
    private bool updatingZoomUI;

    public QuizModeControl(Quiz quiz, IReadOnlyList<Question> questions, bool reviewMode)
    {
        this.questions = questions;
        this.reviewMode = reviewMode;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(4)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var statusBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 6)
        };
        statusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        statusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        progressLabel.Text = $"There are {questions.Count} questions, and your progress of answering is";
        statusBar.Controls.Add(progressLabel, 0, 0);
        statusBar.Controls.Add(progressBar, 1, 0);
        root.Controls.Add(statusBar, 0, 0);

        var answerColumn = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = SystemColors.Control,
            Margin = new Padding(0)
        };
        answerColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        answerColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        answerColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        answerColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        answerColumn.Controls.Add(new Label
        {
            Text = "Answer",
            ForeColor = Color.Blue,
            Font = new Font(Font, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        }, 0, 0);

        var radioPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        foreach (var radio in answerRadios)
        {
            radio.AutoSize = true;
            radio.Margin = new Padding(0, 3, 0, 3);
            radioPanel.Controls.Add(radio);
        }
        answerColumn.Controls.Add(radioPanel, 0, 1);

        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        navigation.Controls.AddRange(new Control[] { backQuestionButton, nextQuestionButton, submitButton });
        answerColumn.Controls.Add(navigation, 0, 2);
        bodySplit.Panel1.Controls.Add(answerColumn);
        bodySplit.Panel1.BorderStyle = BorderStyle.FixedSingle;

        // Build zoom toolbar
        var toolTip = new ToolTip();
        toolTip.SetToolTip(zoomSlider, "Kéo thanh trượt để phóng to / thu nhỏ ảnh (tự động lưu cấu hình)");
        toolTip.SetToolTip(zoomOutButton, "Thu nhỏ ảnh (-10%)");
        toolTip.SetToolTip(zoomInButton, "Phóng to ảnh (+10%)");
        toolTip.SetToolTip(fitButton, "Khôi phục kích thước vừa khung nhìn (100%)");
        toolTip.SetToolTip(fitWidthButton, "Phóng to vừa khít chiều rộng khung nhìn");

        zoomBar.Controls.AddRange(new Control[]
        {
            zoomTitleLabel,
            zoomOutButton,
            zoomSlider,
            zoomInButton,
            zoomPercentLabel,
            fitButton,
            fitWidthButton
        });

        imageScrollPanel.Controls.Add(questionImage);

        // Configure right-hand question container
        questionContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        questionContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        questionContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        questionContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
        questionContainer.Controls.Add(zoomBar, 0, 0);
        questionContainer.Controls.Add(questionText, 0, 1);
        questionContainer.Controls.Add(imageScrollPanel, 0, 2);

        bodySplit.Panel2.Controls.Add(questionContainer);
        root.Controls.Add(bodySplit, 0, 1);
        Controls.Add(root);
        Controls.Add(resultLabel);

        for (var i = 0; i < answerRadios.Length; i++)
        {
            var optionIndex = i;
            answerRadios[i].CheckedChanged += (_, _) =>
            {
                if (rendering || reviewMode || !answerRadios[optionIndex].Checked)
                    return;
                selections[currentIndex] = ((char)('A' + optionIndex)).ToString();
                UpdateProgress();
            };
        }

        backQuestionButton.Click += (_, _) => NavigateQuestion(-1);
        nextQuestionButton.Click += (_, _) => NavigateQuestion(1);
        submitButton.Click += (_, _) => Submit();

        // Zoom event wiring
        zoomSlider.ValueChanged += (_, _) =>
        {
            if (updatingZoomUI)
                return;
            updatingZoomUI = true;
            try
            {
                SettingsManager.SetImageZoom(zoomSlider.Value);
                zoomPercentLabel.Text = $"{zoomSlider.Value}%";
            }
            finally
            {
                updatingZoomUI = false;
            }
        };

        zoomInButton.Click += (_, _) => SettingsManager.ZoomImageIn();
        zoomOutButton.Click += (_, _) => SettingsManager.ZoomImageOut();
        fitButton.Click += (_, _) => SettingsManager.SetImageZoom(100);
        fitWidthButton.Click += (_, _) => FitToWidth();

        SettingsManager.ImageZoomChanged += SettingsManager_ImageZoomChanged;

        // Splitter persistence and mouse handling
        bodySplit.SplitterMoved += (_, _) =>
        {
            if (!rendering && bodySplit.ClientSize.Width > 0)
            {
                var ratio = (double)bodySplit.SplitterDistance / bodySplit.ClientSize.Width;
                SettingsManager.SetSplitterRatio(ratio);
            }
        };

        SizeChanged += (_, _) =>
        {
            SetSplitRatio();
            ResizeQuestionContent();
        };
        bodySplit.SizeChanged += (_, _) =>
        {
            SetSplitRatio();
            ResizeQuestionContent();
        };
        imageScrollPanel.Resize += (_, _) => UpdateImageSizeAndPosition();
        HandleCreated += (_, _) =>
        {
            SetSplitRatio();
            ResizeQuestionContent();
        };

        SetupImagePanningAndZoom();
        RenderQuestion();
    }

    private void SettingsManager_ImageZoomChanged(object? sender, EventArgs e)
    {
        if (updatingZoomUI)
            return;
        updatingZoomUI = true;
        try
        {
            if (zoomSlider.Value != SettingsManager.ImageZoomPercent)
                zoomSlider.Value = SettingsManager.ImageZoomPercent;
            zoomPercentLabel.Text = $"{SettingsManager.ImageZoomPercent}%";
            UpdateImageSizeAndPosition();
        }
        finally
        {
            updatingZoomUI = false;
        }
    }

    private void SetupImagePanningAndZoom()
    {
        var dragStartMouse = Point.Empty;
        var dragStartScroll = Point.Empty;
        var isDragging = false;

        questionImage.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && questionImage.Image is not null)
            {
                isDragging = true;
                dragStartMouse = Cursor.Position;
                dragStartScroll = new Point(-imageScrollPanel.AutoScrollPosition.X, -imageScrollPanel.AutoScrollPosition.Y);
                questionImage.Cursor = Cursors.Hand;
            }
        };

        questionImage.MouseMove += (_, e) =>
        {
            if (isDragging && e.Button == MouseButtons.Left)
            {
                var dx = Cursor.Position.X - dragStartMouse.X;
                var dy = Cursor.Position.Y - dragStartMouse.Y;
                imageScrollPanel.AutoScrollPosition = new Point(
                    Math.Max(0, dragStartScroll.X - dx),
                    Math.Max(0, dragStartScroll.Y - dy));
            }
        };

        void EndDrag()
        {
            if (isDragging)
            {
                isDragging = false;
                questionImage.Cursor = Cursors.Default;
            }
        }

        questionImage.MouseUp += (_, _) => EndDrag();
        questionImage.MouseLeave += (_, _) => EndDrag();

        MouseEventHandler onMouseWheel = (_, e) =>
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.Delta > 0)
                    SettingsManager.ZoomImageIn();
                else if (e.Delta < 0)
                    SettingsManager.ZoomImageOut();
                if (e is HandledMouseEventArgs handled)
                    handled.Handled = true;
            }
            else
            {
                var curY = -imageScrollPanel.AutoScrollPosition.Y;
                var newY = Math.Max(0, curY - e.Delta);
                imageScrollPanel.AutoScrollPosition = new Point(-imageScrollPanel.AutoScrollPosition.X, newY);
                if (e is HandledMouseEventArgs handled)
                    handled.Handled = true;
            }
        };

        questionImage.MouseWheel += onMouseWheel;
        imageScrollPanel.MouseWheel += onMouseWheel;
    }

    private void FitToWidth()
    {
        if (questionImage.Image is null || imageScrollPanel.ClientSize.Width <= 10)
            return;

        var img = questionImage.Image;
        var containerWidth = imageScrollPanel.ClientSize.Width;
        var containerHeight = imageScrollPanel.ClientSize.Height;

        var baseFitScale = Math.Min(
            (float)containerWidth / img.Width,
            (float)containerHeight / img.Height);

        if (baseFitScale > 0)
        {
            var fitWidthScale = (float)containerWidth / img.Width;
            var targetPercent = (int)Math.Round((fitWidthScale / baseFitScale) * 100.0);
            SettingsManager.SetImageZoom(targetPercent);
        }
    }

    private void SetSplitRatio()
    {
        if (!bodySplit.IsHandleCreated)
            return;
        var width = bodySplit.ClientSize.Width;
        const int leftMinimum = 170;
        const int rightMinimum = 350;
        if (width < leftMinimum + rightMinimum)
            return;
        var target = (int)Math.Round(width * SettingsManager.SplitterRatio);
        var distance = Math.Clamp(target, leftMinimum, width - rightMinimum);
        bodySplit.SplitterDistance = distance;
        bodySplit.Panel1MinSize = leftMinimum;
        bodySplit.Panel2MinSize = rightMinimum;
    }

    private void ResizeQuestionContent()
    {
        var hasText = questionText.Visible;
        var hasImage = questionImage.Image is not null;

        zoomBar.Visible = hasImage;
        imageScrollPanel.Visible = hasImage;

        questionContainer.SuspendLayout();
        questionContainer.RowStyles[0].SizeType = SizeType.AutoSize;

        if (hasText && hasImage)
        {
            questionContainer.RowStyles[1].SizeType = SizeType.Percent;
            questionContainer.RowStyles[1].Height = 30;
            questionContainer.RowStyles[2].SizeType = SizeType.Percent;
            questionContainer.RowStyles[2].Height = 70;
        }
        else if (hasText)
        {
            questionContainer.RowStyles[1].SizeType = SizeType.Percent;
            questionContainer.RowStyles[1].Height = 100;
            questionContainer.RowStyles[2].SizeType = SizeType.Absolute;
            questionContainer.RowStyles[2].Height = 0;
        }
        else if (hasImage)
        {
            questionContainer.RowStyles[1].SizeType = SizeType.Absolute;
            questionContainer.RowStyles[1].Height = 0;
            questionContainer.RowStyles[2].SizeType = SizeType.Percent;
            questionContainer.RowStyles[2].Height = 100;
        }
        else
        {
            questionContainer.RowStyles[1].SizeType = SizeType.Percent;
            questionContainer.RowStyles[1].Height = 100;
            questionContainer.RowStyles[2].SizeType = SizeType.Absolute;
            questionContainer.RowStyles[2].Height = 0;
        }

        questionContainer.ResumeLayout(true);
        UpdateImageSizeAndPosition();
    }

    private void UpdateImageSizeAndPosition()
    {
        if (questionImage.Image is null)
        {
            questionImage.Visible = false;
            return;
        }

        var img = questionImage.Image;
        if (img.Width <= 0 || img.Height <= 0)
            return;

        var containerWidth = imageScrollPanel.ClientSize.Width;
        var containerHeight = imageScrollPanel.ClientSize.Height;

        if (containerWidth <= 10 || containerHeight <= 10)
            return;

        var baseFitScale = Math.Min(
            (float)containerWidth / img.Width,
            (float)containerHeight / img.Height);

        if (baseFitScale <= 0f)
            return;

        var zoomFactor = SettingsManager.ImageZoomPercent / 100.0f;
        var scale = baseFitScale * zoomFactor;

        var targetWidth = Math.Max(1, (int)Math.Round(img.Width * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(img.Height * scale));

        var x = targetWidth < containerWidth ? (containerWidth - targetWidth) / 2 : 0;

        var prevScroll = new Point(-imageScrollPanel.AutoScrollPosition.X, -imageScrollPanel.AutoScrollPosition.Y);
        imageScrollPanel.AutoScrollPosition = new Point(0, 0);

        questionImage.SuspendLayout();
        questionImage.Location = new Point(x, 0);
        questionImage.Size = new Size(targetWidth, targetHeight);
        questionImage.ResumeLayout();

        imageScrollPanel.AutoScrollMinSize = new Size(targetWidth, targetHeight);

        if (prevScroll.X > 0 || prevScroll.Y > 0)
        {
            imageScrollPanel.AutoScrollPosition = prevScroll;
        }

        questionImage.Visible = true;
        questionImage.Invalidate();
    }

    private void NavigateQuestion(int delta)
    {
        var next = currentIndex + delta;
        if (next < 0 || next >= questions.Count)
            return;
        currentIndex = next;
        RenderQuestion();
    }

    private void RenderQuestion()
    {
        var question = questions[currentIndex];
        rendering = true;
        try
        {
            questionText.Text = question.TextContent ?? "";
            questionText.Visible = !string.IsNullOrWhiteSpace(question.TextContent);
            SetQuestionImage(question.ImageContent);
            ResizeQuestionContent();
            for (var i = 0; i < answerRadios.Length; i++)
            {
                var letter = ((char)('A' + i)).ToString();
                answerRadios[i].Checked = reviewMode
                    ? question.CorrectAnswer == letter
                    : selections.TryGetValue(currentIndex, out var selected) && selected == letter;
                answerRadios[i].Enabled = !reviewMode && !resultLabel.Visible;
            }
        }
        finally
        {
            rendering = false;
        }

        backQuestionButton.Enabled = currentIndex > 0;
        var isLast = currentIndex == questions.Count - 1;
        nextQuestionButton.Visible = !isLast;
        submitButton.Visible = !reviewMode && isLast && !resultLabel.Visible;
        UpdateProgress();
        ResizeQuestionContent();
    }

    private void SetQuestionImage(byte[]? imageBytes)
    {
        questionImage.Image?.Dispose();
        questionImage.Image = null;
        questionImage.Visible = imageBytes is not null;
        if (imageBytes is null)
        {
            questionImage.Invalidate();
            return;
        }

        using var stream = new MemoryStream(imageBytes);
        using var image = Image.FromStream(stream);
        questionImage.Image = new Bitmap(image);
        imageScrollPanel.AutoScrollPosition = new Point(0, 0);
        UpdateImageSizeAndPosition();
    }

    private void UpdateProgress()
    {
        var completed = reviewMode ? currentIndex + 1 : selections.Count;
        progressBar.Value = questions.Count == 0
            ? 0
            : Math.Clamp((int)Math.Round(completed * 100.0 / questions.Count), 0, 100);
    }

    private void Submit()
    {
        var score = 0;
        for (var i = 0; i < questions.Count; i++)
        {
            if (selections.TryGetValue(i, out var answer) && answer == questions[i].CorrectAnswer)
                score++;
        }
        resultLabel.Text = $"Kết quả: {score}/{questions.Count} câu đúng.";
        resultLabel.Visible = true;
        foreach (var radio in answerRadios)
            radio.Enabled = false;
        submitButton.Visible = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SettingsManager.ImageZoomChanged -= SettingsManager_ImageZoomChanged;
            questionImage.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}
