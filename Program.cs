using System;
using System.Collections.Generic;
using System.Linq;

namespace IntegerAllocation
{
	class Program
	{
		public static void Main(string[] args)
		{
			var a = new Variable("a");
			var b = new Variable("b");
			var c = new Variable("c");
			var d = new Variable("d");
			
			var input = new Bag(
				new[]
				{
					new Sum(new[] { a, b }, "sum1"),
					new Sum(new[] { b, c }, "sum2"),
					new Sum(new[] { c, d }, "sum3"),
					new Sum(new[] { a, a, d }, "sum4"),
					new Sum(new[] { b }, "sum5"),
				}
			);

			var allVars = new[] {a, b, c, d};

			var orderedSolutions = Permutation.GetAll(allVars)
				.Select(orderedVars => new OrderedAlgorithm(orderedVars, input).Calculate())
				.ToArray();
			
			var minimalSolution = orderedSolutions
				.Select(_ => new { Solution = _, MaxSum = _.Item2.Sums.Max(sum => sum.CalculatedValue) })
				.OrderBy(_ => _.MaxSum)
				.First();
			PrintSolution(minimalSolution.Solution, minimalSolution.MaxSum);
		}

		private static void PrintSolution(Tuple<ICollection<VariableWithValue>, Bag> minimalSolution, int maxSum)
		{
			Console.WriteLine(string.Join(", ", minimalSolution.Item1.Select(_ => _.ToString())));
			Console.WriteLine(minimalSolution.Item2.ToString());
			Console.WriteLine("max={0}", maxSum);
		}
	}
}