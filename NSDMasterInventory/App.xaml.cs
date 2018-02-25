using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using NSDMasterInventory.comparers;
using NSDMasterInventory.io;
using NSDMasterInventory.item;
using ZXing;

namespace NSDMasterInventory
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static volatile string MainDirectory;

		public static volatile string DatabaseDirectory;
		public static volatile string BackupsDirectory;
		public static volatile string LogsDirectory;
		public static volatile string BarcodesDirectory;

		public static volatile string InventoryExcelFile;
		public static volatile string ConfigResourcePath = "/config.properties";

		public static volatile bool UpdatingSpreadsheetCurrently = false;
		public static volatile bool UpdatingDatabaseCurrently = false;
		public static volatile bool ProgramFinished = false;

		public static volatile string CheckSumExcel;
		public static volatile string CheckSumDatabase;

		//private static Timer timerExcel;
		//private static Timer timerDatabase;

		protected override void OnStartup(StartupEventArgs e)
		{
			//Console.WriteLine("asdfjkl;!");
			GenerateWorkspace();
			//Console.WriteLine(DatabaseDirectory);

			base.OnStartup(e);
			/*
			while (true)
			{
				if (!(updatingSpreadsheetCurrently || updatingDatabaseCurrently)) break; //or if already synced
				//wait
			}

			programFinished = true;
			backupFiles();*/
		}

		public static void GenerateWorkspace()
		{
			var configUtil = new ConfigUtil();
			configUtil.getAndSetWorkspaceDirectory();

			Directory.CreateDirectory(MainDirectory);
			Directory.CreateDirectory(DatabaseDirectory);
			Directory.CreateDirectory(BackupsDirectory);
			Directory.CreateDirectory(LogsDirectory);
			Directory.CreateDirectory(BarcodesDirectory);
			File.Create(InventoryExcelFile);

			/*
			File xssfFile = new File(INVENTORY_EXCEL_FILE);
			if (xssfFile.length() > 0)
			{
				try
				{
					XSSFWorkbook xssfWorkbook = new XSSFWorkbook(xssfFile);
					if (xssfWorkbook.getNumberOfSheets() < 1) xssfWorkbook.createSheet();
					else
					{
						for (Sheet sheet : xssfWorkbook)
						{
							if (!(file = new File(DATABASE_DIRECTORY + sheet.getSheetName() + ".csv")).exists()) file.createNewFile();
						}
					}

					DatabaseWriter databaseWriter = new DatabaseWriter();
					//databaseWriter.readExcelAllWriteToCSV(Main.DATABASE_DIRECTORY);
				} catch (POIXMLException | IOException e)
				{
					e.printStackTrace();
				}
			}*/
			//checkSumExcel = MD5CheckSum.getMD5Checksum(INVENTORY_EXCEL_FILE);
			//checkSumDatabase = MD5CheckSum.getMD5Checksum(DATABASE_DIRECTORY);
		}

		public static void BackupFiles()
		{
			var now = DateTime.Now;
			var dateString = "MM-dd-yyyy/HH꞉mm꞉ss";
			var finalPath = BackupsDirectory + now.ToString(dateString);
			Directory.CreateDirectory(finalPath);

			DirectoryCopy(DatabaseDirectory, finalPath, true);
			DirectoryCopy(InventoryExcelFile, finalPath, true);
		}

		public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			var dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);

			var dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName)) Directory.CreateDirectory(destDirName);

			// Get the files in the directory and copy them to the new location.
			var files = dir.GetFiles();
			foreach (var file in files)
			{
				var temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, false);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
				foreach (var subdir in dirs)
				{
					var temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
		}

		public static string GetPrefabFromDatabase(string path)
		{
			return File.ReadAllLines(path)[0];
		}

		public static List<string> GetStrings(string path)
		{
			List<string> strings = new List<string>();

			int counter = 0;
			foreach (string line in File.ReadAllLines(path))
			{
				if (counter != 0)
				{
					strings.Add(line);
				}

				counter++;
			}

			return strings;
		}

		public static List<Item<string>> SortList(List<Item<string>> items, List<int> sorts)
		{
			if (sorts.Count == 0)
			{
				return items;
			}

			foreach (int fieldNo in sorts)
			{
				items.Sort(new Sorter(fieldNo));
			}

			return items;
		}

		public static List<Item<string>> GetItems(string file, string prefab)
		{
			List<Item<string>> items = new List<Item<string>>();

			//System.out.println(fileEntry.getAbsolutePath());
			foreach (string line in GetStrings(file))
			{
				//Console.WriteLine(line);
				//System.out.println(StringUtil.countMatches(string, ','));
				Item<string> item = new Item<string>(Item<string>.Parser(line));
				//Console.WriteLine(item);
				items.Add(item);
			}

			//Console.WriteLine(items[4]);

			Directory.CreateDirectory(@"..\resources\prefabs\" + prefab);
			if (!File.Exists(@"..\resources\prefabs\" + prefab + @"\SortBys.properties"))
				File.Create(@"..\resources\prefabs\" + prefab + @"\SortBys.properties");
			Prop prop = new Prop(@"..\resources\prefabs\" + prefab + @"\SortBys.properties");
			//Console.WriteLine(prop.Filename);

			SortedDictionary<int, int> treeMap = new SortedDictionary<int, int>();
			List<int> sorts = new List<int>();

			for (int i = 0; i < prop.Count; i++)
			{
				if (!string.IsNullOrEmpty(prop.Get("field" + i)) && !prop.Get("field" + i).Equals("0"))
				{
					treeMap.Add(int.Parse(prop.Get("field" + i)), i);
				}
			}

			SortedDictionary<int, int>.KeyCollection keySets = treeMap.Keys;

			foreach (int i in keySets)
			{
				sorts.Add(treeMap[i]);
			}

			return SortList(items, sorts);
		}

		/*
		public static List<ExpandoObject> GetExpandos(string path, string prefab)
		{
			Directory.CreateDirectory(@"..\resources\prefabs\" + prefab);
			if (!File.Exists(@"..\resources\prefabs\" + prefab + @"\Fields.properties"))
				File.Create(@"..\resources\prefabs\" + prefab + @"\Fields.properties");
			Prop prop = new Prop(@"..\resources\prefabs\" + prefab + @"\Fields.properties");

			List<ExpandoObject> expandos = new List<ExpandoObject>();
			List<Item<string>> itemList = GetItems(path, prefab);
			//Console.WriteLine(itemList.Count);
			for (int x = 0; x < itemList.Count; x++)
			{
				dynamic expando = new ExpandoObject();
				for (int y = 0; y < prop.Count; y++)
				{
					string field = prop.Get($"field{y}");
					object value = itemList[x][y];
					((IDictionary<string, object>) expando).Add(field, value.ToString());

					//Console.WriteLine("field: " + field);

					((IDictionary<string, object>) expando).TryGetValue(field, out var aString);
					//Console.WriteLine(aString.ToString());
				}

				expandos.Add(expando);
			}

			prop.Save();

			return expandos;
		}
		*/

		public static DataTable ToDataTable(List<Item<String>> items, string prefab)
		{
			var data = items.ToArray();
			if (data.Count() == 0) return null;

			var dt = new DataTable();

			//dt.Columns.Add("#");
			//dt.Columns[0].ReadOnly = true;
			Prop prop = GetProp(prefab, "Fields");

			for (int i = 0; i < prop.Count; i++)
			{
				//Console.WriteLine(prop.Get("field" + i));
				dt.Columns.Add(prop.Get("field" + i));
			}

			//int counter = 1;
			foreach (var item in data)
			{
				DataRow row = dt.NewRow();
				//row[0] = counter;
				for (int i = 0; i < prop.Count; i++)
				{
					row[i] = item[i];
				}

				dt.Rows.Add(row);
				//counter++;
			}

			return dt;
		}

		public static void GenerateBarcodesFromInventory()
		{
			try
			{
				ClearDir(BarcodesDirectory);
			}
			catch (IOException e)
			{
				Console.WriteLine(e.Message);
			}

			try
			{
				//string charset = "UTF-8"; // or "ISO-8859-1"
				BarcodeGenerator.createDMCodes(200, 200);
			}
			catch (WriterException e)
			{
				Console.WriteLine("Could not generate Barcode, WriterException :: " + e.Message);
			}
			catch (IOException e)
			{
				Console.WriteLine("Could not generate Barcode, IOException :: " + e.Message);
			}
		}

		public static bool SearchUsingScanner(string path, string searchQuery)
		{
			var totalList = new List<string>();

			foreach (var theString in File.ReadAllLines(path)) totalList.Add(theString);

			var total = new StringBuilder();
			foreach (var line in totalList) total.Append(line);

			return total.ToString().ToLower().Contains(searchQuery.ToLower());
		}

		public static void MarkInventoried(string path, string textToReplace)
		{
			try
			{
				var input = "";

				foreach (var line in File.ReadAllLines(path))
				{
					if (line.ToLower().Contains(textToReplace.ToLower()))
					{
						var itemStrings = Item<string>.Parser(textToReplace);
						itemStrings[itemStrings.Count - 1] = "TRUE";
						for (var j = 0; j < itemStrings.Count; j++)
							if (j == 0)
								input += itemStrings[j];
							else
								input += "," + itemStrings[j];
					}

					input += line + '\n';
				}

				RemoveEmptyLines(path);

				var file = new StreamWriter(path);
				file.WriteLine(input);
				file.Close();
			}
			catch (Exception)
			{
				Console.WriteLine("Problem reading file.");
			}
		}

		private static void RemoveEmptyLines(string file)
		{
			try
			{
				File.Create(file);
				foreach (var lineOfText in File.ReadAllLines(file))
				{
					var ofText = lineOfText.Trim();
					if (!string.IsNullOrEmpty(lineOfText)) File.AppendText(ofText);
				}
			}
			catch (IOException e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public static void ClearDir(string path)
		{
			var di = new DirectoryInfo(path);
			foreach (var file in di.GetFiles()) file.Delete();

			foreach (var dir in di.GetDirectories()) dir.Delete(true);
		}

		public static Prop GetProp(string prefab, string property)
		{
			Directory.CreateDirectory(@"..\resources\prefabs\" + prefab);
			if (!File.Exists(@"..\resources\prefabs\" + prefab + @"\" + property + ".properties"))
				File.Create(@"..\resources\prefabs\" + prefab + @"\" + property + ".properties");
			Prop prop = new Prop(@"..\resources\prefabs\" + prefab + @"\" + property + ".properties");

			return prop;
		}

		public static Prop GetComboBoxProp(string prefab, int index)
		{
			Directory.CreateDirectory(@"..\resources\prefabs\" + prefab);
			if (!File.Exists(@"..\resources\prefabs\" + prefab + @"\ComboBoxes\comboBox" + index + ".properties"))
				File.Create(@"..\resources\prefabs\" + prefab + @"\ComboBoxes\comboBox" + index + ".properties");
			Prop prop = new Prop(@"..\resources\prefabs\" + prefab + @"\ComboBoxes\comboBox" + index + ".properties");

			return prop;
		}
	}
}