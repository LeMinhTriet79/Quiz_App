using Dapper;
using Microsoft.Data.Sqlite;

namespace QuizApp;

internal static class Database
{
    private static readonly string DatabasePath =
        Path.Combine(AppContext.BaseDirectory, "quizapp.db");

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            ForeignKeys = true
        }.ToString());
        connection.Open();
        return connection;
    }

    public static void Initialize()
    {
        using var connection = OpenConnection();
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Subjects (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Quizzes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SubjectId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (SubjectId) REFERENCES Subjects(Id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS Questions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                QuizId INTEGER NOT NULL,
                TextContent TEXT NULL,
                ImageContent BLOB NULL,
                OptionA TEXT NULL,
                OptionB TEXT NULL,
                OptionC TEXT NULL,
                OptionD TEXT NULL,
                CorrectAnswer TEXT NOT NULL CHECK (CorrectAnswer IN ('A', 'B', 'C', 'D')),
                FOREIGN KEY (QuizId) REFERENCES Quizzes(Id) ON DELETE CASCADE
            );
            """);

        // Older installations stored questions directly under a subject. Move each
        // subject's questions into a generated legacy quiz without changing question IDs.
        var questionsSchema = connection.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Questions'");
        if (questionsSchema is not null &&
            System.Text.RegularExpressions.Regex.IsMatch(
                questionsSchema, @"\bSubjectId\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            connection.Execute("""
                BEGIN;
                CREATE TABLE Questions_new (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    QuizId INTEGER NOT NULL,
                    TextContent TEXT NULL,
                    ImageContent BLOB NULL,
                    OptionA TEXT NULL,
                    OptionB TEXT NULL,
                    OptionC TEXT NULL,
                    OptionD TEXT NULL,
                    CorrectAnswer TEXT NOT NULL CHECK (CorrectAnswer IN ('A', 'B', 'C', 'D')),
                    FOREIGN KEY (QuizId) REFERENCES Quizzes(Id) ON DELETE CASCADE
                );
                INSERT INTO Quizzes (SubjectId, Title, CreatedAt)
                SELECT s.Id, 'Bộ đề cũ', strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
                FROM Subjects s
                WHERE NOT EXISTS (
                    SELECT 1 FROM Quizzes z WHERE z.SubjectId = s.Id AND z.Title = 'Bộ đề cũ'
                );
                INSERT INTO Questions_new
                    (Id, QuizId, TextContent, ImageContent, OptionA, OptionB, OptionC, OptionD, CorrectAnswer)
                SELECT q.Id, z.Id, q.TextContent, q.ImageContent,
                       q.OptionA, q.OptionB, q.OptionC, q.OptionD, q.CorrectAnswer
                FROM Questions q
                JOIN Quizzes z
                  ON z.Id = (
                      SELECT MIN(z2.Id)
                      FROM Quizzes z2
                      WHERE z2.SubjectId = q.SubjectId AND z2.Title = 'Bộ đề cũ'
                  );
                DROP TABLE Questions;
                ALTER TABLE Questions_new RENAME TO Questions;
                COMMIT;
                """);
        }
    }

    public static List<Subject> GetSubjects()
    {
        using var connection = OpenConnection();
        return connection.Query<Subject>(
            "SELECT Id, Name, CreatedAt FROM Subjects ORDER BY Name COLLATE NOCASE").AsList();
    }

    public static void AddSubject(string name)
    {
        using var connection = OpenConnection();
        connection.Execute(
            "INSERT INTO Subjects (Name, CreatedAt) VALUES (@Name, @CreatedAt)",
            new { Name = name, CreatedAt = DateTime.Now.ToString("O") });
    }

    public static void UpdateSubject(int id, string name)
    {
        using var connection = OpenConnection();
        connection.Execute("UPDATE Subjects SET Name = @Name WHERE Id = @Id", new { Id = id, Name = name });
    }

    public static void DeleteSubject(int id)
    {
        using var connection = OpenConnection();
        connection.Execute("DELETE FROM Subjects WHERE Id = @Id", new { Id = id });
    }

    public static List<Quiz> GetQuizzes(int subjectId)
    {
        using var connection = OpenConnection();
        return connection.Query<Quiz>(
            """
            SELECT Id, SubjectId, Title, CreatedAt
            FROM Quizzes
            WHERE SubjectId = @SubjectId
            ORDER BY CreatedAt, Title COLLATE NOCASE
            """, new { SubjectId = subjectId }).AsList();
    }

    public static void AddQuiz(int subjectId, string title)
    {
        using var connection = OpenConnection();
        connection.Execute(
            "INSERT INTO Quizzes (SubjectId, Title, CreatedAt) VALUES (@SubjectId, @Title, @CreatedAt)",
            new { SubjectId = subjectId, Title = title, CreatedAt = DateTime.Now.ToString("O") });
    }

    public static void UpdateQuiz(int id, string title)
    {
        using var connection = OpenConnection();
        connection.Execute("UPDATE Quizzes SET Title = @Title WHERE Id = @Id", new { Id = id, Title = title });
    }

    public static void DeleteQuiz(int id)
    {
        using var connection = OpenConnection();
        connection.Execute("DELETE FROM Quizzes WHERE Id = @Id", new { Id = id });
    }

    public static List<Question> GetQuestions(int quizId)
    {
        using var connection = OpenConnection();
        return connection.Query<Question>(
            """
            SELECT Id, QuizId, TextContent, ImageContent, OptionA, OptionB, OptionC, OptionD, CorrectAnswer
            FROM Questions WHERE QuizId = @QuizId ORDER BY Id
            """, new { QuizId = quizId }).AsList();
    }

    public static void SaveQuestion(Question question)
    {
        using var connection = OpenConnection();
        if (question.Id == 0)
        {
            connection.Execute(
                """
                INSERT INTO Questions
                    (QuizId, TextContent, ImageContent, OptionA, OptionB, OptionC, OptionD, CorrectAnswer)
                VALUES
                    (@QuizId, @TextContent, @ImageContent, @OptionA, @OptionB, @OptionC, @OptionD, @CorrectAnswer)
                """, question);
        }
        else
        {
            connection.Execute(
                """
                UPDATE Questions
                SET TextContent = @TextContent, ImageContent = @ImageContent,
                    OptionA = @OptionA, OptionB = @OptionB, OptionC = @OptionC, OptionD = @OptionD,
                    CorrectAnswer = @CorrectAnswer
                WHERE Id = @Id AND QuizId = @QuizId
                """, question);
        }
    }

    public static void DeleteQuestion(int questionId)
    {
        using var connection = OpenConnection();
        connection.Execute("DELETE FROM Questions WHERE Id = @Id", new { Id = questionId });
    }
}
