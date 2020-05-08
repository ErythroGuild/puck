using System;
using System.Collections.Generic;
using System.IO;

namespace Puck {
	class Blocklist {
		static readonly Logger log = Program.GetLogger();
		
		// Static import/export functions.
		public static Blocklist Import(string path)
			{ return new Blocklist(path); }
		public static void Export(Blocklist list, string path)
			{ list.Export(path); }



		// using HashSet to automatically de-dupe entries
		public HashSet<ulong> data;

		// Hide default constructor.
		private Blocklist()
			{ data = new HashSet<ulong>(); }

		// Construct a blocklist from a file.
		public Blocklist(string path) {
			log.Info("Reading blocklist...");
			data = new HashSet<ulong>();

			// Open text file.
			StreamReader file;
			try {
				file = new StreamReader(path);
			} catch (Exception) {
				log.Error("Could not open \"" + path + "\".", 1);
				log.Error("Blocklist not loaded.", 1);
				return;
			}

			// Read into list.
			while (!file.EndOfStream) {
				string line = file.ReadLine() ?? "";
				ulong id = Convert.ToUInt64(line);
				data.Add(id);
			}

			file.Close();
			log.Info("Blocklist import complete.", 1);
		}

		// Export the blocklist to a file.
		public void Export(string path) {
			log.Info("Saving blocklist to file...", 1);
			StreamWriter file = new StreamWriter(path);

			foreach (ulong id in data) {
				file.WriteLine(id.ToString());
			}

			file.Close();
			log.Info("Blocklist saved.", 1);
		}

		// Convenience wrapper functions.
		public void Add(ulong id) {
			data.Add(id);
		}
		public void Remove(ulong id) {
			data.Remove(id);
		}
		public bool Contains(ulong id) {
			return data.Contains(id);
		}
		public void Union(Blocklist list) {
			data.UnionWith(list.data);
		}
		public void Intersect(Blocklist list) {
			data.IntersectWith(list.data);
		}
	}
}
