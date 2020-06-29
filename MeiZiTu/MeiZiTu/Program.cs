using RestSharp;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MeiZiTu
{
    class Program
    {
        private static readonly string basePath = Directory.GetCurrentDirectory();
        

        static void Main(string[] args)
        {
            ProcessAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            Console.WriteLine("抓取完毕，按任意键退出");
            Console.ReadKey();
        }


        static async Task ProcessAsync()
        {
            const string baseUrl = "https://www.mzitu.com/";
            const string userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/22.0.1207.1 Safari/537.1";
            var headers = new Dictionary<string, string>
            {
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8" },
                { "Accept-Encoding", "gzip" },
                { "referer", "https://www.mzitu.com/"}
            };
            var allPageClient = new RestClient
            {
                UserAgent = userAgent,
            };
            allPageClient.AddDefaultHeaders(headers);
            var allPageReq = new RestRequest($"{baseUrl}all", Method.GET);


            var allPgaeRes = await allPageClient.ExecuteAsync(allPageReq);
            if (allPgaeRes.IsSuccessful)
            {
                var allPageContent = allPgaeRes.Content;
                if (!string.IsNullOrEmpty(allPageContent))
                {
                    var allPageDoc = new HtmlAgilityPack.HtmlDocument();
                    allPageDoc.LoadHtml(allPageContent);
                    var allPageDocNode = allPageDoc.DocumentNode;
                    var hrefNodes = allPageDocNode.SelectNodes("//ul[@class='archives']/li/p[@class='url']/a");

                    if (hrefNodes.Count > 0)
                    {
                        var photoPath = $"{basePath}/Photo";

                        #region 创建文件夹
                        try
                        {
                            if (!Directory.Exists(photoPath))
                            {
                                Directory.CreateDirectory(photoPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"创建文件夹【{photoPath}】时发生错误:{ex}");
                            return;
                        }
                        #endregion

                        // 获取所有已存在的目录名
                        var allExistingPhotoDirArr = Directory.GetDirectories(photoPath).Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();
                        var imgInfoList = hrefNodes.Select(hrefNode => new ImageInfo
                        {
                            Title = SafeFileName(hrefNode.InnerText),
                            Url = hrefNode.GetAttributeValue("href", string.Empty),
                        }).Where(x => !string.IsNullOrEmpty(x.Title) && !string.IsNullOrEmpty(x.Url)).ToList();

                        foreach (var imgInfo in imgInfoList)
                        {
                            // 已存在该目录且目录里面有文件->跳过
                            var isPass = false;
                            if (allExistingPhotoDirArr.Contains(imgInfo.Title) && new DirectoryInfo($"{photoPath}/{imgInfo.Title}")?.GetFiles()?.Count() > 0)
                            {
                                isPass = true;
                            }
                            if (!isPass)
                            {
                                var savePah = $"{photoPath}/{imgInfo.Title}";
                                try
                                {
                                    if (!Directory.Exists(savePah))
                                    {
                                        Directory.CreateDirectory(savePah);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"创建文件夹【{savePah}】时发生错误:{ex}");
                                    return;
                                }
                                Console.WriteLine($"正在抓取【{imgInfo.Title}】");

                                var imgClient = new RestClient
                                {
                                    UserAgent = userAgent,
                                };
                                imgClient.AddDefaultHeaders(headers);
                                var imgReq = new RestRequest(imgInfo.Url, Method.GET);
                                var imgRes = await imgClient.ExecuteAsync(imgReq);
                                if (imgRes.IsSuccessful)
                                {
                                    var imgContent = imgRes.Content;
                                    if (!string.IsNullOrEmpty(imgContent))
                                    {
                                        var imgPageDoc = new HtmlAgilityPack.HtmlDocument();
                                        imgPageDoc.LoadHtml(imgContent);
                                        var imgPageDocNode = imgPageDoc.DocumentNode;

                                        var maxPageStr = imgPageDocNode.SelectSingleNode("//div[@class='pagenavi']/a[last()-1]/span")?.InnerText;
                                        if (int.TryParse(maxPageStr, out var maxPage) && maxPage >= 1)
                                        {
                                            for (var currentPage = 1; currentPage <= maxPage; currentPage++)
                                            {
                                                Console.WriteLine($"正在抓取【{imgInfo.Title}】{currentPage}/{maxPage}");
                                                var currentPageImgClient = new RestClient
                                                {
                                                    UserAgent = userAgent,
                                                };
                                                currentPageImgClient.AddDefaultHeaders(headers);
                                                var currentPageImgReq = new RestRequest($"{imgInfo.Url}/{currentPage}", Method.GET);
                                                var currentPageImgRes = await currentPageImgClient.ExecuteAsync(currentPageImgReq);
                                                if (currentPageImgRes.IsSuccessful)
                                                {
                                                    var currentPageImgContent = currentPageImgRes.Content;
                                                    if (!string.IsNullOrEmpty(currentPageImgContent))
                                                    {
                                                        var currentPageImgDoc = new HtmlAgilityPack.HtmlDocument();
                                                        currentPageImgDoc.LoadHtml(imgContent);
                                                        var currentPageImgDocNode = currentPageImgDoc.DocumentNode;

                                                        var imgSrc = currentPageImgDocNode.SelectSingleNode("//div[@class='main-image']/p/a/img").GetAttributeValue("src", string.Empty);
                                                        if (!string.IsNullOrEmpty(imgSrc))
                                                        {
                                                            var imgDownloadClient = new RestClient
                                                            {
                                                                UserAgent = userAgent,
                                                            };
                                                            imgDownloadClient.AddDefaultHeaders(headers);
                                                            using var writer = File.OpenWrite($"{savePah}/{currentPage}.jpg");
                                                            var imgDownloadReq = new RestRequest(imgSrc)
                                                            {
                                                                ResponseWriter = responseStream =>
                                                                {
                                                                    using (responseStream)
                                                                    {
                                                                        responseStream.CopyTo(writer);
                                                                    }
                                                                }
                                                            };
                                                            var response = imgDownloadClient.DownloadData(imgDownloadReq);
                                                        }
                                                    }
                                                }
                                            }
                                            Console.WriteLine($"【{imgInfo.Title}】共{maxPage}抓取完毕");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"【{imgInfo.Title}】抓取失败");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"【{imgInfo.Title}】抓取失败");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"【{imgInfo.Title}】抓取失败");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"【{imgInfo.Title}】跳过");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("未能找到可爬取的图片");
                    }
                }
            }
        }

        private static readonly string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        static string SafeFileName(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                foreach (char c in invalid)
                {
                    fileName = fileName.Replace(c.ToString(), string.Empty);
                }
            }
            return fileName;
        }

        class ImageInfo
        {
            public string Title { get; set; }

            public string Url { get; set; }
        }
    }
}
