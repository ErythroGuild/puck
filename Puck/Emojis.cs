namespace Puck;

class Emojis {
	public DiscordEmoji Tank { get; init; }
	public DiscordEmoji Heal { get; init; }
	public DiscordEmoji Dps  { get; init; }
	public DiscordEmoji Cancel  { get; init; }
	public DiscordEmoji Refresh { get; init; }
	public DiscordEmoji Delist  { get; init; }

	public Emojis(DiscordClient client) {
		DiscordEmoji GetEmoji(ulong id) =>
			DiscordEmoji.FromGuildEmote(client, id);

		Tank = GetEmoji(981422648322064395);
		Heal = GetEmoji(981422648275914805);
		Dps  = GetEmoji(981422648271712276);
		Cancel  = GetEmoji(981425774190030848);
		Refresh = GetEmoji(981425774726885406);
		Delist  = GetEmoji(981425774101921823);
	}
}
