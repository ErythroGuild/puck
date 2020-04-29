namespace Puck {
	// TODO: check for existing format markers in string (and escapes)
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
		// TODO: single backtick? auto-choose single-line / multiline?
		// TODO: check for existing code formatting
		public static string CodeBlock(string input) {
			return "```" + input + "```";
		}

		public static string Code(string input, string language) {
			return "```" + language + "\n" + input + "\n```";
		}

		// Quote styles
		// TODO: auto-choose single/multi-line
		// TODO: check for existing quote formatting
		public static string QuoteLine(string input) {
			return "> " + input;
		}

		public static string QuoteBlock(string input) {
			return ">>> " + input;
		}

		// Link styles
		// TODO: validate input
		public static string NoPreview(string input) {
			return "<" + input + ">";
		}
	}
}
