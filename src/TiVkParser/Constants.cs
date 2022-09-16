﻿using System.Diagnostics.CodeAnalysis;
using Spectre.Console;

namespace TiVkParser;

public static class Constants
{
    public static class Params
    {
        public const string AccessToken = "vk1.a.VKvZGRjUoJIJYIoFFx_pIrWS6DXQMXR1wRA52VyfLqBpDQRcSEp8ph4C3_GGC0uTz_4NfDtcavV1dSuhKvTlTotBVSRHApFrmOOh7bnLVM_el1WdiYlzWLqaF-2hYJ83dIgAABcppxCKg-yfKm2PvKjHaPGR-XebBCz62mEHkKOB4TGORBo3eMiQb2Gpxkcc";
    }
    
    public static class Titles
    {
        /// <summary>
        /// *Название программы* (*Версия* (*дата*)) by *Разработчик*
        /// </summary>
        public const string FullTitle = "TiVkParser (v.1.0 (02.09.2022)) by Timick";
        /// <summary>
        /// *Название программы* by *Разработчик*
        /// </summary>
        public const string ShortTitle = "TiVkParser by Timick";
        /// <summary>
        /// *Название программы*
        /// </summary>
        public const string VeryShortTitle = "TiVkParser";
        /// <summary>
        /// Имя лог-файла
        /// </summary>
        public const string LogFileName = $"{VeryShortTitle}.log";
    }

    [SuppressMessage("Usage", "CA2211:Поля, не являющиеся константами, не должны быть видимыми")]
    public static class Colors
    {
        /// <summary>
        /// Основной цвет
        /// </summary>
        public static Color MainColor = Color.SteelBlue;
        /// <summary>
        /// Цвет успеха
        /// </summary>
        public static Color SuccessColor = Color.SeaGreen1;
        /// <summary>
        /// Цвет ошибки
        /// </summary>
        public static Color ErrorColor = Color.Red; 
    }
}