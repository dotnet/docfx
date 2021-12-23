using System.Diagnostics;
using Mono.Options;

namespace Mono.Documentation {

	public abstract class MDocCommand {

		public TraceLevel TraceLevel { get; set; }
		public bool DebugOutput { get; set; }

		public abstract void Run (IEnumerable<string> args);

		protected List<string> Parse (OptionSet p, IEnumerable<string> args, 
				string command, string prototype, string description)
		{
			bool showHelp = false;
			p.Add ("h|?|help", 
					"Show this message and exit.", 
					v => showHelp = v != null );

			List<string> extra = null;
			if (args != null) {
				extra = p.Parse (args.Skip (1));
			}
			if (args == null || showHelp) {
				Console.WriteLine ("usage: mdoc {0} {1}", 
						args == null ? command : args.First(), prototype);
				Console.WriteLine ();
				Console.WriteLine (description);
				Console.WriteLine ();
				Console.WriteLine ("Available Options:");
				p.WriteOptionDescriptions (Console.Out);
				return null;
			}
			return extra;
		}

		public void Error (string format, params object[] args)
		{
			throw new Exception (string.Format (format, args));
		}

		public void Message (TraceLevel level, string format, params object[] args)
		{
			if ((int) level > (int) TraceLevel)
				return;
			if (level == TraceLevel.Error)
				Console.Error.WriteLine (format, args);
			else
				Console.WriteLine (format, args);
		}
	}
}
