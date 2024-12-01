using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;

namespace FileAnalyzeSender
{
    class Program
    {
        // Импортируем функцию для скрытия консоли
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;

        static void Main(string[] args)
        {
            // Скрываем консольное окно
            IntPtr handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            // Проверяем, что путь к файлу передан в аргументах командной строки
            if (args.Length == 0)
            {
                return;
            }

            string filePath = args[0];

            if (!File.Exists(filePath))
            {
                return;
            }

            // Читаем URL и prompt из внешнего файла конфигурации
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            IConfiguration config = builder.Build();
            string url = config["url"];
            string prompt = config["prompt"];

            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            EdgeDriver driver = null;

            try
            {
                // Настройки браузера Edge с использованием отдельного профиля
                var options = new EdgeOptions();
                string userProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "FileAnalyzeProfile");
                if (!Directory.Exists(userProfilePath))
                {
                    Directory.CreateDirectory(userProfilePath);
                }
                options.AddArgument($"--user-data-dir={userProfilePath}");

                // Создаем экземпляр EdgeDriver
                driver = new EdgeDriver(options);

                // Открываем URL для анализа
                driver.Navigate().GoToUrl(url);

                // Устанавливаем значение для скрытого поля prompt с помощью JavaScript
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("document.getElementById('prompt').value = arguments[0];", prompt);

                // Находим поле для загрузки файла и загружаем файл
                var fileInput = driver.FindElement(By.Id("image"));
                fileInput.SendKeys(filePath);

                // Находим кнопку для начала анализа и нажимаем на нее
                var analyzeButton = driver.FindElement(By.XPath("//button[@onclick='analyzeImage()']"));
                analyzeButton.Click();

                // Ждем завершения анализа (например, появления результата на странице)
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(drv => drv.FindElement(By.Id("result")).Displayed);
            }
            catch (Exception)
            {
                // Ошибка обрабатывается молча
            }
            finally
            {
                // Завершаем программу после выполнения действий
                Environment.Exit(0);
            }
        }
    }
}
