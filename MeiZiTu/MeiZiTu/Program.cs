using System;
using System.IO;
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


        static Task ProcessAsync()
        {
            const string baseUrl = "https://www.mzitu.com/";


        }
    }
}
