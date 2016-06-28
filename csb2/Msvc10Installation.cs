using Microsoft.Win32;
using NiceIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	class Msvc10Installation : MsvcInstallation
	{
		public Msvc10Installation(NPath visualStudioDir) :
			base(new Version(10, 0), visualStudioDir)
		{
			var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows\v7.0A");

			if (key == null)
				return;

			var sdkDir = (string)key.GetValue("InstallationFolder");

			if (!string.IsNullOrEmpty(sdkDir))
				SDKDirectory = new NPath(sdkDir);
		}

		public override IEnumerable<Type> SupportedArchitectures
		{
			get
			{
				return new[]
				{
					typeof(x86Architecture),
					typeof(x64Architecture)
				};
			}
		}

		public override IEnumerable<NPath> GetIncludeDirectories()
		{
			yield return VisualStudioDirectory.Combine("VC", "include");
			yield return SDKDirectory.Combine("Include");
		}

		public override IEnumerable<NPath> GetLibDirectories(Architecture architecture, string sdkSubset = null)
		{
			if (architecture is x86Architecture)
			{
				yield return VisualStudioDirectory.Combine(@"VC", "lib");
				yield return SDKDirectory.Combine("Lib");
			}

			else if (architecture is x64Architecture)
			{
				yield return VisualStudioDirectory.Combine(@"VC", "lib", "amd64");
				yield return SDKDirectory.Combine("Lib", "x64");
			}
			else
			{
				throw new NotSupportedException("Unknown architecture: " + architecture);
			}
		}
	}
}
