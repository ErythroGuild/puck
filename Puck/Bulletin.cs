using DSharpPlus.Entities;

using System;
using System.Threading.Tasks;
using System.Timers;

namespace Puck {
	class Bulletin {
		public DiscordMessage message;
		public BulletinData data;
		public Timer updater;

		public event EventHandler<ulong> Delisted;

		private const double interval_refresh = 15 * 1000;

		public Bulletin(DiscordMessage message, BulletinData data) {
			this.message = message;
			this.data = data;
			
			updater = new Timer(interval_refresh);
			updater.AutoReset = true;
			updater.Elapsed += (o, e) => { _ = Update(); };
			updater.Start();
		}

		public async Task Update() {
			string bulletin_new = data.ToString();
			await message.ModifyAsync(bulletin_new);

			// TODO: add a warning 1:30 before delisting?
			if (data.expiry < DateTimeOffset.Now) {
				updater.Stop();

				string notification = "";
				notification +=
					"Your group " +
					data.title.Bold() +
					" has been delisted. :white_check_mark:";
				_ = data.owner.SendMessageAsync(notification);  // no need to await
				// TODO: move notification to Puck.Program?

				Delisted(this, message.Id);
			}
		}
	}
}
