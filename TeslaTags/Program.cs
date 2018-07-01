using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TeslaTags
{
	public static class Program
	{
		public static async Task<Int32> Main(String[] args)
		{
			if( args.Length < 1 )
			{
				Console.WriteLine( "Usage: TeslaTags.exe <musicDirectory> [<readOnly> = false]" );
				return 1;
			}

			String root = args[0];
			Boolean readOnly = Boolean.Parse( args.ElementAtOrDefault(1) ?? "false" ); // `Boolean.Parse` is case-insensitive
			
			RetaggingOptions opts = new RetaggingOptions( root, readOnly, undo: false, directoryFilter: null, genreRules: new GenreRules() );

			RealTeslaTagService service = new RealTeslaTagService();

			Progress<IReadOnlyList<String>> directoriesReceiver = new Progress<IReadOnlyList<String>>( directories => {

				if( directories.Count == 0 )
				{
					Console.WriteLine( "Directory {0} is empty.", root );
				}

			} );

			Progress<DirectoryResult> directoryReceiver = new Progress<DirectoryResult>( result => {

				Console.Write( result.FolderType );
				Console.Write( "{0} files, {1} modified files, ", result.TotalFiles, result.ActualModifiedFiles );

				if( result.Messages.Any( m => m.Severity == MessageSeverity.Error ) ) Console.ForegroundColor = ConsoleColor.Red;
				Console.Write( "{0} errors", result.Messages.Count( m => m.Severity == MessageSeverity.Error ) );
				Console.ResetColor();

				Console.Write( ", " );

				if( result.Messages.Any( m => m.Severity == MessageSeverity.Warning ) ) Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write( "{0} warnings", result.Messages.Count( m => m.Severity == MessageSeverity.Warning ) );
				Console.ResetColor();

				Console.WriteLine();

			} );

			CancellationTokenSource cts = new CancellationTokenSource();

			try
			{
				Task task = service.StartRetaggingAsync( opts, directoriesReceiver, directoryReceiver, cts.Token );
				await task;
			}
			catch( Exception ex )
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine( "Error:" );
				Console.ResetColor();

				while( ex != null )
				{
					Console.WriteLine( ex.GetType().FullName );
					Console.WriteLine( ex.Message );
					Console.WriteLine( ex.StackTrace );
					Console.WriteLine();

					ex = ex.InnerException;
				}
			}

			return 0;
		}
	}

	

	

	
}
