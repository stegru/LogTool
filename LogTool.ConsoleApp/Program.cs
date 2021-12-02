using System.Linq;
using LogTool.LogProcessor.Parser;

namespace LogTool.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {

            LogParser.ProcessPath(@"C:\local-src\LogTool\TestData").ToList();

        }
    }
}