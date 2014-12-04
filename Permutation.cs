using System.Collections.Generic;
using System.Linq;

namespace IntegerAllocation
{
	public class Permutation
	{
		public static IEnumerable<IList<T>> GetAll<T>(ICollection<T> source)
		{
			return GetAll(source, new List<T>());
		}

		private static IEnumerable<IList<T>> GetAll<T>(ICollection<T> source, IList<T> head)
		{
			if (head.Count == source.Count)
			{
				yield return head;
				yield break;
			}

			var tail = source.Except(head).ToArray();
			foreach (var el in tail)
			{
				head.Add(el);

				foreach (var sub in GetAll(source, head))
				{
					yield return sub;
				}

				head.RemoveAt(head.Count - 1);
			}
		}
	}
}