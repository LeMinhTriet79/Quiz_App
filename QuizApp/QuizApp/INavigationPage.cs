namespace QuizApp;

internal interface INavigationPage
{
    event Action<UserControl, string>? NavigateRequested;
}

internal interface IBackNavigationPage
{
    event EventHandler? BackRequested;
}
