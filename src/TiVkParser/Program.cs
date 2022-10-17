using Spectre.Console;
using Spectre.Console.Cli;
using TiVkParser;
using TiVkParser.Commands.Friends;
using TiVkParser.Commands.Groups;
using TiVkParser.Logging;
using TiVkParser.Services;

var app = new CommandApp();
app.Configure(conf =>
{
    conf.AddCommand<FriendsCommand>("friends")
        .WithAlias("fr")
        .WithDescription("Получение друзей пользователя используя UserID")
        .WithExample(new[] { "friends" })
        .WithExample(new[] { "fr" });

    conf.AddCommand<GroupsCommand>("groups")
        .WithAlias("gr")
        .WithDescription("Получение лайков и/или комментариев пользователя в группах")
        .WithExample(new[] { "groups" })
        .WithExample(new[] { "gr" });

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