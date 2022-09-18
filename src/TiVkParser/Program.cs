using Spectre.Console;
using Spectre.Console.Cli;
using TiVkParser;
using TiVkParser.Commands;
using TiVkParser.Services;

var app = new CommandApp();
app.Configure(conf =>
{
    conf.AddCommand<GroupsCommand>("groups");
    
    conf.Settings.ApplicationName = $"{Constants.Titles.VeryShortTitle}.exe";
    conf.Settings.ApplicationVersion = Constants.Titles.VersionWithDate;
    conf.Settings.ExceptionHandler += ex => 
    {
        AnsiConsoleLib.ShowFiglet(Constants.Titles.VeryShortTitle, Justify.Center, Constants.Colors.ErrorColor);
        AnsiConsoleLib.ShowRule(Constants.Titles.FullTitle, Justify.Right, Constants.Colors.ErrorColor);
        
        AnsiConsole.MarkupLine("\n> [bold red]A fatal error has occurred in the operation of the program![/]\n");
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        
        SerilogLib.Fatal(ex);
        
        AnsiConsole.Console.Input.ReadKey(true);
        return -1;
    };
});
await app.RunAsync(args);