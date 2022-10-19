// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.ComponentModel;
using Spectre.Console.Cli;

namespace TiVkParser.Commands.Keywords;

public class KeywordsCommandSettings : BaseSettings
{
    [Description("Поиск по комментариям (Default = false)")]
    [CommandOption("--comments")]
    [DefaultValue(false)]
    public bool IsComments { get; init; }
}