using System.ComponentModel;
using Spectre.Console.Cli;

namespace TiVkParser.Commands;

public class BaseSettings : CommandSettings
{
    [Description("Извлечь пустой файл конфигурации (Default = false)")]
    [CommandOption("--extCfg")]
    [DefaultValue(false)]
    public bool IsExtractConfigFile { get; init; }
    
    [Description("Путь до файла конфигурации (Default = TiVkParser.toml)")]
    [CommandOption("-c|--cfg")]
    [DefaultValue("TiVkParser.toml")]
    public string? ConfigFile { get; init; }
    
    [Description("Максимальное кол-во элементов для всех запросов, получаемых через VK API (DefaultValue = 1000)")]
    [CommandOption("-l|--apiLimit")]
    [DefaultValue((long)1000)]
    public long TotalItemsForApi { get; init; }
    
    [Description("Логгирование (DefaultValue = true)")]
    [CommandOption("--logging")]
    [DefaultValue(true)]
    public bool IsLogging { get; init; }
}