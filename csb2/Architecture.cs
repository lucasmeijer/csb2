using System;

namespace Unity.IL2CPP.Building
{
	public abstract class Architecture
	{
		public abstract int Bits { get; }
		public abstract string Name { get; }

		public static Architecture OfCurrentProcess
		{
			get { return IntPtr.Size == 4 ? (Architecture)new x86Architecture() : new x64Architecture(); }
		}

		public static Architecture BestThisMachineCanRun { get { return new x64Architecture(); } }

		public static bool operator ==(Architecture left, Architecture right)
		{
			if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
				return ReferenceEquals(left, right);

			return left.GetType() == right.GetType();
		}

		public static bool operator !=(Architecture left, Architecture right)
		{
			return !(left == right);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			return GetType() == obj.GetType();
		}

		public override int GetHashCode()
		{
			return GetType().GetHashCode();
		}
	}

	public class x86Architecture : Architecture
	{
		public override int Bits { get { return 32; } }
		public override string Name { get { return "x86"; } }
	}

	public class x64Architecture : Architecture
	{
		public override int Bits { get { return 64; } }
		public override string Name { get { return "x64"; } }
	}

	public class ARMv7Architecture : Architecture
	{
		public override int Bits { get { return 32; } }
		public override string Name { get { return "ARMv7"; } }
	}

	public class ARM64Architecture : Architecture
	{
		public override int Bits { get { return 64; } }
		public override string Name { get { return "ARM64"; } }
	}

	public class EmscriptenJavaScriptArchitecture : Architecture
	{
		public override int Bits { get { return 32; } }
		public override string Name { get { return "EmscriptenJavaScript"; } }
	}
}