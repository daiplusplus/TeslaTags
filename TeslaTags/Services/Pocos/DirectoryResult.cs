using System;
using System.Collections.Generic;

namespace TeslaTags
{
	public class DirectoryResult
	{
		private static readonly List<Message> _empty = new List<Message>();

		public DirectoryResult( String directoryPath, FolderType folderType, Int32 totalFiles, Int32 proposedModifiedFilesCount, Int32 actualModifiedFilesCount, List<Message> messages )
		{
			this.DirectoryPath         = directoryPath;
			this.FolderType            = folderType;
			this.TotalFiles            = totalFiles;
			this.ProposedModifiedFiles = proposedModifiedFilesCount;
			this.ActualModifiedFiles   = actualModifiedFilesCount;
			this.Messages              = messages ?? _empty;
		}

		public String                 DirectoryPath         { get; }
		public FolderType             FolderType            { get; }
		public Int32                  TotalFiles            { get; }
		public Int32                  ProposedModifiedFiles { get; }
		public Int32                  ActualModifiedFiles   { get; }
		public IReadOnlyList<Message> Messages              { get; }
	}
}
