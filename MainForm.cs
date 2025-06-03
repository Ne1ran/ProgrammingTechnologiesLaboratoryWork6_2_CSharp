using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProgrammingTechnologiesLaboratoryWork6_2_CSharp
{
    public partial class MainForm : Form
    {
        private const string ThreadResultFilePath = @"C:\Save\resultsDll.txt";

        private readonly List<Thread> _threads = new();
        private Pen _basePen = null!;
        private Pen _graphicsPen = null!;
        private CancellationTokenSource _cancellationTokenSource;

        private float _calculatingStep; // Шаг для X. Чем меньше - тем лучше график выглядит
        private int _maxX; // Ограничение по X для графика. Это значение отражает, сколько X мы подразумеваем в графике
        private int _maxY; // Ограничение по Y для графика. Это значение отражает, сколько Y мы подразумеваем в графике

        public MainForm()
        {
            InitializeComponent();
            _cancellationTokenSource = new();
            progressBar1.Maximum = 100;
        }

        private void MainForm_Load_1(object sender, EventArgs e)
        {
            _basePen = new(Color.Black);
            _graphicsPen = new(Color.Red);
            _calculatingStep = 0.1f; // Задаем количество точек в графике. Чем больше, тем более четким он получится
            _maxX = 10;
            _maxY = 10;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void Button2_Click(object sender, EventArgs e)
        {
            StopThreads();

            _cancellationTokenSource = new();

            int minX = Convert.ToInt32(textBox1.Text);
            int maxX = Convert.ToInt32(textBox4.Text);
            int minY = Convert.ToInt32(textBox2.Text);
            int maxY = Convert.ToInt32(textBox3.Text);
            int threadsCount = Convert.ToInt32(textBox5.Text);
            int sizeOfArrays = Convert.ToInt32(textBox6.Text);

            progressBar1.Maximum = 100;
            progressBar1.Step = 1;
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            progressBar1.Refresh();

            ClearFiles();

            List<Task> tasks = new();
            for (int i = 0; i < threadsCount; i++) {
                int threadId = i + 1;
                var calculator =
                        new ProgrammingTechnologiesLaboratoryWork6_1.ThreadCalculator(threadId, minX, maxX, minY, maxY, sizeOfArrays, UpdateUI);

                Task task = Task.Run(() => calculator.ThreadDoWork());
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            await DrawResults();
        }

        private List<Point3D> ReadResultsFromFile()
        {
            List<Point3D> points = new();
            try {
                if (File.Exists(ThreadResultFilePath)) {
                    string[] lines = File.ReadAllLines(ThreadResultFilePath);
                    foreach (string entry in lines) {
                        if (string.IsNullOrEmpty(entry)) {
                            continue;
                        }
                        if (!entry.Contains('[')) {
                            continue;
                        }

                        string[] oneRecord = entry.Split(';');
                        foreach (string singleDataRecord in oneRecord) {
                            // Извлекаем координаты с помощью регулярных выражений
                            Match zMatch = Regex.Match(singleDataRecord, @"z = (-?\d+,\d+)");
                            Match xMatch = Regex.Match(singleDataRecord, @"x = (\d+)");
                            Match yMatch = Regex.Match(singleDataRecord, @"y = (\d+)");

                            if (!zMatch.Success || !xMatch.Success || !yMatch.Success) {
                                continue;
                            }

                            if (float.TryParse(zMatch.Groups[1].Value.Replace(',', '.'), out float z) && float.TryParse(xMatch.Groups[1].Value, out float x)
                                && float.TryParse(yMatch.Groups[1].Value, out float y)) {
                                points.Add(new(x, y, z));
                            }
                        }
                    }
                } else {
                    MessageBox.Show("Файл с результатами не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } catch (Exception ex) {
                MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return points;
        }

        private async Task DrawResults()
        {
            List<Point3D> results = ReadResultsFromFile();
            pictureBox1.Refresh(); // Обновляем графику
            await Task.Delay(1000); // Небольшая задержка
            DrawFunction(pictureBox1, results); // Рисуем сам график
        }

        private void DrawFunction(PictureBox pictureBox, List<Point3D> points)
        {
            if (points == null || points.Count == 0)
            {
                return;
            }

            try
            {
                using (var bmp = new Bitmap(pictureBox.Width, pictureBox.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                        // Определяем границы данных
                        float minX = points.Min(p => p.X);
                        float maxX = points.Max(p => p.X);
                        float minY = points.Min(p => p.Y);
                        float maxY = points.Max(p => p.Y);
                        float minZ = points.Min(p => p.Z);
                        float maxZ = points.Max(p => p.Z);

                        // Угол для изометрической проекции (в радианах)
                        double angle30Deg = Math.PI / 6.0; // 30 градусов
                        double cos30 = Math.Cos(angle30Deg);
                        double sin30 = Math.Sin(angle30Deg);

                        // Определяем область рисования с отступами
                        float padding = 40; // Отступ от краев PictureBox
                        float drawAreaWidth = pictureBox.Width - 2 * padding;
                        float drawAreaHeight = pictureBox.Height - 2 * padding;

                        // Рассчитываем эффективное максимальное значение Z для масштабирования
                        float effectiveMaxZForScaling = Math.Min(maxZ, 500.0f);
                        // Убедимся, что эффективный minZ не больше effectiveMaxZForScaling
                        float effectiveMinZForScaling = Math.Min(minZ, effectiveMaxZForScaling);


                        // Включаем точки осей в расчет границ проекции для корректного масштабирования осей
                         var pointsForScaling = new List<Point3D>(points)
                         {
                             new Point3D(minX, minY, effectiveMinZForScaling), // Начало координат данных (с ограниченным Z)
                             new Point3D(maxX, minY, effectiveMinZForScaling), // Конец оси X
                             new Point3D(minX, maxY, effectiveMinZForScaling), // Конец оси Y
                             new Point3D(minX, minY, effectiveMaxZForScaling) // Конец оси Z (с ограничением)
                         };


                        // Вычисляем границы проекции всех точек для определения точного масштаба
                        float minProjectedX = float.MaxValue;
                        float maxProjectedX = float.MinValue;
                        float minProjectedY = float.MaxValue;
                        float maxProjectedY = float.MinValue;

                        foreach (Point3D point3D in pointsForScaling)
                        {
                            // Проектируем точку без учета масштаба и смещения, используя стандартную изометрию
                            // При проекции для масштабирования используем ограниченное значение Z
                            PointF projected = ProjectPointRaw(point3D, minX, minY, effectiveMinZForScaling, cos30, sin30);

                            minProjectedX = Math.Min(minProjectedX, projected.X);
                            maxProjectedX = Math.Max(maxProjectedX, projected.X);
                            minProjectedY = Math.Min(minProjectedY, projected.Y);
                            maxProjectedY = Math.Max(maxProjectedY, projected.Y);
                        }

                        // Рассчитываем точный масштаб на основе границ проекции
                        float projectedRangeX = maxProjectedX - minProjectedX > 0 ? maxProjectedX - minProjectedX : 1.0f;
                        float projectedRangeY = maxProjectedY - minProjectedY > 0 ? maxProjectedY - minProjectedY : 1.0f;

                        float scaleX = drawAreaWidth / projectedRangeX;
                        float scaleY = drawAreaHeight / projectedRangeY;

                        float finalScale = Math.Min(scaleX, scaleY) * 0.9f; // Общий масштаб с небольшим запасом

                         // Рассчитываем смещение для центрирования графика
                        // Центр спроецированного прямоугольника
                        float projectedCenterX = (minProjectedX + maxProjectedX) / 2f;
                        float projectedCenterY = (minProjectedY + maxProjectedY) / 2f;

                        // Центр области рисования
                        float drawAreaCenterX = padding + drawAreaWidth / 2f;
                        float drawAreaCenterY = padding + drawAreaHeight / 2f;

                        // Смещение, чтобы центр проекции совпал с центром области рисования
                        float offsetX = drawAreaCenterX - projectedCenterX * finalScale;
                        float offsetY = drawAreaCenterY - projectedCenterY * finalScale;


                        // Рисуем оси
                        using (var axisPen = new Pen(Color.Black, 2))
                        {
                             // Начальная точка осей (проекция начала координат данных minX, minY, minZ)
                            PointF origin = ProjectPointScaled(new Point3D(minX, minY, minZ), finalScale, offsetX, offsetY, cos30, sin30, minX, minY, minZ);

                            // Ось X (черная)
                            Point3D xAxisEndPoint3D = new Point3D(maxX, minY, minZ);
                            PointF xAxisEnd = ProjectPointScaled(xAxisEndPoint3D, finalScale, offsetX, offsetY, cos30, sin30, minX, minY, minZ);
                            g.DrawLine(axisPen, origin, xAxisEnd);
                            g.DrawString("X", new Font("Arial", 10), Brushes.Black, xAxisEnd.X + 5, xAxisEnd.Y - 15);

                            // Ось Y (черная)
                            Point3D yAxisEndPoint3D = new Point3D(minX, maxY, minZ);
                            PointF yAxisEnd = ProjectPointScaled(yAxisEndPoint3D, finalScale, offsetX, offsetY, cos30, sin30, minX, minY, minZ);
                             g.DrawLine(axisPen, origin, yAxisEnd);
                            g.DrawString("Y", new Font("Arial", 10), Brushes.Black, yAxisEnd.X - 15, yAxisEnd.Y - 5);

                            // Ось Z (черная)
                            Point3D zAxisEndPoint3D = new Point3D(minX, minY, effectiveMaxZForScaling); // Используем ограниченное значение Z для оси
                            PointF zAxisEnd = ProjectPointScaled(zAxisEndPoint3D, finalScale, offsetX, offsetY, cos30, sin30, minX, minY, minZ);
                             g.DrawLine(axisPen, origin, zAxisEnd);
                            g.DrawString("Z", new Font("Arial", 10), Brushes.Black, zAxisEnd.X - 15, zAxisEnd.Y - 15);
                        }

                        // Проецируем и рисуем точки и линии
                        using (var pointPen = new Pen(Color.Red, 3))
                        using (var linePen = new Pen(Color.Blue, 2))
                        {
                            PointF previousPoint = PointF.Empty;
                            bool firstPoint = true;

                            foreach (Point3D point3D in points)
                            {
                                // Используем фактическое значение Z для проекции точки
                                PointF projectedPoint = ProjectPointScaled(point3D, finalScale, offsetX, offsetY, cos30, sin30, minX, minY, minZ);

                                // Рисуем точку
                                g.DrawEllipse(pointPen, projectedPoint.X - 3, projectedPoint.Y - 3, 6, 6);

                                // Рисуем линию к предыдущей точке
                                if (!firstPoint)
                                {
                                    g.DrawLine(linePen, previousPoint, projectedPoint);
                                }

                                previousPoint = projectedPoint;
                                firstPoint = false;
                            }
                        }

                        // Сохраняем старое изображение
                        Image oldImage = pictureBox.Image;

                        // Создаем копию битмапа
                        var newImage = new Bitmap(bmp);

                        // Устанавливаем новое изображение
                        pictureBox.Image = newImage;

                        // Освобождаем старое изображение
                        oldImage?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отрисовке графика: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Вспомогательный метод для изометрической проекции (без масштаба и смещения)
        // Использует стандартную изометрическую проекцию под углом 30 градусов
        private PointF ProjectPointRaw(Point3D point, float minX, float minY, float minZ, double cos30, double sin30)
        {
            // Смещаем точку так, чтобы начало координат данных (minX, minY, minZ) стало 0,0,0
            float x = point.X - minX;
            float y = point.Y - minY;
            float z = point.Z - minZ;

            // Применяем изометрическую проекцию
            float projectedX = (float) ((x - y) * cos30);
            // В стандартной изометрии Z добавляется к Y для вертикального положения
            float projectedY = (float) (z + (x + y) * sin30);

            return new PointF(projectedX, projectedY);
        }

        // Вспомогательный метод для изометрической проекции (с масштабом и смещением)
        private PointF ProjectPointScaled(Point3D point, float scale, float offsetX, float offsetY, double cos30, double sin30, float minX, float minY, float minZ)
        {
            // Смещаем точку так, чтобы начало координат данных (minX, minY, minZ) стало 0,0,0
            float x = point.X - minX;
            float y = point.Y - minY;
            float z = point.Z - minZ;

            // Применяем изометрическую проекцию
            float projectedX_raw = (float) ((x - y) * cos30);
            float projectedY_raw = (float) (z + (x + y) * sin30);

            // Масштабируем и применяем смещение.
            // Y в Graphics растет вниз, поэтому вычитаем из offsetY
            float projectedX = projectedX_raw * scale + offsetX;
            float projectedY = offsetY - projectedY_raw * scale;


            return new PointF(projectedX, projectedY);
        }

        private void UpdateUI(string startTime, string finishTime, string duration, int threadId, int progressValue)
        {
            if (InvokeRequired) {
                Invoke(() => UpdateUI(startTime, finishTime, duration, threadId, progressValue));
                return;
            }

            label8.Text = startTime;
            label9.Text = finishTime;
            label11.Text = duration;
            label13.Text = threadId.ToString();
            try {
                if (progressValue >= 0 && progressValue <= progressBar1.Maximum) {
                    progressBar1.Value = progressValue;
                } else if (progressValue > progressBar1.Maximum) {
                    progressBar1.Value = progressBar1.Maximum;
                }
            } catch (ArgumentOutOfRangeException) {
                progressBar1.Value = progressBar1.Maximum;
            }
        }

        private void StopThreads()
        {
            if (_cancellationTokenSource != null) {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            foreach (Thread thread in _threads) {
                try {
                    if (thread.IsAlive) {
                        thread.Interrupt();
                    }
                } catch (Exception) {
                    // ignored
                }
            }

            _threads.Clear();
        }

        private void ClearFiles()
        {
            try {
                File.WriteAllText(ThreadResultFilePath, string.Empty);
            } catch (Exception ex) {
                MessageBox.Show($"Ошибка при очистке файлов: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopThreads();
            base.OnFormClosing(e);
        }
    }
}