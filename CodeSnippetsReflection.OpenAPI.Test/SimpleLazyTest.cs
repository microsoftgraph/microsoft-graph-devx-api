using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class SimpleLazyTest
    {
        private static Random random = new Random();

        // return a random string or throw an exception
        private String chaoticService()
        {
            if (random.Next() % 2 == 0 ) throw new Exception("Chaos just because");

            Thread.Sleep(300);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [Fact]
        public async Task NoExceptionCaching()
        {
            var responseProvider = new SimpleLazy<String>(() => chaoticService());

            String result = default;

            Parallel.For(0, 15, t =>
            {
                for (int i = 0; i < 10; i++)
                {

                    if(result != default) // once value is set value should not be changed
                    {
                        if (result != responseProvider.Value)
                        {
                            Assert.Equal(result , responseProvider.Value);
                        }
                    }
                    else
                    {
                        try
                        {
                            result = responseProvider.Value;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            });
        }
    }
}
