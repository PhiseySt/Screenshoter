using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Screenshoter
{
    class Program
    {
        #region DllImport

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SwHide = 0;


        #endregion

        /// <summary>
        /// Как часто делать скриншот, в секундах
        /// </summary>
        static int Interval { get; set; }

        /// <summary>
        /// Размер хранилища скриншотов, в MB
        /// </summary>
        static int Limit { get; set; }

        /// <summary>
        /// Путь к хранилищу скриншотов, пример C:\temp
        /// </summary>
        static string _path { get; set; }

        static void Main(string[] args)
        {
            Log.Instance.Info($"started");

            var handle = GetConsoleWindow();
            ShowWindow(handle, SwHide);

            // Чтение конфигурации
            ReadSettings();

            // Запуск бесконечной работы скриншотов 
            while (true)
            {
                // Проверить хранилище
                CheckStorage();

                // Выполнить скриншот
                DoScreen();

                // Подождать интервал времени
                Thread.Sleep(Interval * 1000);
            }
        }

        /// <summary>
        /// Чтение настроек из файла конфигурации
        /// </summary>
        static void ReadSettings()
        {
            Interval = 10;
            if (Properties.Settings.Default.interval > 0) Interval = Properties.Settings.Default.interval;
            Log.Instance.Info($"set interval = {Interval} sec");

            Limit = 20;
            if (Properties.Settings.Default.limit > 0) Limit = Properties.Settings.Default.limit;
            Log.Instance.Info($"set storage = {Limit} Mb");

            _path = @"C:\temp";
            if (!string.IsNullOrEmpty(Properties.Settings.Default.path)) _path = Properties.Settings.Default.path;
            Log.Instance.Info($"set path = {_path}");
        }

        /// <summary>
        /// Проверка доступного места в хранилище
        /// </summary>
        static void CheckStorage()
        {
            var currentSize = StorageSize();

            if (currentSize > Limit)
            {
                // Сколько нужно очистить MB
                var totalToTrash = currentSize - Limit;

                // Очистить необходимое кол-во KB
                StorageClear(totalToTrash * 1024);
            }
        }

        /// <summary>
        /// Заполненность хранилища, в MB
        /// </summary>
        /// <returns></returns>
        static long StorageSize()
        {
            long i = 0;

            try
            {
                DirectoryInfo directory = new DirectoryInfo(_path);
                FileInfo[] files = directory.GetFiles();

                foreach (FileInfo file in files)
                {
                    i += file.Length;
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error(3, ex.Message);
                return Limit;
            }

            return i / (1024 * 1024);
        }

        /// <summary>
        /// Очистка хранилища
        /// </summary>
        /// <param name="sizeKb"></param>
        private static void StorageClear(long sizeKb)
        {
            try
            {
                Log.Instance.Info($"clear = {sizeKb} Kb");

                DirectoryInfo directory = new DirectoryInfo(_path);
                FileInfo[] files = directory.GetFiles().OrderBy(f => f.CreationTime).ToArray();

                foreach (FileInfo file in files)
                {
                    var size = file.Length / 1024;
                    File.Delete(file.FullName);
                    sizeKb -= size;
                    if (sizeKb <= 0) break;
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error(2, ex.Message);
            }
        }

        /// <summary>
        /// Создание скриншота
        /// </summary>
        private static void DoScreen()
        {
            try
            {
                Bitmap printscreen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                Graphics graphics = Graphics.FromImage(printscreen);
                graphics.CopyFromScreen(0, 0, 0, 0, printscreen.Size);
                printscreen.Save(Path.Combine(_path, GetFileName()), System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                Log.Instance.Error(1, ex.Message);
            }
        }

        /// <summary>
        /// Имя файла создаваемого скриншота
        /// </summary>
        /// <returns></returns>
        static string GetFileName()
        {
            var time = DateTime.Now;
            return $"{time:yyyy_MM_dd__HH_mm_ss}.png";
        }
    }
}
