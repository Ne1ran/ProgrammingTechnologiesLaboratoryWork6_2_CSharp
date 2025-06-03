using System;
using System.Windows.Forms;

namespace ProgrammingTechnologiesLaboratoryWork6_2_CSharp
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
} 