﻿using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;
using TiVkParser;
using TiVkParser.Commands.Friends;
using TiVkParser.Commands.Groups;
using TiVkParser.Commands.Keywords;
using TiVkParser.Logging;
using TiVkParser.Services;

Console.OutputEncoding = Encoding.UTF8;
Console.Title = Constants.Titles.ShortTitle;
SerilogLib.FileName = Constants.Titles.LogFileName;

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
        .WithExample(new[] { "gr" })
        .WithExample(new[] { "gr", "-l 10000" })
        .WithExample(new[] { "gr", "--apiLimit 10000" })
        .WithExample(new[] { "gr", "--likes" })
        .WithExample(new[] { "gr", "--date=10.16.2022", "--likes=true", "--comments=false" });

    conf.AddCommand<KeywordsCommand>("keywords")
        .WithAlias("kw")
        .WithDescription("Получение постов и/или комментариев по ключевым словам")
        .WithExample(new[] { "keyword" })
        .WithExample(new[] { "kw" });

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