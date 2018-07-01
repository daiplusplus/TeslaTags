using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaTags
{
	public class DirectoryResult
	{
		private static readonly List<Message> _empty = new List<Message>();

		public DirectoryResult( String directoryPath, FolderType folderType, Int32 totalFiles, Int32 modifiedFiles, List<Message> messages )
		{
			this.DirectoryPath = directoryPath;
			this.FolderType    = folderType;
			this.TotalFiles    = totalFiles;
			this.ModifiedFiles = modifiedFiles;
			this.Messages      = messages ?? _empty;
		}

		public String                 DirectoryPath { get; }
		public FolderType             FolderType    { get; }
		public Int32                  TotalFiles    { get; }
		public Int32                  ModifiedFiles { get; }
		public IReadOnlyList<Message> Messages      { get; }
	}
}
