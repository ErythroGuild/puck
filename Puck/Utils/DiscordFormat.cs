namespace Puck.Utils;

static partial class Util {
	// Basic text formatting (single-line).
	public static string Bold(this string input) => $"**{input}**";
	public static string Italicize(this string input) => $"*{input}*";
	public static string Underline(this string input) => $"__{input}__";
	public static string Strikethrough(this string input) => $"~~{input}~~";
	public static string Monospace(this string input) => $"`{input}`";
	public static string Quote(this string input) => $"> {input}";
	public static string Spoiler(this string input) => $"||{input}||";

	// Block text formatting.
	public static string QuoteBlock(this string input) =>
		$">>> {input}";
	public static string CodeBock(this string input, string language="") =>
		$"```{language}\n{input}\n```";

	// Adjusts to 12-/24-hour clock based on user's client locale.
	// Default style (no format specifier) is equivalent to ":f"
	// (DateTimeShort).
	public enum TimestampStyle {
		Relative,  // "2 months ago", "in an hour"
		TimeShort, //  h:mm tt       |  HH:mm
		TimeLong,  //  h:mm:ss tt    |  HH:mm:ss
		DateShort, //  MM/dd/yyyy    |  dd/MM/yyyy
		DateLong,  //  MMMM d, yyyy  |  dd MMMM yyyy
		DateTimeShort, //  MMMM d, yyyy h:mm tt        |  dd MMMM yyyy HH:mm
		DateTimeLong,  //  dddd, MMMM d, yyyy h:mm tt  |  dddd, dd MMMM yyyy HH:mm
	};
	// Returns a formatted timestamp from a given DateTimeOffset.
	// Valid format strings are currently undocumented.
	public static string Timestamp(this DateTimeOffset time, TimestampStyle style=TimestampStyle.DateTimeShort) =>
		$"<t:{time.ToUnixTimeSeconds()}:{_timestampTable[style]}>";
	public static string Timestamp(this DateTimeOffset time, string format="f") =>
		$"<t:{time.ToUnixTimeSeconds()}:{format}>";
	private static readonly Dictionary<TimestampStyle, string> _timestampTable = new () {
		{ TimestampStyle.Relative,  "R" },
		{ TimestampStyle.TimeShort, "t" },
		{ TimestampStyle.TimeLong,  "T" },
		{ TimestampStyle.DateShort, "d" },
		{ TimestampStyle.DateLong,  "D" },
		{ TimestampStyle.DateTimeShort, "f" },
		{ TimestampStyle.DateTimeLong,  "F" },
	};

	// Prints the "user#tag" of a user.
	public static string Tag(this DiscordUser user) =>
		$"{user.Username}#{user.Discriminator}";
	public static string Tag(this DiscordMember user) =>
		$"{user.Username}#{user.Discriminator}";

	// Prints a DiscordColor in "#RRGGBB" format.
	public static string HexCode(this DiscordColor color) =>
		$"#{color:X6}";
}
