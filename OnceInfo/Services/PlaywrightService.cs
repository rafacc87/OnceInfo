using HtmlAgilityPack;
using Microsoft.Playwright;
using System.Diagnostics;

namespace OnceInfo.Services
{
    public static class PlaywrightService
    {
        public static async Task EnsurePlaywrightBrowsersAsync()
        {
            try
            {
                using var playwright = await Playwright.CreateAsync();
            }
            catch (PlaywrightException)
            {
                Console.WriteLine("> No se han encontrado navegadores Playwright. Descargando navegadores...");
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = @"-ExecutionPolicy Bypass -File .\playwright.ps1 install",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
                Console.WriteLine("> Descarga completada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"> Error comprobando Playwright: {ex.Message}");
                throw;
            }
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