using StackExchange.Redis;
using System;

namespace RedisProj
{
    class Program
    {
        static void Main(string[] args)
        {
            RedisApp app = new RedisApp();
            Console.WriteLine("Starting to connect.");
            app.Connect();
            
        }
    }
}
