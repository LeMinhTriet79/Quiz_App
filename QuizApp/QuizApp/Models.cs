namespace QuizApp;

public sealed class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class Quiz
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string Title { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public sealed class Question
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string? TextContent { get; set; }
    public byte[]? ImageContent { get; set; }
    public string? OptionA { get; set; }
    public string? OptionB { get; set; }
    public string? OptionC { get; set; }
    public string? OptionD { get; set; }
    public string CorrectAnswer { get; set; } = "A";
}
