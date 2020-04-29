using DSharpPlus.Entities;

using System;
using System.Threading.Tasks;
using System.Timers;

namespace Puck {
	class Bulletin {
		public DiscordMessage message;
		public BulletinData data;
		public bool do_notify_on_delist;

		private Timer updater;

		private const double interval_refresh = 15 * 1000;

		public event EventHandler<ulong>? Delisted;

		public Bulletin(DiscordMessage message, BulletinData data) {
			this.message = message;
			this.data = data;
			do_notify_on_delist = true;

			updater = new Timer(interval_refresh) {
				AutoReset = true
			};
			updater.Elapsed += (o, e) => { _ = Update(); };
			updater.Start();
		}

		public async Task Update() {
			string bulletin_new = data.ToString();
			await message.ModifyAsync(bulletin_new);

			if (data.expiry < DateTimeOffset.Now) {
				updater.Stop();

				string notification = "";
				notification +=
					"Your group " +
					data.title.Bold() +
					" has been delisted. :white_check_mark:";
				if (do_notify_on_delist)
					_ = data.owner.SendMessageAsync(notification);  // no need to await
					// TODO: move notification to Puck.Program?

				Delisted?.Invoke(this, message.Id);
			}
		}
	}
}
