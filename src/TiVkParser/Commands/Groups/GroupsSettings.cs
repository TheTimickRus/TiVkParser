// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.ComponentModel;
using Spectre.Console.Cli;

namespace TiVkParser.Commands.Groups;

public class GroupsSettings : BaseSettings
{
    [Description("Временная граница для получаемых постов. Задается в формате dd.mm.yyyy (26.10.2022) (DefaultValue = null)")]
    [CommandOption("-d|--date")]
    [DefaultValue(null)]
    public DateTime? DateFilter { get; init; }
    
    [Description("Поиск по лайкам (DefaultValue = false)")]
    [CommandOption("--likes")]
    [DefaultValue(false)]
    public bool IsLike { get; init; }
        
    [Description("Поиск по комментариям (DefaultValue = true)")]
    [CommandOption("--comments")]
    [DefaultValue(true)]
    public bool IsComments { get; init; }
}