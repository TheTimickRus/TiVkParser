// ReSharper disable ClassNeverInstantiated.Global

using System.ComponentModel;
using Spectre.Console.Cli;

namespace TiVkParser.Commands.SearchByKeywords;

public class SearchByKeywordsSettings : BaseSettings
{
    [Description("Поиск по комментариям (Default = false)")]
    [CommandOption("--comments")]
    [DefaultValue(false)]
    public bool IsComments { get; init; }
}