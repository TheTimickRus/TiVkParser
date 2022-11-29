using Spectre.Console;

namespace TiVkParser.Services;

public static class AnsiConsoleLib
{
    public static void ShowFiglet(string text, Justify? alignment, Color? color)
    {
        AnsiConsole.Write(new FigletText(text) { Justification = alignment, Color = color });
        AnsiConsole.WriteLine();
    }

    public static void ShowRule(string text, Justify? alignment, Color? color)
    {
        AnsiConsole.Write(
            new Rule(text)
            {
                Justification = alignment,
                Style = new Style(color)
            });
        AnsiConsole.WriteLine();
    }

    public static void ShowHeader(bool clearConsole = true)
    {
        if (clearConsole)
            AnsiConsole.Console.Clear();
        
        ShowFiglet(Constants.Titles.VeryShortTitle, Justify.Center, Constants.Colors.MainColor);
        ShowRule(Constants.Titles.FullTitle, Justify.Right, Constants.Colors.MainColor);
    }
}