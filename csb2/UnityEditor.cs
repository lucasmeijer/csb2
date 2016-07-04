using System.Linq;
using NiceIO;
using Unity.TinyProfiling;

namespace csb2
{
    class UnityEditor : Node
    {
        public UnityEditor() : base("UnityEditor")
        {
            var objectNodes = new ObjectNode[0];

            using (TinyProfiler.Section("GetJamOutput"))
                objectNodes = JamOutput.GetObjectNodes().ToArray();


            var jamParser = new JamOutputParser();
            var libs = new[]
            {
                "C:/Program Files (x86)/Microsoft Visual Studio 10.0/vc/lib/amd64/libcmtd.lib", "C:/unity2/External/DirectX/libs64/dinput.lib", "C:/unity2/External/DirectX/libs64/dxguid.lib",
                "C:/unity2/External/FreeImage/builds/win64/FreeImage.lib", "C:/unity2/External/RakNet/builds/raknet_vc64.lib", "C:/unity2/External/Audio/libogg/libs/libogg64.lib",
                "C:/unity2/External/Audio/libvorbis/libs/libvorbis64.lib", "C:/unity2/External/Audio/libvorbis/libs/libvorbisfile64.lib", "C:/unity2/External/theora/libs/theora_full_static64.lib",
                "C:/unity2/External/FMOD/builds/win64/lib/libfmodex.lib", "C:/unity2/External/Audio/lame/lib64/libmp3lame.lib", "C:/unity2/External/Audio/xma/lib/win64/xmaencoder.lib",
                "C:/unity2/External/libcurl/builds/win64/lib/libcurl_a.lib", "C:/unity2/External/openssl/builds/win64/lib/libeay32.lib", "C:/unity2/External/openssl/builds/win64/lib/ssleay32.lib",
                "C:/unity2/PlatformDependent/Win/libs64/hid.lib", "C:/unity2/External/TextureCompressors/ATC_Qualcomm/win64/TextureConverter.lib",
                "C:/unity2/External/TextureCompressors/DXT_ATI/libS3TC_x64.lib", "C:/unity2/External/Umbra/builds/lib/win64/umbraoptimizer64.lib",
                "C:/unity2/External/xercesc/builds/win64/lib/xerces-c_static_3.lib", "C:/unity2/External/xsec/builds/win64/lib/xsec_lib.lib",
                "C:/unity2/External/SpeedTree/builds/Lib/Windows/VC10.x64/SpeedTreeCore_Windows_v7.0_VC10_MT64_Static.lib", "C:/unity2/External/Cef/builds/lib/win64/libcef.lib",
                "C:/unity2/External/Cef/builds/lib/win64/libcef_dll_wrapper.lib", "C:/unity2/External/TextureCompressors/ISPCTextureCompressor/ispc_texcomp_x64.lib",
                "C:/unity2/External/Enlighten/builds/Lib/WIN64_2010ST/Enlighten3.lib", "C:/unity2/External/Enlighten/builds/Lib/WIN64_2010ST/EnlightenBake.lib",
                "C:/unity2/External/Enlighten/builds/Lib/WIN64_2010ST/Enlighten3HLRT.lib", "C:/unity2/External/Enlighten/builds/Lib/WIN64_2010ST/EnlightenPrecomp2.lib",
                "C:/unity2/External/Enlighten/builds/Lib/WIN64_2010ST/GeoBase.lib", "C:/unity2/External/Enlighten/builds/Lib/WIN64_2010ST/GeoCore.lib",
                "C:/unity2/External/Enlighten/builds/Lib/WIN64_2010ST/GeoRayTrace.lib", "C:/unity2/External/Enlighten/builds/Lib/WIN64_2010ST/IntelTBB.lib",
                "C:/unity2/External/Enlighten/builds/Lib/WIN64_2010ST/Zlib.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/LowLevel.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/LowLevelCloth.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3CharacterKinematic_x64.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3Common_x64.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3Cooking_x64.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3Extensions.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3Vehicle.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3_x64.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysXProfileSDK.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysXVisualDebuggerSDK.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/PvdRuntime.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/PxTask.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/SceneQuery.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/SimulationController.lib", "C:/unity2/External/theora/libs/theora_decode_static64.lib",
                "C:/unity2/External/FMOD/builds/win64/lib/libfmodex.lib", "C:/unity2/External/SketchUp/builds/binaries/win64/slapi.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/comdlg32.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/kernel32.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/user32.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/gdi32.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/comctl32.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/advapi32.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/crypt32.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/shell32.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/shlwapi.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/ole32.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/uuid.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/version.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/OpenGL32.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/Glu32.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/winmm.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/ws2_32.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/MSImg32.lib", "C:/unity2/External/postgresql/libs/win64/libpq.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/urlmon.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/wbemuuid.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/oleaut32.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/psapi.lib",
                "C:/unity2/External/Quicktime/Libraries/QTMLClient.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/Strmiids.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/Msacm32.lib", "C:/unity2/External/Audio/libogg/libs/libogg.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/imm32.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/Iphlpapi.lib",
                "C:/unity2/External/videoInput/dshow/lib/ddraw.lib", "C:/unity2/External/videoInput/dshow/lib/strmbase.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/Dnsapi.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/Iphlpapi.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/Setupapi.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/Winhttp.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/Wininet.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/mfplat.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/mfreadwrite.lib",
                "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/mfuuid.lib", "C:/Program Files (x86)/Microsoft SDKs/Windows/v7.0A/Lib/x64/propsys.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/LowLevel.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/LowLevelCloth.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3CharacterKinematic_x64.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3Common_x64.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3Cooking_x64.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3Extensions.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3Vehicle.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysX3_x64.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysXProfileSDK.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/PhysXVisualDebuggerSDK.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/PvdRuntime.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/PxTask.lib",
                "C:/unity2/External/PhysX3/builds/Lib/win64/release/SceneQuery.lib", "C:/unity2/External/PhysX3/builds/Lib/win64/release/SimulationController.lib",
                "C:/unity2/External/Allegorithmic/builds/Engines/lib/win64/mt/release/libirc.lib", "C:/unity2/External/Allegorithmic/builds/Engines/lib/win64/mt/release/libmmt.lib",
                "C:/unity2/External/Allegorithmic/builds/Engines/lib/win64/mt/release/substance_sse2_blend.lib", "C:/unity2/External/Allegorithmic/builds/Engines/lib/win64/mt/release/svml_dispmt.lib",
                "C:/unity2/External/Allegorithmic/builds/Engines/lib/win64/mt/debug/substance_linker.lib", "C:/unity2/External/Allegorithmic/builds/Engines/lib/win64/mt/debug/algcompressionlzma.lib",
                "C:/unity2/External/Allegorithmic/builds/Engines/lib/win64/mt/debug/algcompressionstd7z.lib", "C:/unity2/External/Allegorithmic/builds/Engines/lib/win64/mt/debug/pfxlinkercommon.lib",
                "C:/unity2/External/Allegorithmic/builds/Engines/lib/win64/mt/debug/lzma.lib", "C:/unity2/External/Allegorithmic/builds/Engines/lib/win64/mt/debug/tinyxml.lib"
            };

            var more =
                "C:/unity2/build/Extensions/win64-debug/Infrastructure/EditorExtensionRegistrarLib.debug.lib C:/unity2/artifacts/linkeroutput/win64_debug_libwebsockets_mono_1/libwebsockets.lib C:/unity2/artifacts/linkeroutput/win64_debug_zlib_mono_1/zlib.lib C:/unity2/artifacts/linkeroutput/win64_debug_libpng_mono_1/libpng.lib C:/unity2/artifacts/linkeroutput/win64_debug_libjpeg_mono_1/libjpeg.lib C:/unity2/artifacts/linkeroutput/win64_debug_zlib_mono_1/zlib.lib C:/unity2/artifacts/linkeroutput/win64_debug_UnitTest++_mono_1/UnitTest++.lib C:/unity2/artifacts/linkeroutput/win64_debug_EditorCrashHandlerLib_mono_1/EditorCrashHandlerLib.lib C:/unity2/artifacts/linkeroutput/win64_debug_UnityYAMLMergeLib_mono_1/UnityYAMLMergeLib.lib C:/unity2/artifacts/linkeroutput/win64_debug_udis86lib_mono_1/udis86lib.lib C:/unity2/artifacts/linkeroutput/win64_debug_pogostubslib_mono_1/pogostubslib.lib C:/unity2/artifacts/linkeroutput/win64_debug_pubnub_mono_1/pubnub.lib C:/unity2/artifacts/linkeroutput/win64_debug_libcrunch_mono_1/libcrunch.lib C:/unity2/artifacts/linkeroutput/win64_debug_UnityAsmUtilsLib_mono_1/UnityAsmUtilsLib.lib";

            var allLibs = libs.Concat(more.Split(' '));
            var exeNode = new ExeNode(new NPath("c:/unity2/build/WindowsEditor/Unity.exe"), objectNodes, allLibs.Distinct().Select(i => new NPath(i)).ToArray(), new[] { "/INCREMENTAL","/DEBUG","/MACHINE:X64","/PDB:C:/unity2/build/WindowsEditor/Unity_x64_d.pdb","/PDBSTRIPPED:C:/unity2/build/WindowsEditor/Unity_x64_d_s.pdb","/stack:2097152","/NXCOMPAT:NO","/MANIFEST:NO","/DYNAMICBASE:NO","/NOLOGO","/LARGEADDRESSAWARE","/NODEFAULTLIB:LIBDECIMAL","/NODEFAULTLIB:LIBCMT","/NODEFAULTLIB:LIBCMTD","/NODEFAULTLIB:LIBC","/NODEFAULTLIB:MSVCRT","/SUBSYSTEM:WINDOWS" });

            SetStaticDependencies(exeNode);
        }

        public override string NodeTypeIdentifier => "CppProgram";
    }
}