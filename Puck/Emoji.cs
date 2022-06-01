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

		Tank = GetEmoji(708431859369115790);
		Heal = GetEmoji(708431859435962418);
		Dps  = GetEmoji(708431859385630862);
		Cancel  = GetEmoji(981425774190030848);
		Refresh = GetEmoji(981425774726885406);
		Delist  = GetEmoji(981425774101921823);
	}
}
