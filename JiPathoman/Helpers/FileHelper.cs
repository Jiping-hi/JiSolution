using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace JiPathoman.Helpers
{
    public class FileHelper
    {
        public static bool CreateFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return false;

            var dir = Directory.CreateDirectory(Path.GetFullPath(folderPath));
            return dir.Exists;
        }

        public static async Task<bool> DownloadFile(string url, string saveToFile = null)
        {
            var fileInfo = new FileInfo(saveToFile);
            if (fileInfo.Exists)
            {
                return true;
            }
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(120);
            var uri = new Uri(url);

            try
            {
                var responseMessage = await client.GetAsync(uri);

                if (responseMessage.IsSuccessStatusCode)
                {
                    using (var fs = new FileStream(saveToFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await responseMessage.Content.CopyToAsync(fs);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return false;
            }

            return false;
        }

        public static int FilesCount(string dirPath, string searchPattern = null)
        {
            var watch = new Stopwatch();
            watch.Start();
            int cnt;
            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                cnt = Directory.EnumerateFiles(dirPath).Count(); //cnt = Directory.GetFiles(dirPath, searchPattern).Length;
            }
            else
            {
                cnt = Directory.EnumerateFiles(dirPath, searchPattern).Count();
            }
            watch.Stop();
            Log.Verbose($"EnumerateFiles: {cnt}, {watch.Elapsed.ToString()}");

            return cnt;
        }

        public static List<string> FileList(string dirPath, string searchPattern = null)
        {
            List<string> result;
            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                result = Directory.EnumerateFiles(dirPath).ToList(); //cnt = Directory.GetFiles(dirPath, searchPattern).Length;
            }
            else
            {
                result = Directory.EnumerateFiles(dirPath, searchPattern).ToList();
            }

            return result ?? new List<string>();
        }

        public static int RemoveEmptyFiles(string dirPath)
        {
            int result = 0;
            var di = new DirectoryInfo(Startup.DataLog);
            var files = di.GetFiles();

            foreach (var item in files)
            {
                if(item.Length == 0)
                {
                    File.Delete(item.FullName);
                    result++;
                }
            }
            Log.Verbose($"Removed empty log files: {result}");
            return result;
        }
    }
}
