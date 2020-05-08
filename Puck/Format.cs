namespace Puck {
	// These functions are all "dumb" and do not attempt to perform
	// any sort of validation on their input/output.
	// That functionality would be expensive and subject to change
	// since Discord's formatting behavior is undocumented.
	static class Format {
		// Basic text styles
		public static string Bold(this string s) {
			return "**" + s + "**";
		}

		public static string Italics(this string s) {
			return "*" + s + "*";
		}

		public static string Strike(this string s) => s.Strikethrough();
		public static string Strikethrough(this string s) {
			return "~~" + s + "~~";
		}

		public static string Under(this string s) => s.Underline();
		public static string Underline(this string s) {
			return "__" + s + "__";
		}

		// Only works for links.
		public static string NoPreview(this string s) => s.NoEmbed();
		public static string NoEmbed(this string s) {
			return "<" + s + ">";
		}

		public static string Spoiler(this string s) {
			return "||" + s + "||";
		}

		public static string Code(this string s) {
			return "`" + s + "`";
		}

		public static string Quote(this string s) {
			return "> " + s;
		}

		// Block text styles
		public static string CodeBlock(string s) {
			return "```" + s + "```";
		}

		public static string Code(string s, string lang) => CodeBlock(s, lang);
		public static string CodeBlock(string s, string lang) {
			return "```" + lang + "\n" + s + "\n```";
		}

		public static string QuoteBlock(string s) {
			return ">>> " + s;
		}
	}
}
