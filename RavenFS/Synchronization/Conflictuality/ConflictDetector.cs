namespace RavenFS.Synchronization.Conflictuality
{
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using Client;
	using RavenFS.Infrastructure;

	public class ConflictDetector
	{
		public ConflictItem Check(string fileName, NameValueCollection localMetadata, NameValueCollection remoteMetadata)
		{
			if (Historian.IsDirectChildOfCurrent(localMetadata, remoteMetadata))
			{
				return null;
			}

			return
				new ConflictItem
				{
					CurrentHistory = TransformToFullConflictHistory(localMetadata),
					RemoteHistory = TransformToFullConflictHistory(remoteMetadata),
					FileName = fileName,
				};
		}

		public ConflictItem CheckOnSource(string fileName, NameValueCollection localMetadata,
		                                  NameValueCollection remoteMetadata)
		{
			return Check(fileName, remoteMetadata, localMetadata);
		}

		private static List<HistoryItem> TransformToFullConflictHistory(NameValueCollection metadata)
		{
			var version = long.Parse(metadata[SynchronizationConstants.RavenSynchronizationVersion]);
			var serverId = metadata[SynchronizationConstants.RavenSynchronizationSource];
			var fullHistory = Historian.DeserializeHistory(metadata);
			fullHistory.Add(new HistoryItem() { ServerId = serverId, Version = version });

			return fullHistory;
		} 
	}
}