using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Puck {
	class Settings {
		public DiscordChannel? bulletin;
		public DiscordRole? default_mention;
		public TimeSpan duration;
		public TimeSpan increment;

		public const string mention_none = "none";

		private static readonly TimeSpan duration_default = TimeSpan.FromMinutes(10);
		private static readonly TimeSpan increment_default = TimeSpan.FromMinutes(5);

		public Settings(DiscordChannel? bulletin) :
			this(bulletin, null) { }
		public Settings(DiscordChannel? bulletin, DiscordRole? mention) {
			this.bulletin = bulletin;
			default_mention = mention;
			duration = duration_default;
			increment = increment_default;
		}

		private const string key_separator = ": ";
		private const string key_bulletin	= "bulletin channel";
		private const string key_mention	= "mention role";
		private const string key_duration	= "duration (min)";
		private const string key_increment	= "increment (min)";
		private const int count_keys = 4;

		public static async Task Export(
			string path,
			DiscordClient client,
			Dictionary<ulong, Settings> settings
		) {
			// Copy any uncached settings back into file.
			Dictionary<ulong, Settings> settings_old = await Import(path, client);
			foreach (ulong id_old in settings_old.Keys) {
				if (!settings.ContainsKey(id_old))
					settings.Add(id_old, settings_old[id_old]);
			}

			StreamWriter file = new StreamWriter(path);

			foreach (KeyValuePair<ulong, Settings> pair in settings) {
				file.WriteLine(pair.Key.ToString());

				void Write(string key, string data) {
					file.WriteLine(key + key_separator + data);
				}
				Settings entry = pair.Value;

				Write(key_bulletin,		entry.bulletin?.Id.ToString() ?? "null");
				Write(key_mention,		entry.default_mention?.Id.ToString() ?? "null");
				Write(key_duration,		entry.duration.TotalMinutes.ToString());
				Write(key_increment,	entry.increment.TotalMinutes.ToString());
			}

			file.Close();
		}

		public static async Task<Dictionary<ulong, Settings>> Import(
			string path,
			DiscordClient client
		) {
			Dictionary<ulong, Settings> dict = new Dictionary<ulong, Settings>();

			StreamReader file = new StreamReader(path);
			while (!file.EndOfStream) {
				string line = file.ReadLine() ?? "";

				Settings settings = new Settings(null);
				ulong guild_id = Convert.ToUInt64(line);
				DiscordGuild guild;
				try {
					guild = await client.GetGuildAsync(guild_id);
				} catch (UnauthorizedException) {
					Console.WriteLine(
						"Not authorized to access guild: " +
						guild_id.ToString());
					for (int i = 0; i < count_keys; i++)
						file.ReadLine();	// discard
					continue;
				}

				Dictionary<string, string> lines = new Dictionary<string, string>();
				for (int i = 0; i < count_keys; i++) {
					line = file.ReadLine() ?? "";
					string[] line_parts = line.Split(key_separator, 2);
					lines.Add(line_parts[0], line_parts[1]);
				}

				foreach (string key in lines.Keys) {
					string data = lines[key];
					switch (key) {
					case key_bulletin:
						ulong bulletin_id = Convert.ToUInt64(data);
						settings.bulletin = await client.GetChannelAsync(bulletin_id);
						break;
					case key_mention:
						if (data == "null") {
							settings.default_mention = null;
							break;
						}
						ulong mention_id = Convert.ToUInt64(data);
						settings.default_mention = guild.GetRole(mention_id);
						break;
					case key_duration:
						int duration_min = Convert.ToInt32(data);
						settings.duration = TimeSpan.FromMinutes(duration_min);
						break;
					case key_increment:
						int increment_min = Convert.ToInt32(data);
						settings.increment = TimeSpan.FromMinutes(increment_min);
						break;
					}
				}

				dict.Add(guild_id, settings);
			}
			file.Close();

			return dict;
		}
	}
}
