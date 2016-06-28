using Microsoft.Win32;
using NiceIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	class Msvc12Installation : MsvcInstallation
	{
		public Msvc12Installation(NPath visualStudioDir) :
			base(new Version(12, 0), visualStudioDir)
		{
			var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows\v8.1");

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
					typeof(ARMv7Architecture),
					typeof(x64Architecture)
				};
			}
		}

		public override IEnumerable<NPath> GetIncludeDirectories()
		{
			yield return VisualStudioDirectory.Combine("VC", "include");

			var includeDirectory = SDKDirectory.Combine("Include");
			yield return includeDirectory.Combine("shared");
			yield return includeDirectory.Combine("um");
			yield return includeDirectory.Combine("winrt");
		}

		public override IEnumerable<NPath> GetLibDirectories(Architecture architecture, string sdkSubset = null)
		{
			var vcLibPath = VisualStudioDirectory.Combine("VC", "lib");
			var sdkLibDirectory = SDKDirectory.Combine("lib", "winv6.3", "um");

			if (sdkSubset != null)
				vcLibPath = vcLibPath.Combine(sdkSubset);

			if (architecture is x86Architecture)
			{
				yield return vcLibPath;
				yield return sdkLibDirectory.Combine("x86");
			}
			else if (architecture is x64Architecture)
			{
				yield return vcLibPath.Combine("amd64");
				yield return sdkLibDirectory.Combine("x64");
			}
			else if (architecture is ARMv7Architecture)
			{
				yield return vcLibPath.Combine("arm");
				yield return sdkLibDirectory.Combine("arm");
			}
			else
			{
				throw new NotSupportedException(string.Format("Architecture {0} is not supported by MSVC 12 compiler!", architecture));
			}
		}
	}
}
