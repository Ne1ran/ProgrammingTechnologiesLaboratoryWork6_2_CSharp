using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Путь к файлу, куда записываются результаты вычислений из DLL.
        /// </summary>
        private const string ThreadResultFilePath = @"C:\Save\resultsDll.txt";

        /// <summary>
        /// Список активных потоков для вычислений.
        /// Используется для контроля и остановки потоков при необходимости.
        /// </summary>
        private readonly List<Thread> _threads = new();

        /// <summary>
        /// Токен отмены для корректного завершения асинхронных операций.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        private Pen _basePen = null!;
        private Pen _graphicsPen = null!;

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
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Запускает многопоточные вычисления с заданными параметрами.
        /// Создает отдельный поток для каждого набора вычислений.
        /// </summary>
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
            progressBar1.Value = 100;
            progressBar1.Refresh();
            await DrawResults();
        }

        /// <summary>
        /// Читает результаты вычислений из файла и преобразует их в список точек.
        /// Обрабатывает формат: [x][y] z = value, x = value, y = value;
        /// </summary>
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

                            if (float.TryParse(zMatch.Groups[1].Value, out float z) && float.TryParse(xMatch.Groups[1].Value, out float x)
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
            if (points == null || points.Count == 0) {
                return;
            }

            try {
                using var bmp = new Bitmap(pictureBox.Width, pictureBox.Height);
                using Graphics g = Graphics.FromImage(bmp);

                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                DrawingParameters parameters = CalculateDrawingParameters(points, pictureBox);
                DrawAxes(g, parameters, pictureBox);
                PointF[] scaledPoints = Transform3DTo2D(points, parameters);
                DrawPointsAndLines(g, scaledPoints);
                DrawAxisLabels(g, parameters, pictureBox);
                UpdatePictureBox(pictureBox, bmp);
            } catch (Exception ex) {
                MessageBox.Show($"Ошибка при отрисовке графика: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Рассчитывает параметры отрисовки на основе набора точек.
        /// Определяет границы осей, масштабы и центры отрисовки.
        /// </summary>
        private DrawingParameters CalculateDrawingParameters(List<Point3D> points, PictureBox pictureBox)
        {
            var parameters = new DrawingParameters {
                    MinX = MathF.Max(-100, points.Min(p => p.X)),
                    MaxX = MathF.Min(100, points.Max(p => p.X)),
                    MinY = MathF.Max(-100, points.Min(p => p.Y)),
                    MaxY = MathF.Min(100, points.Max(p => p.Y)),
                    MinZ = MathF.Max(-100, points.Min(p => p.Z)),
                    MaxZ = MathF.Min(100, points.Max(p => p.Z))
            };

            float rangeX = parameters.MaxX - parameters.MinX;
            float rangeY = parameters.MaxY - parameters.MinY;
            float rangeZ = parameters.MaxZ - parameters.MinZ;

            float drawWidth = pictureBox.Width - 60;
            float drawHeight = pictureBox.Height - 60;

            float scaleX = rangeX > 0 ? drawWidth / rangeX : 1;
            float scaleY = rangeY > 0 ? drawHeight / rangeY : 1;
            float scaleZ = rangeZ > 0 ? drawHeight / rangeZ : 1;

            parameters.BaseScale = Math.Min(scaleX, scaleY);
            float zScaleMultiplier = 1.5f;
            parameters.FinalZScale = scaleZ * zScaleMultiplier;

            parameters.DrawCenterX = drawWidth / 2f + 30;
            parameters.DrawCenterY = drawHeight / 2f + 30;

            return parameters;
        }

        private void DrawAxes(Graphics g, DrawingParameters parameters, PictureBox pictureBox)
        {
            g.DrawLine(_basePen, 30, parameters.DrawCenterY, pictureBox.Width - 30, parameters.DrawCenterY);
            g.DrawLine(_basePen, parameters.DrawCenterX, pictureBox.Height - 30, parameters.DrawCenterX, 30);
            g.DrawLine(_basePen, parameters.DrawCenterX, parameters.DrawCenterY,
                       parameters.DrawCenterX - parameters.FinalZScale * (parameters.MaxZ - parameters.MinZ) / 2f,
                       parameters.DrawCenterY - parameters.FinalZScale * (parameters.MaxZ - parameters.MinZ) / 2f);
        }

        /// <summary>
        /// Преобразует трехмерные координаты в двухмерные для отображения на экране.
        /// Учитывает масштабирование и центрирование графика.
        /// </summary>
        private PointF[] Transform3DTo2D(List<Point3D> points, DrawingParameters parameters)
        {
            float rangeX = parameters.MaxX - parameters.MinX;
            float rangeY = parameters.MaxY - parameters.MinY;
            float rangeZ = parameters.MaxZ - parameters.MinZ;

            return points.Select(p => {
                             float normalizedX = Math.Clamp(rangeX > 0 ? (p.X - parameters.MinX) / rangeX : 0.5f, -1f, 1f);
                             float normalizedY = Math.Clamp(rangeY > 0 ? (p.Y - parameters.MinY) / rangeY : 0.5f, -1f, 1f);
                             float normalizedZ = Math.Clamp(rangeZ > 0 ? (p.Z - parameters.MinZ) / rangeZ : 0.5f, -1f, 1f);

                             float projectedX = parameters.DrawCenterX + (normalizedX * parameters.BaseScale - normalizedZ * parameters.FinalZScale);
                             float projectedY = parameters.DrawCenterY
                                                - (normalizedY * parameters.BaseScale + normalizedZ * parameters.FinalZScale * 0.5f);

                             return new PointF(projectedX, projectedY);
                         })
                         .ToArray();
        }

        private void DrawPointsAndLines(Graphics g, PointF[] scaledPoints)
        {
            foreach (PointF point in scaledPoints) {
                try {
                    g.DrawEllipse(_graphicsPen, point.X - 1, point.Y - 1, 2, 2);
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            }

            using var linePen = new Pen(Color.Blue, 2);
            for (int i = 0; i < scaledPoints.Length - 1; i++) {
                g.DrawLine(linePen, scaledPoints[i], scaledPoints[i + 1]);
            }
        }

        private void DrawAxisLabels(Graphics g, DrawingParameters parameters, PictureBox pictureBox)
        {
            using var font = new Font("Arial", 10);
            using var brush = new SolidBrush(Color.Black);

            g.DrawString("X", font, brush, pictureBox.Width - 20, parameters.DrawCenterY - 20);
            g.DrawString("Y", font, brush, parameters.DrawCenterX - 20, 20);
            g.DrawString("Z", font, brush, parameters.DrawCenterX - parameters.FinalZScale * (parameters.MaxZ - parameters.MinZ) / 2f - 20,
                         parameters.DrawCenterY - parameters.FinalZScale * (parameters.MaxZ - parameters.MinZ) / 2f - 20);
        }

        private void UpdatePictureBox(PictureBox pictureBox, Bitmap bmp)
        {
            Image oldImage = pictureBox.Image;
            var newImage = new Bitmap(bmp);
            pictureBox.Image = newImage;
            oldImage?.Dispose();
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

        /// <summary>
        /// Останавливает все активные потоки вычислений.
        /// Вызывается при закрытии формы или перезапуске вычислений.
        /// </summary>
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

        /// <summary>
        /// Очищает файл результатов перед новыми вычислениями.
        /// </summary>
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