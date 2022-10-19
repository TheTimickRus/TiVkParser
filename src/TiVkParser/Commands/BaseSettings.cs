// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.ComponentModel;
using Spectre.Console.Cli;

namespace TiVkParser.Commands;

public class BaseSettings : CommandSettings
{
    [Description("Путь до файла конфигурации (Default = TiVkParser.toml)")]
    [CommandOption("-c|--cfg")]
    [DefaultValue("TiVkParser.toml")]
    public string? ConfigFile { get; init; }
    
    [Description("Максимальное кол-во элементов для всех запросов, получаемых через VK API (Default = 2500)")]
    [CommandOption("-l|--apiLimit")]
    [DefaultValue((long)2500)]
    public long ApiLimit { get; init; }
    
    [Description("Извлечь пустой файл конфигурации (Default = false)")]
    [CommandOption("--extCfg")]
    [DefaultValue(false)]
    public bool IsExtractConfigFile { get; init; }
    
    [Description("Логгирование (Default = true)")]
    [CommandOption("--logging")]
    [DefaultValue(true)]
    public bool IsLogging { get; init; }
}