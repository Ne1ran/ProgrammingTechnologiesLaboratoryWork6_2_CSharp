namespace ProgrammingTechnologiesLaboratoryWork6_2_CSharp;

/// <summary>
/// Структура для представления точки в трехмерном пространстве.
/// Используется для хранения координат при визуализации графика функции.
/// </summary>
public struct Point3D
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Point3D(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}