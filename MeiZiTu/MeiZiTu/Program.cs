using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

            var client = new RestClient(baseUrl);

            var request = new RestRequest("all", Method.GET);

            
                var res = await client.ExecuteAsync(request);
            if (res.IsSuccessful)
            {
                var content = res.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    var doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(content);
                    var docNode = doc.DocumentNode;
                    var hrefNodes = docNode.SelectNodes("//ul[@class='archives']/li/p[@class='url']/a");

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
                        var imgInfoList = hrefNodes.Select(hrefNode => new ImageInfo{ 
                            Title= hrefNode.InnerText,
                             Url = hrefNode.GetAttributeValue("href", string.Empty),
                        }).Where(x=>!string.IsNullOrEmpty(x.Title) && !string.IsNullOrEmpty(x.Url)).ToList();

                        // 已存在该目录且目录里面有文件->跳过
                        imgInfoList.AsParallel().ForAll(async imgInfo =>
                        {
                            var isPass = false;
                            if (allExistingPhotoDirArr.Contains(imgInfo.Title) && new DirectoryInfo($"{photoPath}/{imgInfo.Title}")?.GetFiles()?.Count()>0)
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

                                var imgClient = new RestClient();
                                var imgReq = new RestRequest(imgInfo.Url, Method.GET);
                                var imgRes = await imgClient.ExecuteAsync(imgReq);
                                if (imgRes.IsSuccessful)
                                {
                                    var imgContent = imgRes.Content;
                                    if (!string.IsNullOrEmpty(imgContent))
                                    { 
                                        
                                    }
                                }
                            }

                        });
                    }
                    else
                    {
                        Console.WriteLine("未能找到可爬取的图片");
                    }
                }
            }
        }

        class ImageInfo
        { 
            public string Title { get; set; }

            public string Url { get; set; }
        }
    }
}
