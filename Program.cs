using MeiyounaiseOsu.Core;

namespace MeiyounaiseOsu
{
    class Program
    {
        private static void Main()
        {
            using var b = new Bot();
            b.RunAsync().Wait();
        }
    }
}