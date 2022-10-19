// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.ComponentModel;
using Spectre.Console.Cli;

namespace TiVkParser.Commands.Groups;

public class GroupsSettings : BaseSettings
{
    [Description("Временная граница для получаемых постов. Формат - mm.dd.yyyy (10.26.2022) (Default = null)")]
    [CommandOption("-d|--date")]
    [DefaultValue(null)]
    public DateTime? DateFilter { get; init; }
    
    [Description("Поиск по лайкам (Default = false)")]
    [CommandOption("--likes")]
    [DefaultValue(false)]
    public bool IsLike { get; init; }
        
    [Description("Поиск по комментариям (Default = true)")]
    [CommandOption("--comments")]
    [DefaultValue(true)]
    public bool IsComments { get; init; }
}