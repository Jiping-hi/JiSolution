using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JiPathoman.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace JiPathoman
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public static string DataRoot { get; set; }
        public static string DataLog { get; set; }
        public static string DataUpload { get; set; }
        public static string DataDownload { get; set; }
        public static string DataWebpage { get; set; }
        public static string AppRoot { get; set; }
        public static string WebRoot { get; set; }


        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Init();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSignalR();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseSignalR(routes => { routes.MapHub<JiHub>("/hub"); });

            app.UseMvc();

            AppRoot = env.ContentRootPath;
            WebRoot = env.WebRootPath;
        }

        bool Init()
        {
            InitDirs();
            InitLogs();

            return true;
        }

        bool InitDirs()
        {
            DataRoot = Configuration["Pathoman:DataRootDir"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pathoman-Data");
            DataRoot = Path.GetFullPath(DataRoot);
            FileHelper.CreateFolder(DataRoot);

            DataUpload = Path.GetFullPath(Path.Combine(DataRoot, @"upload\"));
            DataLog = Path.GetFullPath(Path.Combine(DataRoot, @"log\"));
            DataDownload = Path.GetFullPath(Path.Combine(DataRoot, @"download\"));
            DataWebpage = Path.GetFullPath(Path.Combine(DataRoot, @"webpage\"));

            FileHelper.CreateFolder(DataUpload);
            FileHelper.CreateFolder(DataLog);
            FileHelper.CreateFolder(DataDownload);
            FileHelper.CreateFolder(DataWebpage);

            FileHelper.RemoveEmptyFiles(DataLog);


            return true;
        }

        bool InitLogs()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

            var log0 = Path.GetFullPath(Path.Combine(DataLog, $@"{timestamp}-Log0-All.log"));
            var log1 = Path.GetFullPath(Path.Combine(DataLog, $@"{timestamp}-Log1-Verbose.log"));
            var log2 = Path.GetFullPath(Path.Combine(DataLog, $@"{timestamp}-Log2-Debug.log"));
            var log3 = Path.GetFullPath(Path.Combine(DataLog, $@"{timestamp}-Log3-Information.log"));
            var log4 = Path.GetFullPath(Path.Combine(DataLog, $@"{timestamp}-Log4-Warning.log"));
            var log5 = Path.GetFullPath(Path.Combine(DataLog, $@"{timestamp}-Log5-Error.log"));
            var log6 = Path.GetFullPath(Path.Combine(DataLog, $@"{timestamp}-Log6-Fatal.log"));

            var timeSpan = TimeSpan.FromSeconds(1);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                    .WriteTo.File(log0, flushToDiskInterval: timeSpan)
                    .WriteTo.Logger(config => config.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Verbose).WriteTo.File(log1, flushToDiskInterval: timeSpan))
                    .WriteTo.Logger(config => config.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Debug).WriteTo.File(log2, flushToDiskInterval: timeSpan))
                    .WriteTo.Logger(config => config.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information).WriteTo.File(log3, flushToDiskInterval: timeSpan))
                    .WriteTo.Logger(config => config.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning).WriteTo.File(log4, flushToDiskInterval: timeSpan))
                    .WriteTo.Logger(config => config.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error).WriteTo.File(log5, flushToDiskInterval: timeSpan))
                    .WriteTo.Logger(config => config.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Fatal).WriteTo.File(log6, flushToDiskInterval: timeSpan))
                .CreateLogger();
            var timeStamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            Log.Information($"App-Start {timestamp} .........");
            return true;
        }

    }
}
