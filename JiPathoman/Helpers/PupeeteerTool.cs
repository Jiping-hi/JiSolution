using PuppeteerSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JiPathoman.Helpers
{

    public class PupeeteerTool
    {
        public static string userAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.39 Safari/537.36";
        private static readonly int chromiumRevision = BrowserFetcher.DefaultRevision;
        public static Browser browser;
        public static Page page;
        public static NavigationOptions ops;
        public static LaunchOptions options;
        public static TimeSpan Delay = TimeSpan.FromSeconds(5);

        static  PupeeteerTool()
        {
            ops = new NavigationOptions()
            {
                Timeout = 120000,
            };

            options = new LaunchOptions
            {
                Headless = true,
                /// pipe options is missing, may be in version 1.1.0
            };
        }

        public static async Task<bool> InitAsync()
        {
            var info = await new BrowserFetcher().DownloadAsync(chromiumRevision);
            Log.Information($"Pupeeteer:{info.Revision}");

            return true;
        }

        public static async Task<bool> DownloadFile(string url, string toFile, string srcFile = null, string pdfFile = null, string scriptText = null)
        {
            if (string.IsNullOrWhiteSpace(toFile)) return false;

            var saveToFile = Path.GetFullPath(toFile);
            if (File.Exists(saveToFile))
            {
                Log.Debug($"skip - {saveToFile}");
                return true;
            }


            Stopwatch sw = new Stopwatch();
            sw.Start();

            Response response;

            using (var browser = await Puppeteer.LaunchAsync(options))
            using (var page = await browser.NewPageAsync())
            {
                await page.SetUserAgentAsync(PupeeteerTool.userAgent);


                try
                {
                    response = await page.GoToAsync(url, ops);
                    if( !string.IsNullOrWhiteSpace(scriptText))
                    {
                        var result = await page.EvaluateExpressionAsync(scriptText);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    return false;
                }

                string content = await page.GetContentAsync();
                await System.IO.File.AppendAllTextAsync(saveToFile, content);

                if (!string.IsNullOrWhiteSpace(srcFile))
                {
                    var source = await response.TextAsync();
                    var srcPath = Path.GetFullPath(srcFile);
                    await System.IO.File.AppendAllTextAsync(srcPath, source);
                }

                if (!string.IsNullOrWhiteSpace(pdfFile))
                {
                    var source = await response.TextAsync();
                    var pdfPath = Path.GetFullPath(pdfFile);
                    await page.PdfAsync(pdfPath);
                }

                await page.CloseAsync();
                await browser.CloseAsync();
            }

            await Task.Delay(10000);
            sw.Stop();
            Log.Verbose($"  done.page {sw.Elapsed.ToString()}");

            return true;
        }

    }
}
