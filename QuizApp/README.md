# Quiz App (Windows Forms)

Ứng dụng quản lý môn học, bộ đề, câu hỏi trắc nghiệm và chế độ luyện tập/ôn bài. Project dùng C# Windows Forms trên .NET 8 và lưu dữ liệu cục bộ trong SQLite.

Quan hệ dữ liệu: `Subjects (1) -> (N) Quizzes (1) -> (N) Questions`.

## Cấu trúc project

```text
QuizApp/
  Program.cs                 Điểm khởi chạy, khởi tạo database
  MainForm.cs                Cửa sổ duy nhất, điều hướng và zoom chữ
  SubjectListControl.cs      Dashboard và CRUD môn học
  QuizListControl.cs         Danh sách bộ đề và điều hướng chức năng
  QuestionListControl.cs     Danh sách CRUD câu hỏi
  QuestionEditorControl.cs   Nhập câu hỏi, đáp án và ảnh clipboard
  QuizModeControl.cs         Practice Mode và Review Mode
  SettingsManager.cs         Cỡ chữ toàn cục
  Models.cs                  Subject, Quiz và Question
  Database.cs                Khởi tạo schema, migration và truy vấn SQLite bằng Dapper
  QuizApp.csproj
```

## Thư viện NuGet

Project tham chiếu sẵn `Dapper` và `Microsoft.Data.Sqlite` trong `QuizApp.csproj`. Nếu tạo project mới, cài bằng:

```powershell
dotnet add package Dapper --version 2.1.66
dotnet add package Microsoft.Data.Sqlite --version 8.0.11
```

Mở `QuizApp.sln` bằng Visual Studio có hỗ trợ .NET 8, sau đó build/run như một ứng dụng Windows Forms. Hoặc chạy:

```powershell
dotnet restore
dotnet run --project QuizApp\QuizApp.csproj
```

Database `quizapp.db` được tạo cạnh file thực thi (`AppContext.BaseDirectory`). Để chuyển ứng dụng cùng dữ liệu sang máy khác, publish ứng dụng, chạy ít nhất một lần để tạo database, rồi nén toàn bộ thư mục publish cùng file `quizapp.db`. Không xóa hoặc tách database khỏi thư mục ứng dụng.

Trong trình tạo/sửa câu hỏi, Ctrl+V dán văn bản vào nội dung câu hỏi; nếu clipboard có hình ảnh, ứng dụng chuyển ảnh sang PNG `byte[]` và lưu vào cột `ImageContent` dạng SQLite BLOB. Khi hiển thị, byte array được giải mã thành ảnh và clone để ảnh không phụ thuộc vòng đời của stream.

`TextContent` và bốn trường `OptionA`–`OptionD` cho phép `NULL`; các trường đáp án để trống sẽ được lưu là `NULL`. Nội dung câu hỏi có thể chỉ gồm hình ảnh, nhưng phải chọn một đáp án đúng.

Khi khởi chạy, database cũ có câu hỏi tham chiếu trực tiếp đến môn học sẽ được nâng cấp trong một transaction: mỗi môn học nhận một bộ đề **Bộ đề cũ**, các câu hỏi/ảnh/đáp án được chuyển sang quan hệ mới và giữ nguyên ID. Không xóa database cũ trước khi chạy migration.
