// ReSharper disable MemberCanBePrivate.Global

namespace TiVkParser.Core.Helpers;

/// <summary>
/// Класс - Helper для работы с VK пагинацией
/// </summary>
public class OffsetPagination
{
    /// <summary>
    /// Общее количество элементов
    /// </summary>
    public ulong TotalCount { get; set; }
    
    /// <summary>
    /// Текущий Offset. Передавать в API запрос
    /// </summary>
    public ulong CurrentOffset { get; private set; }
    
    /// <summary>
    /// Величина, на которую необходимо смещать значение пагинации
    /// </summary>
    public ulong OffsetLenght { get; set; }
    
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
    public OffsetPagination(ulong totalCount, ulong offsetStart = 0, ulong offsetLenght = 100)
    {
        TotalCount = totalCount;
        CurrentOffset = offsetStart;
        OffsetLenght = offsetLenght;
    }
    
    /// <summary>
    /// Метод для увеличения Offset. Вызывать в теле цикла, после выполнения API запроса
    /// </summary>
    public void Increment(ulong? manualOffsetForCurrentIteration = null)
    {
        if (manualOffsetForCurrentIteration is null)
        {
            CurrentOffset += OffsetLenght;
            return;
        }
        
        CurrentOffset += manualOffsetForCurrentIteration.Value;
    }
}