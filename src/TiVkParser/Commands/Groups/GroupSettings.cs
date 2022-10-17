// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.ComponentModel;
using Spectre.Console.Cli;

namespace TiVkParser.Commands.Groups;

public class GroupSettings : BaseSettings
{
    [Description("Временная граница для получаемых постов. Задается в формате dd:mm:yy (DefaultValue = null)")]
    [CommandOption("-d|--date")]
    [DefaultValue(null)]
    public string? DateFilter { get; init; }
        
    [Description("Общий лимит кол-ва получаемых объектов для всех запросов (DefaultValue = 1000)")]
    [CommandOption("--limit")]
    [DefaultValue((long)1000)]
    public long LimitFilter { get; init; }
        
    [Description("Поиск по лайкам (DefaultValue = false)")]
    [CommandOption("--likes")]
    [DefaultValue(false)]
    public bool IsLike { get; init; }
        
    [Description("Поиск по комментариям (DefaultValue = true)")]
    [CommandOption("--comments")]
    [DefaultValue(true)]
    public bool IsComments { get; init; }
}