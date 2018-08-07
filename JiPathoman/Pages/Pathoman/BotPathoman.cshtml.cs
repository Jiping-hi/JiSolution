using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JiPathoman.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using Serilog;

namespace JiPathoman.Pages.Pathoman
{
    public class BotPathomanModel : PageModel
    {
        private readonly IHubContext<JiHub> hub;
        private readonly IConfiguration configuration;

        public string Message { get; set; }

        [BindProperty]
        public string filenames { get; set; }
        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Population_Control { get; set; }

        [BindProperty]
        public int BotDelay { get; set; }

        public BotPathomanModel(IHubContext<JiHub> hub, IConfiguration configuration)
        {
            this.hub = hub;
            this.configuration = configuration;
            BotDelay = 15;
            Population_Control = configuration["Pathoman:Population_Control"] ?? "East Asian";
            Email = configuration["Pathoman:Email"] ?? "dianyingmi@gmail.com";

        }

        public void OnGet()
        {
            filenames = string.Join(Environment.NewLine, FileHelper.FileList(Startup.DataUpload));
        }
        public void OnPost()
        {
            Log.Debug("OnPost");
            Message = $"starting ";
            var task = OnPostAsync();

        }

        private async Task OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(filenames))
            {
                this.Message = $"Empty filename list!";
                Log.Warning(this.Message);
                return;
            }

            var filename_list = filenames.Split(Environment.NewLine);
            var url = "https://pathoman.mskcc.org/";
            var scriptText = @"";
            var dir = $@"{Startup.DataWebpage}";
            var cnt = 0;

            Log.Debug(Message);

            await hub.Clients.All.SendAsync("ReceiveMessage", Message);

            foreach (var item in filename_list)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var saveToFile = $@"{dir}\{timestamp}-result.htm";
                var pdfFile = $@"{dir}\{timestamp}-result.pdf";
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }
                if (System.IO.File.Exists(item) == false)
                {
                    Log.Warning($"Not exist {item}");
                    continue;
                }

                var result = await postBackAsync(url, scriptText, saveToFile, uploadFile:item, pdfFile:pdfFile);
                Message = $"ok - submitted {item} ";
                Log.Debug(Message);
                await Task.Delay(1000 * BotDelay);
                await hub.Clients.All.SendAsync("ReceiveMessage", Message);
                cnt++;
            }
            Message = $"Done, successfully submitted files({cnt})";

            await hub.Clients.All.SendAsync("ReceiveMessage", Message);
        }

        private async Task<int> postBackAsync(string url, string scriptText, string saveToFile, string uploadFile = null, string pdfFile = null)
        {
            var result = 0;

            await PupeeteerTool.InitAsync();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Response response;
            var ops = new NavigationOptions()
            {
                Timeout = 120000,
            };

            var options = new LaunchOptions
            {
                Headless = true,
                /// pipe options is missing, may be in version 1.1.0
            };
            using (var browser = await Puppeteer.LaunchAsync(options))
            using (var page = await browser.NewPageAsync())
            {
                await page.SetUserAgentAsync(PupeeteerTool.userAgent);

                try
                {
                    response = await page.GoToAsync(url, ops);
                    if (!string.IsNullOrWhiteSpace(scriptText))
                    {
                        var eval_result = await page.EvaluateExpressionAsync(scriptText);
                    }
                    await page.TypeAsync("form[action=\"/pathoMANUpload\"] input[name=\"email\"]", Email);
                    await page.TypeAsync("form[action=\"/pathoMANUpload\"] select[name=\"PopulationControl\"]", Population_Control);
                    var fileInput = await page.QuerySelectorAsync("form[action=\"/pathoMANUpload\"] input[type=\"file\"]");
                    await fileInput.UploadFileAsync(uploadFile);
                    var btn = await page.QuerySelectorAsync("form[action=\"/pathoMANUpload\"] input[type=\"submit\"]");
                    Log.Information($"Email={Email}, Population_Control={Population_Control}, uploadFile={uploadFile}, ");

                    await btn.ClickAsync();
                    var navOptions = new NavigationOptions()
                    {
                        Timeout = 5000,
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    };
                    await page.WaitForNavigationAsync(navOptions);

                    string content = await page.GetContentAsync();
                    await System.IO.File.AppendAllTextAsync(saveToFile, content);

                    if (!string.IsNullOrWhiteSpace(pdfFile))
                    {
                        var pdfPath = Path.GetFullPath(pdfFile);
                        await page.PdfAsync(pdfPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    return -1;
                }

                await page.CloseAsync();
                await browser.CloseAsync();
            }

            sw.Stop();
            Log.Verbose($"  done.page {sw.Elapsed.ToString()}, see {saveToFile}");
            Message += $"  result: saved to {saveToFile}";

            result = 100;
            return result;
        }

    }
}