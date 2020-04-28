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

		public async Task Update() {
			string bulletin_new = data.ToString();
			await message.ModifyAsync(bulletin_new);

			// TODO: add a warning 1:30 before delisting?
			if (data.expiry < DateTimeOffset.Now) {
				string notification = "";
				notification +=
					"Your group " +
					Format.Bold(data.title) +
					" has been delisted. :white_check_mark:";
				_ = data.owner.SendMessageAsync(notification);  // no need to await
				// TODO: move notification to Puck.Program?

				Delisted(this, message.Id);
			}
		}
	}
}
