namespace Puck {
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

		public static string Spoiler(this string s) {
			return "||" + s + "||";
		}

		public static string Code(this string s) {
			return "`" + s + "`";
		}

		// Advanced text styles
		public static string CodeBlock(string input) {
			return "```" + input + "```";
		}

		public static string Code(string input, string language) {
			return "```" + language + "\n" + input + "\n```";
		}

		// Quote styles
		public static string QuoteLine(string input) {
			return "> " + input;
		}

		public static string QuoteBlock(string input) {
			return ">>> " + input;
		}

		// Link styles
		public static string NoPreview(string input) {
			return "<" + input + ">";
		}
	}
}
