// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace TiVkParser.Core.Helpers;

/// <summary>
/// Класс - Helper для работы с VK пагинацией
/// </summary>
public class OffsetPagination
{
    /// <summary>
    /// Общее количество элементов
    /// </summary>
    public long TotalCount { get; set; }
    
    /// <summary>
    /// Текущий Offset. Передавать в API запрос
    /// </summary>
    public long CurrentOffset { get; private set; }
    
    /// <summary>
    /// Величина, на которую необходимо смещать значение пагинации
    /// </summary>
    public long OffsetLenght { get; set; }
    
    /// <summary>
    /// Признак окончания работы пагинации. Передавать в условие цикла
    /// </summary>
    public bool IsNotFinal => TotalCount > CurrentOffset;
    
    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="totalCount">Всего объектов</param>
    /// <param name="offsetStart">С какого значения начинать пагинацию. По умолчанию - 0</param>
    /// <param name="offsetLenght">Величина смещения. По умолчанию - 100</param>
    public OffsetPagination(long totalCount, long offsetStart = 0, long offsetLenght = 100)
    {
        TotalCount = totalCount;
        CurrentOffset = offsetStart;
        OffsetLenght = offsetLenght;
    }
    
    /// <summary>
    /// Метод для увеличения Offset. Вызывать в теле цикла, после выполнения API запроса
    /// </summary>
    /// <param name="manualOffset">Смещение, отличное от OffsetLenght</param>
    public void Increment(long? manualOffset = null)
    {
        if (manualOffset is null)
        {
            CurrentOffset += OffsetLenght;
            return;
        }
        
        CurrentOffset += manualOffset.Value;
    }
}