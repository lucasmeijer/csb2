using Microsoft.Win32;
using NiceIO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	public abstract class MsvcInstallation
	{
		private static Dictionary<Version, MsvcInstallation> _installations;

		protected NPath VisualStudioDirectory { get; set; }
		protected NPath SDKDirectory { get; set; }
		protected bool HasCppSDK { get { return SDKDirectory != null && SDKDirectory.Exists(); } }
		public abstract IEnumerable<Type> SupportedArchitectures { get; }

		public Version Version { get; set; } 
		public abstract IEnumerable<NPath> GetIncludeDirectories();
		public abstract IEnumerable<NPath> GetLibDirectories(Architecture architecture, string sdkSubset = null);

		protected MsvcInstallation(Version visualStudioVersion, NPath visualStudioDir)
		{
			VisualStudioDirectory = visualStudioDir;
			Version = visualStudioVersion;
		}

		protected MsvcInstallation(Version visualStudioVersion)
		{
			VisualStudioDirectory = GetVisualStudioInstallationFolder(visualStudioVersion);
			Version = visualStudioVersion;
		}

		public virtual string GetPathEnvVariable(Architecture architecture)
		{
			if (architecture is ARMv7Architecture || architecture is x86Architecture)
				return string.Format("{0};{1}", VisualStudioDirectory.Combine("VC", "bin"), SDKDirectory.Combine("bin").Combine("x86"));

			return string.Format("{0};{1}", VisualStudioDirectory.Combine("VC", "bin", "amd64"), SDKDirectory.Combine("bin").Combine("x64"));
		}

		public NPath GetVSToolPath(Architecture architecture, string toolName)
		{
			var binFolder = VisualStudioDirectory.Combine("VC", "bin");

			if (architecture is x86Architecture)
				return binFolder.Combine(toolName);

			if (architecture is x64Architecture)
				return binFolder.Combine("amd64", toolName);

			if (architecture is ARMv7Architecture)
				return binFolder.Combine("x86_arm", toolName);

			throw new NotSupportedException("Can't find MSVC tool for " + architecture);
		}

		public NPath GetSDKToolPath(string toolName)
		{
			var binFolder = SDKDirectory.Combine("bin");
			var architecture = Architecture.BestThisMachineCanRun;

			if (architecture is x86Architecture)
				return binFolder.Combine("x86", toolName);

			if (architecture is x64Architecture)
				return binFolder.Combine("x64", toolName);

			throw new NotSupportedException("Can't find MSVC tool for " + architecture);
		}

		static MsvcInstallation()
		{
			_installations = new Dictionary<Version, MsvcInstallation>();

			var msvc10Version = new Version(10, 0);
			var msvc12Version = new Version(12, 0);
			var msvc14Version = new Version(14, 0);

			var msvc10InstallationFolder = GetVisualStudioInstallationFolder(msvc10Version);
			var msvc12InstallationFolder = GetVisualStudioInstallationFolder(msvc12Version);
			var msvc14InstallationFolder = GetVisualStudioInstallationFolder(msvc14Version);

			if (msvc10InstallationFolder != null)
			{
				var msvc10 = new Msvc10Installation(msvc10InstallationFolder);

				if (msvc10.HasCppSDK)
					_installations.Add(msvc10Version, msvc10);
			}

			if (msvc12InstallationFolder != null)
			{
				var msvc12 = new Msvc12Installation(msvc12InstallationFolder);

				if (msvc12.HasCppSDK)
					_installations.Add(msvc12Version, msvc12);
			}

			if (msvc14InstallationFolder != null)
			{
				var msvc14 = new Msvc14Installation(msvc14InstallationFolder);

				if (msvc14.HasCppSDK)
					_installations.Add(msvc14Version, msvc14);
			}
		}

		protected static NPath GetVisualStudioInstallationFolder(Version version)
		{
			var key = Registry.CurrentUser.OpenSubKey(string.Format(@"SOFTWARE\Microsoft\VisualStudio\{0}.{1}_Config", version.Major, version.Minor));

			if (key != null)
			{
				var installationFolder = (string)key.GetValue("InstallDir");

				if (!string.IsNullOrEmpty(installationFolder))
					return new NPath(installationFolder).Parent.Parent; // InstallDir registry value points to <dir>\Common7\IDE
			}

			return null;
		}

		public static MsvcInstallation GetLatestInstalled()
		{
			var key = _installations.Keys.OrderByDescending(k => k.Major).ThenByDescending(k => k.Minor).FirstOrDefault();

			if (key != null)
				return _installations[key];

			throw new Exception("No MSVC installations were found on the machine!");
		}

		public static MsvcInstallation GetInstallation(Version version)
		{
			var key = _installations.Keys.FirstOrDefault(k => k == version);

			if (key != null)
				return _installations[key];

			throw new Exception(string.Format("MSVC Installation version {0}.{1} is not installed on current machine!", version.Major, version.Minor));
		}
	}
}
