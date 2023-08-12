using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IngameScript
{
	partial class Program : MyGridProgram
	{
		class logmsg
		{
			public logmsg(string m, LT l) { msg = m; level = l; }
			public string msg = "";
			public int c = 1;
			public LT level = LT.LOG_N;
		}

		List<logmsg> loggedMessages = new List<logmsg>();
		int MAX_LOG = 50;
		bool loggedMessagesDirty = true;


		public enum LT
		{
			LOG_N = 0,
			LOG_D,
			LOG_DD
		}

		public static LT LOG_LEVEL = LT.LOG_N;

		public static void log(string s, LT level)
		{
			if (level > LOG_LEVEL) return;

			if (s.Length > 50)
			{
				List<string> tok = new List<string>();
				while (s.Length > 50)
				{
					int c = 0;
					if (tok.Count > 0) c = 2;
					tok.Add(s.Substring(0, 50 - c));
					s = s.Substring(50 - c);
				}
				tok.Add(s);
				s = string.Join("\n ", tok);
			}
			var p = gProgram;
			logmsg l = null;
			if (p.loggedMessages.Count > 0)
			{
				l = p.loggedMessages[p.loggedMessages.Count - 1];
			}
			if (l != null)
			{
				if (l.msg == s) l.c += 1;
				else p.loggedMessages.Add(new logmsg(s, level));
			}
			else p.loggedMessages.Add(new logmsg(s, level));
			if (p.loggedMessages.Count > p.MAX_LOG) p.loggedMessages.RemoveAt(0);
			p.loggedMessagesDirty = true;
		}


		string loggedMessagesCache = "";
		string renderLoggedMessages()
		{
			if (!loggedMessagesDirty) return loggedMessagesCache;

			string o = "";
			foreach (var m in loggedMessages)//(int i = 0; i < loggedMessages.Count; i++)
			{
				o += m.msg;
				if (m.c > 1) o += " (" + m.c + ")";
				o += "\n";
			}
			loggedMessagesDirty = false;
			loggedMessagesCache = o;
			return o;
		}
	}
}
