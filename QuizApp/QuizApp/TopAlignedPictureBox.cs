namespace QuizApp;

internal sealed class TopAlignedPictureBox : PictureBox
{
    public TopAlignedPictureBox()
    {
        BorderStyle = BorderStyle.None;
        BackColor = SystemColors.Window;
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);

        if (Image is not null && ClientSize.Width > 0 && ClientSize.Height > 0)
        {
            var scale = Math.Min(
                (float)ClientSize.Width / Image.Width,
                (float)ClientSize.Height / Image.Height);
            var width = Image.Width * scale;
            var height = Image.Height * scale;
            var destination = new RectangleF((ClientSize.Width - width) / 2f, 0, width, height);

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(Image, destination);
        }

        ControlPaint.DrawBorder(e.Graphics, ClientRectangle, SystemColors.ControlDark,
            ButtonBorderStyle.Solid);
    }
}
