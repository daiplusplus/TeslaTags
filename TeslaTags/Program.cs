using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TagLib;
using TagLib.Id3v1;
using TagLib.Id3v2;
using TagLib.Ape;
using TagLib.Mpeg;

namespace TeslaTags
{
	public static class Program
	{
		public static void Main(String[] args)
		{
			// TODO: Strip APE tags?
			// TODO: Warn about inconsistent values
			// TODO: Warn about similar Artists names, e.g. "P.O.D" and "P.O.D." or "The Hives" and "Hives".
		}
	}
}
