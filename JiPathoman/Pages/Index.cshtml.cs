using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JiPathoman.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JiPathoman.Pages
{
    public class IndexModel : PageModel
    {
        public int UploadCount { get; set; }
        public int DownloadCount { get; set; }
        public int WebpageCount { get; set; }

        public void OnGet()
        {
            UploadCount = FileHelper.FilesCount(Startup.DataUpload);
            DownloadCount = FileHelper.FilesCount(Startup.DataDownload);
            WebpageCount = FileHelper.FilesCount(Startup.DataWebpage);
        }
    }
}
