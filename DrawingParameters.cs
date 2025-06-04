namespace ProgrammingTechnologiesLaboratoryWork6_2_CSharp;

/// <summary>
/// Класс для управления параметрами отрисовки трехмерного графика.
/// Содержит настройки масштабирования, границы осей и центры отрисовки.
/// Используется для преобразования трехмерных координат в двухмерные для отображения.
/// </summary>
public class DrawingParameters
{
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }
    public float MinZ { get; set; }
    public float MaxZ { get; set; }
    public float BaseScale { get; set; }
    public float FinalZScale { get; set; }
    public float DrawCenterX { get; set; }
    public float DrawCenterY { get; set; }
}