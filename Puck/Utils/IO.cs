namespace Puck.Utils;

static partial class Util {
	// Converting strings to/from single-line, easily parseable text.
	public static string Escape(this string str) {
		string text = str;
		foreach (string escape_code in _escapeCodes.Keys) {
			string codepoint = _escapeCodes[escape_code];
			text = text.Replace(codepoint, escape_code);
		}
		return text;
	}
	public static string Unescape(this string str) {
		string text = str;
		foreach (string escape_code in _escapeCodes.Keys) {
			string codepoint = _escapeCodes[escape_code];
			text = text.Replace(escape_code, codepoint);
		}
		return text;
	}
	private static readonly ReadOnlyDictionary<string, string> _escapeCodes =
		new (new ConcurrentDictionary<string, string>() {
			[@"\n"    ] = "\n"    ,
			[@"\esc"  ] = "\x1B"  ,
			[@":bbul:"] = "\u2022",
			[@":wbul:"] = "\u25E6",
			[@":emsp:"] = "\u2003",
			[@":ensp:"] = "\u2022",
			[@":nbsp:"] = "\u00A0",
			[@":+-:"  ] = "\u00B1",
		});

	// Syntax sugar for passing a string as a Lazy<string>.
	public static Lazy<string> AsLazy(this string str) => new (str);

	// Returns all of the string up to the first newline if one exists,
	// and returns the entire string otherwise.
	public static string FirstLineElided(this string input) {
		if (input.Contains('\n')) {
			int i_newline = input.IndexOf("\n");
			return input[..i_newline] + " [...]";
		} else {
			return input;
		}
	}

	// Print a List<string> as concatenated lines.
	public static string ToLines(this List<string> lines) =>
		string.Join("\n", lines);

	// Create a blank file at the given path, if it doesn't exist.
	// Returns true if file was created, false otherwise.
	// `object` is a reference type.
	public static bool CreateIfMissing(string path, object @lock) {
		bool did_create = false;
		lock (@lock) {
			if (!File.Exists(path)) {
				File.Create(path).Close();
				did_create = true;
			}
		}
		return did_create;
	}
}
