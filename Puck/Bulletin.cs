using DSharpPlus.Entities;

using System;
using System.Threading.Tasks;
using System.Timers;

namespace Puck {
	class Bulletin {
		// Constants.
		static readonly Logger log = Program.GetLogger();
		const double interval_refresh = 15 * 1000;



		// Fields.
		public event EventHandler<ulong>? Delisted;

		public DiscordMessage message;
		public BulletinData data;
		public ulong original_id;
		public bool do_notify_owner;

		readonly Timer updater;

		// Constructor (all fields are required to be valid).
		public Bulletin(DiscordMessage message, BulletinData data, ulong original_id) {
			this.message = message;
			this.data = data;
			this.original_id = original_id;
			do_notify_owner = true;

			updater = new Timer(interval_refresh) {
				AutoReset = true
			};
			updater.Elapsed += (o, e) => { _ = Update(); };
			updater.Start();
		}

		//  Update logic (same for every Bulletin).
		public async Task Update() {
			string bulletin_new = data.ToString();
			await message.ModifyAsync(bulletin_new);
			log.Debug("Updated bulletin.", 1, message.Id);

			if (data.expiry < DateTimeOffset.Now) {
				log.Info("Bulletin delisted.", 0, message.Id);
				updater.Stop();

				if (do_notify_owner) {
					string notification = "";
					notification +=
						":white_check_mark: Your group " +
						data.title.Bold() +
						" has been delisted.";

					_ = data.owner.SendMessageAsync(notification);  // no need to await
					log.Info(
						"Sending delist notification to " +
						data.owner.Userstring() + ".",
						0, message.Id
					);
				} else {
					log.Info(
						"Not sending delist notification to " +
						data.owner.DisplayName + ".",
						0, message.Id
					);
				}

				Delisted?.Invoke(this, message.Id);
			}
		}
	}
}
