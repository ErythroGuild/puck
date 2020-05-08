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
		public MentionRole? default_mention;
		public TimeSpan duration;
		public TimeSpan increment;

		public const string mention_none = "none";

		public Settings(DiscordChannel? bulletin) :
			this(bulletin, null) { }
		public Settings(DiscordChannel? bulletin, MentionRole? mention) {
			this.bulletin = bulletin;
			default_mention = mention;
			duration = duration_default;
			increment = increment_default;
		}

		static readonly Logger log = Program.GetLogger();

		static readonly TimeSpan duration_default = TimeSpan.FromMinutes(10);
		static readonly TimeSpan increment_default = TimeSpan.FromMinutes(5);

		const string key_separator = ": ";
		const string key_bulletin	= "bulletin channel";
		const string key_mention	= "mention role";
		const string key_duration	= "duration (min)";
		const string key_increment	= "increment (min)";
		const int count_keys = 4;

		public static async Task Export(
			string path,
			DiscordClient client,
			Dictionary<ulong, Settings> settings,
			bool do_keep_cache = true
		) {
			// Copy any uncached settings back into file.
			if (do_keep_cache) {
				Dictionary<ulong, Settings> settings_old = await Import(path, client);
				foreach (ulong id_old in settings_old.Keys) {
					if (!settings.ContainsKey(id_old))
						settings.Add(id_old, settings_old[id_old]);
				}
			}

			log.Info("Exporting settings...");
			StreamWriter file = new StreamWriter(path);

			foreach (KeyValuePair<ulong, Settings> pair in settings) {
				log.Debug("Server ID: " + pair.Key.ToString(), 1);

				file.WriteLine(pair.Key.ToString());

				void Write(string key, string data) {
					file.WriteLine(key + key_separator + data);
				}
				Settings entry = pair.Value;

				Write(key_bulletin,		entry.bulletin?.Id.ToString() ?? "null");
				Write(key_mention,		entry.default_mention?.ToString() ?? "null");
				Write(key_duration,		entry.duration.TotalMinutes.ToString());
				Write(key_increment,	entry.increment.TotalMinutes.ToString());
			}

			file.Close();
			log.Info("Settings exported.");
		}

		public static async Task<Dictionary<ulong, Settings>> Import(
			string path,
			DiscordClient client
		) {
			Dictionary<ulong, Settings> dict = new Dictionary<ulong, Settings>();

			log.Info("Importing settings: " + path);
			StreamReader file;
			try {
				file = new StreamReader(path);
			} catch (Exception) {
				log.Error("Could not open \"" + path + "\".", 1);
				log.Error("No previously saved settings loaded.", 1);
				return dict;
			}

			while (!file.EndOfStream) {
				string line = file.ReadLine() ?? "";

				Settings settings = new Settings(null);
				ulong guild_id = Convert.ToUInt64(line);
				DiscordGuild guild;
				try {
					guild = await client.GetGuildAsync(guild_id);
					log.Info("Server: " + guild.Name, 1);
				} catch (UnauthorizedException) {
					log.Error("Not authorized to access guild: " + guild_id.ToString(), 1);
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
						if (data == "null") {
							settings.bulletin = null;
							break;
						}
						ulong bulletin_id = Convert.ToUInt64(data);
						settings.bulletin = await client.GetChannelAsync(bulletin_id);
						break;
					case key_mention:
						if (data == "null") {
							settings.default_mention = null;
							break;
						}
						settings.default_mention = MentionRole.FromID(data, guild);
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
			log.Info("Settings import complete.");

			return dict;
		}
	}
}
