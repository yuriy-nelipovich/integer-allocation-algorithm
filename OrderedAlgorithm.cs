using System;
using System.Collections.Generic;
using System.Linq;

namespace IntegerAllocation
{
	public class OrderedAlgorithm
	{
		readonly Bag _input;

		readonly IList<Variable> _varsOrdered;

		readonly IList<int> _varsValues;
	
		public OrderedAlgorithm(IList<Variable> varsOrdered, Bag input)
		{
			_input = input.Clone();
			_varsOrdered = varsOrdered;
			_varsValues = new int[varsOrdered.Count];
		}
		
		public Tuple<ICollection<VariableWithValue>, Bag> Calculate()
		{
			Calculate(new[] { _input }, 0);
			return
				Tuple.Create(
					(ICollection<VariableWithValue>)_varsOrdered
						.Select((_, i) => new VariableWithValue(_, _varsValues[i]))
						.ToArray(),
					_input
				);
		}
		
		int Calculate(ICollection<Bag> bags, int varIndexToExclude)
		{
			if (varIndexToExclude >= _varsOrdered.Count)
			{
			    if (bags.Any(_ => _.Sums.Count() > 1))
			        throw new ArgumentException("Threre is no solution because input Bag contains duplicate sums");
			    return 0;
			}
			
			var varToExclude = _varsOrdered[varIndexToExclude];
			var pairs = bags.Select(_ => new { Original = _, Split = SpliBag(_, varToExclude).ToArray() }).ToArray();
			
			var nextLevelBags = pairs.SelectMany(_ => _.Split).ToArray();
			var nextLevelVarValue = Calculate(nextLevelBags, varIndexToExclude + 1);

			// now the next level bags contain calculated sums
			
			int varValue;
			if (nextLevelVarValue > 0)
			{
				var excluedValues = pairs.SelectMany(_ => GetExcludedValues(_.Split, nextLevelVarValue)).ToList();
				excluedValues.Sort();
				varValue = FindFirstGap(excluedValues, nextLevelVarValue);
			}
			else
			{
				varValue = 1;
			}
			_varsValues[varIndexToExclude] = varValue;
			
			// calculate sums of current bags
			
			foreach (var pair in pairs)
				for (int i = 0; i < pair.Split.Length; i++)
				{
					IncrementSums(pair.Original, pair.Split[i], varValue * i);
				}
			
			return varValue;
		}

		/// <summary>
		/// Splits Bag by excluding the variable from its sums
		/// </summary>
		IEnumerable<Bag> SpliBag(Bag input, Variable varToExclude)
		{
			var remaining = input.Sums;
			
			do
			{
				var current = remaining
					.Where(sum => !sum.Summands.Contains(varToExclude))
					.Select(sum => new Sum(sum.Summands, sum.Id))
					.ToArray();
				
				yield return new Bag(current);
				
				remaining = remaining
					.Where(sum => sum.Summands.Contains(varToExclude))
					.Select(sum => new Sum(ExcludeOneItem(sum.Summands, m => m == varToExclude).ToArray(), sum.Id))
					.ToArray();
			}
			while (remaining.Count > 0);
		}

		/// <summary>
		/// This method is expensive, O(N^2)
		/// </summary>
		IEnumerable<int> GetExcludedValues(IEnumerable<Bag> split, int nextLevelVarValue)
		{
			if (!split.Any())
				yield break;
			
			var head = split.First();
			var tail = split.Skip(1);
			foreach (var second in tail.Select((_, i) => new { Bag = _, Index = i }))
			{
				var result = GetExcludedValues(head, second.Bag, nextLevelVarValue, second.Index + 1);
				foreach (var r in result)
				{
					yield return r;
				}
			}
			
			var tailResult = GetExcludedValues(tail, nextLevelVarValue);
			foreach (var r in tailResult)
			{
				yield return r;
			}
		}

		/// <summary>
		/// This method is expensive, O(M^2)
		/// </summary>
		IEnumerable<int> GetExcludedValues(Bag head, Bag update, int nextLevelVarValue, int multiplier)
		{
			var headValues = head.Sums.Select(_ => _.CalculatedValue);
			var updateValues = update.Sums.Select(_ => _.CalculatedValue);
			foreach (var h in headValues)
				foreach (var u in updateValues)
				{
					var r = h - u;
					if (r <= 0 || r % multiplier != 0)
						continue;
					var rDivided = r / multiplier;
					if (rDivided >= nextLevelVarValue)
						yield return r;
				}
		}

		int FindFirstGap(IEnumerable<int> excluedValues, int minValidValue)
		{
			var lastValidValue = minValidValue;
			foreach (var excl in excluedValues)
			{
				if (excl > lastValidValue)
					break;
				lastValidValue = excl + 1;
			}
			return lastValidValue;
		}

		void IncrementSums(Bag original, Bag head, int valueToAdd)
		{
			foreach (var sum in head.Sums)
			{
				original.GetSumById(sum.Id).CalculatedValue = sum.CalculatedValue + valueToAdd;
			}
		}
		
		IEnumerable<T> ExcludeOneItem<T>(IEnumerable<T> source, Func<T, bool> predicate)
		{
			var hasBeenExcluded = false;
			foreach (var i in source)
			{
				if (hasBeenExcluded)
					yield return i;
				else if (predicate(i))
					hasBeenExcluded = true;
				else
					yield return i;
			}
		}
	}
	
	
	public struct Variable
	{
		public Variable(string name) : this()
		{
			Name = name;
		}
		
		public string Name { get; private set; }
		
		#region Equals and GetHashCode implementation
		
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return (obj is Variable) && Equals((Variable)obj);
		}

		public bool Equals(Variable other)
		{
			return this.Name == other.Name;
		}

		public static bool operator ==(Variable lhs, Variable rhs) {
			return lhs.Equals(rhs);
		}

		public static bool operator !=(Variable lhs, Variable rhs) {
			return !(lhs == rhs);
		}

		#endregion

		public override string ToString()
		{
			return Name;
		}
	}
	
	public class Sum
	{
		public ICollection<Variable> Summands { get; private set; }
		
		public string Id { get; private set; }
		
		public int CalculatedValue { get; set; }
		
		public bool IsEmpty
		{
			get { return Summands.Count == 0; }
		}
		
		public Sum(ICollection<Variable> summands, string id)
		{
			Summands = summands;
			Id = id;
		}

		public override string ToString()
		{
			return string.Format("{0}={1}",
					string.Join("+", Summands.Select(_ => _.ToString())),
					CalculatedValue);
		}
	}	
	
	public class Bag
	{
		public ICollection<Sum> Sums { get; private set; }
		
		public Bag(ICollection<Sum> sums)
		{
			Sums = sums;
		}
		
		public Sum GetSumById(string id)
		{
			return Sums.First(_ => _.Id == id);
		}

		public override string ToString()
		{
			return string.Format("{{{0}}}", string.Join("; ", Sums.Select(_ => _.ToString())));
		}

		public Bag Clone()
		{
			return new Bag(Sums.Select(_ => new Sum(_.Summands, _.Id)).ToArray());
		}
	}

	public struct VariableWithValue
	{
		public Variable Var { get; private set; }

		public int Value { get; private set; }

		public VariableWithValue(Variable @var, int value) : this()
		{
			Var = var;
			Value = value;
		}

		public override string ToString()
		{
			return string.Format("{0}={1}", Var, Value);
		}
	}
}
