using System;
using System.Collections.Generic;

namespace LWTSD
{
	public class ResourceAccess
	{
		public ResourcePath Path;
		public bool Subordinates = false;
		public bool SupportsRead = false;
		public bool SupportsWrite = false;

		public bool AllowsRead( ResourcePath Path)
		{
			if (!SupportsRead)
				return false;

			if (!Subordinates)
				return string.Compare(this.Path, Path) == 0;

			return ((string) Path).IndexOf(Path) == 0;
		}

		public bool AllowsWrite(ResourcePath Path)
		{
			if (!SupportsWrite)
				return false;

			if (!Subordinates)
				return string.Compare(this.Path, Path) == 0;

			return ((string) Path).IndexOf(Path) == 0;
		}

		public static bool AllowsRead(List<ResourceAccess> Access, ResourcePath Path)
		{
			if (Access == null)
				return true;
			
			foreach (ResourceAccess ra in Access)
			{
				if (ra.AllowsRead(Path))
					return true;
			}
			return false;
		}

		public static bool AllowsWrite(List<ResourceAccess> Access, ResourcePath Path)
		{
			if (Access == null)
				return true;
			
			foreach (ResourceAccess ra in Access)
			{
				if (ra.AllowsWrite(Path))
					return true;
			}
			return false;
		}
	}
}
