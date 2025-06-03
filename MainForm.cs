using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using ProgrammingTechnologiesLaboratoryWork6_3;

namespace ProgrammingTechnologiesLaboratoryWork6
{
    public partial class MainForm : Form
    {
        private readonly List<Thread> _threads = new();
        private readonly ThreadManager _mfcThreadManager;
        private bool _isUseMfc;
        private CancellationTokenSource _cancellationTokenSource;

        public MainForm()
        {
            InitializeComponent();
            _cancellationTokenSource = new();

            progressBar1.Maximum = 100;

            _mfcThreadManager = new();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Button2_Click(object sender, EventArgs e)
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

            if (_isUseMfc) {
                var mfcThread = new Thread(() => { _mfcThreadManager.RunCalculations(threadsCount, minX, maxX, minY, maxY, sizeOfArrays); });
                _threads.Add(mfcThread);
                mfcThread.Start();
            } else {
                for (int i = 0; i < threadsCount; i++) {
                    int threadId = i + 1;
                    var calculator =
                            new ProgrammingTechnologiesLaboratoryWork6_1.ThreadCalculator(threadId, minX, maxX, minY, maxY, sizeOfArrays,
                                                                                          UpdateUI);

                    var thread = new Thread(calculator.ThreadDoWork);
                    _threads.Add(thread);
                    thread.Start();
                }
            }
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
                File.WriteAllText(@"C:\Save\results.txt", string.Empty);
                File.WriteAllText(@"C:\Save\resultsDll.txt", string.Empty);
            } catch (Exception ex) {
                MessageBox.Show($"Ошибка при очистке файлов: {ex.Message}");
            }
        }

        private void RadioButton1_Click(object sender, EventArgs e)
        {
            _isUseMfc = !_isUseMfc;
            radioButton1.Checked = _isUseMfc;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopThreads();
            base.OnFormClosing(e);
        }
    }
}