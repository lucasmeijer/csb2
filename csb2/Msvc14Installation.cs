using Microsoft.Win32;
using NiceIO;
using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	class Msvc14Installation : MsvcInstallation
	{
		private readonly string _sdkVersion;

		public Msvc14Installation(NPath visualStudioDir) :
			base(new Version(14, 0), visualStudioDir)
		{
			var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows\v10.0");

			if (key != null)
			{
				var sdkDir = (string)key.GetValue("InstallationFolder");

				if (!string.IsNullOrEmpty(sdkDir))
				{
					SDKDirectory = new NPath(sdkDir);

					var sdkVersionString = (string)key.GetValue("ProductVersion");
					var sdkVersion = !string.IsNullOrEmpty(sdkVersionString)
						? Version.Parse(sdkVersionString)
						: new Version(10, 0, 10240);

					if (sdkVersion.Build == -1)
						sdkVersion = new Version(sdkVersion.Major, sdkVersion.Minor, 0, 0);
					else if (sdkVersion.Revision == -1)
						sdkVersion = new Version(sdkVersion.Major, sdkVersion.Minor, sdkVersion.Build, 0);

					_sdkVersion = sdkVersion.ToString();
				}
			}
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

			var includeDirectory = SDKDirectory.Combine("Include").Combine(_sdkVersion);
			yield return includeDirectory.Combine("shared");
			yield return includeDirectory.Combine("um");
			yield return includeDirectory.Combine("winrt");
			yield return includeDirectory.Combine("ucrt");
		}

		public override IEnumerable<NPath> GetLibDirectories(Architecture architecture, string sdkSubset = null)
		{
			var libDirectory = SDKDirectory.Combine("Lib").Combine(_sdkVersion);
			var vcLibPath = VisualStudioDirectory.Combine("VC", "lib");

			if (sdkSubset != null)
				vcLibPath = vcLibPath.Combine(sdkSubset);

			if (architecture is x86Architecture)
			{
				yield return vcLibPath;
				yield return libDirectory.Combine("um", "x86");
				yield return libDirectory.Combine("ucrt", "x86");
			}
			else if (architecture is x64Architecture)
			{
				yield return vcLibPath.Combine("amd64");
				yield return libDirectory.Combine("um", "x64");
				yield return libDirectory.Combine("ucrt", "x64");
			}
			else if (architecture is ARMv7Architecture)
			{
				yield return vcLibPath.Combine("arm");
				yield return libDirectory.Combine("um", "arm");
				yield return libDirectory.Combine("ucrt", "arm");
			}
			else
			{
				throw new NotSupportedException(string.Format("Architecture {0} is not supported by MsvcToolChain!", architecture));
			}
		}
	}
}
