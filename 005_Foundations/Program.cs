using System;
using System.Collections.Generic;

namespace Foundations
{
    public class Program
    {
        public static void Main()
        {
            var classList = new List<ICalculation> { new Moltiplication(), new Addition() };
            foreach (var c in classList)
            {
                Console.WriteLine($"{c.Description}: {c.Calc(2, 4)}");
            }
            Console.ReadKey();
        }
    }

    public interface ICalculation
    {
        string Description { get; }
        decimal Calc(decimal a, decimal b);
    }

    public class Moltiplication : ICalculation
    {
        public string Description { get { return "Moltiplication of two numbers"; } }

        public decimal Calc(decimal a, decimal b)
        {
            return a * b;
        }
    }

    public class Addition : ICalculation
    {
        public string Description { get { return "Addition of two numbers"; } }

        public decimal Calc(decimal a, decimal b)
        {
            return a + b;
        }
    }
}
