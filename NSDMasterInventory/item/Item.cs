using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NSDMasterInventory.item
{
	public class Item<TS> : ObservableCollection<TS>
	{
		private static string InputLine { get; set; }

		public Item(List<TS> fields)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				Add(fields[i]);
			}

			Inventoried = fields[fields.Count - 1].ToString().ToLower().Equals("true".ToLower());
		}

		public static Item<string> EmptyItem(int size)
		{
			List<string> emptyList = new List<string>();
			for (var i = 0; i < size; i++) emptyList.Add("");

			return new Item<string>(emptyList);
		}

		public static List<string> Parser(string inputLine)
		{
			InputLine = inputLine;
			return new List<string>(inputLine.Split(','));
		}


		public bool Inventoried { get; set; }

		public bool Recycled { get; set; }

		public override string ToString()
		{
			return InputLine;
		}
	}
}