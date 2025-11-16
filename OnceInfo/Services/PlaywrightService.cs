using HtmlAgilityPack;
using Microsoft.Playwright;
using System.Diagnostics;

namespace OnceInfo.Services
{
    public static class PlaywrightService
    {
        public static void EnsurePlaywrightBrowsers()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string playwrightRoot = Path.Combine(userProfile, "AppData", "Local", "ms-playwright");

            // 1) ¿Ya existen navegadores en la ruta REAL?
            if (Directory.Exists(playwrightRoot) &&
                Directory.EnumerateDirectories(playwrightRoot).Any())
            {
                Console.WriteLine("Playwright ya tiene navegadores instalados.");
                return;
            }

            Console.WriteLine("Instalando navegadores de Playwright...");

            // 2) Instalar usando API interna (sin PowerShell)
            Microsoft.Playwright.Program.Main(new[] { "install" });

            // 3) Verificación
            if (!Directory.Exists(playwrightRoot) ||
                !Directory.EnumerateDirectories(playwrightRoot).Any())
            {
                throw new Exception("Playwright no pudo instalar los navegadores.");
            }

            Console.WriteLine("Navegadores instalados correctamente.");
        }

        public static async Task<HtmlDocument> GetHtmlDocumentAsync(string url, bool headless = true, int timeout = 60000)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = headless
            });

            var page = await browser.NewPageAsync();
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = timeout });
            var content = await page.ContentAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            return doc;
        }
    }
}