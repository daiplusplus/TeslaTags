using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaTags.Gui
{
	/// <summary>Gets "live" configuration values directly from the UI, even if they aren't saved yet.</summary>
	public interface ILiveConfigurationService
	{
		FileSystemPredicate CreateFileSystemPredicate();
	}
}
