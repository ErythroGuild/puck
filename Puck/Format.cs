namespace Puck {
	class Format {
		// Basic text styles
		public static string Bold(string input) {
			return "**" + input + "**"; // TODO: check for existing `*`
		}

		public static string Italicize(string input) {
			return "*" + input + "*";   // TODO: check for existing `*`
		}

		public static string Strikethrough(string input) {
			return "~~" + input + "~~";
		}

		public static string Underline(string input) {
			return "__" + input + "__"; // TODO: check for existing `_` (italics)
		}

		public static string Spoiler(string input) {
			return "||" + input + "||"; // TODO: check for existing link formatting
		}

		// Advanced text styles
		// TODO: single backtick? auto-choose single-line / multiline?
		// TODO: check for existing code formatting
		public static string Code(string input) {
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
