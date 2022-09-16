using Spectre.Console;
using Spectre.Console.Cli;
using TiVkParser;
using TiVkParser.Commands;
using TiVkParser.Services;

var app = new CommandApp();
app.Configure(conf =>
{
    conf.AddCommand<GroupsCommand>("groups");
    
    conf.Settings.ApplicationName = "TiVkParser.exe";
    conf.Settings.ApplicationVersion = "v.1.0 (02.09.2022)";
    conf.Settings.ExceptionHandler += ex => 
    {
        AnsiConsoleLib.ShowFiglet(Constants.Titles.VeryShortTitle, Justify.Center, Constants.Colors.ErrorColor);
        AnsiConsole.MarkupLine("\n> [bold red]A fatal error has occurred in the operation of the program![/]\n");
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        
        SerilogLib.Fatal(ex);

        AnsiConsole.Console.Input.ReadKey(true);
        return -1;
    };
});
await app.RunAsync(args);