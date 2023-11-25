﻿using System.Diagnostics;
using System.Security.Cryptography;

namespace Bifrost.Launcher
{
    public partial class GameDirectory
    {
        private string _directoryPath = string.Empty;
        private string _executableDirectory32 = string.Empty;
        private string _executableDirectory64 = string.Empty;
        private string _executableName = string.Empty;

        internal string ExecutablePath32 { get => IsInitialized ? Path.Combine(_executableDirectory32, _executableName) : string.Empty; }
        internal string ExecutablePath64 { get => IsInitialized && Supports64 ? Path.Combine(_executableDirectory64, _executableName) : string.Empty; }
        internal bool Supports64 { get; private set; }  // Only versions 1.26.0.119 (2014-09-12) and up have Win64 executables

        public bool IsInitialized { get; private set; } = false;
        public string Version { get; private set; } = "Unknown Version";

        public bool Initialize(string path, out string message)
        {
            // Determine executable directories
            _directoryPath = path;
            _executableDirectory32 = Path.Combine(_directoryPath, "UnrealEngine3", "Binaries", "Win32");
            _executableDirectory64 = Path.Combine(_directoryPath, "UnrealEngine3", "Binaries", "Win64");

            // Fail initialization if Win32 executable directory does not exist
            if (Directory.Exists(_executableDirectory32) == false)
            {
                message = "Marvel Heroes not found.";
                IsInitialized = false;
                return IsInitialized;
            }

            // Detect executable name
            _executableName = DetectExecutableName();

            // Fail initialization if could not detect executable name
            if (_executableName == string.Empty)
            {
                message = "Marvel Heroes executable not found.";
                IsInitialized = false;
                return IsInitialized;
            }

            // Detect version and Win64 support
            Version = DetectExecutableVersion();
            Supports64 = Directory.Exists(_executableDirectory64) && File.Exists(Path.Combine(_executableDirectory64, _executableName));

            // Finish initialization
            message = "Game directory initialized successfully.";
            IsInitialized = true;
            return IsInitialized;
        }

        public string GetVersionDebugInfo()
        {
            if (IsInitialized == false)
                return "Not Initialized";

            var versionInfo = FileVersionInfo.GetVersionInfo(ExecutablePath32);
            byte[] executable = File.ReadAllBytes(ExecutablePath32);
            bool isShipping = CheckExecutableSignature(executable, ShippingSignature);
            string hash = Convert.ToHexString(SHA1.HashData(executable));

            return $"{_executableName}\n{hash}\n{versionInfo.FileVersion}\nIsShipping: {isShipping}";
        }

        private string DetectExecutableName()
        {
            if (File.Exists(Path.Combine(_executableDirectory32, ExecutableNameOmega)))
                return ExecutableNameOmega;

            if (File.Exists(Path.Combine(_executableDirectory32, ExecutableName2016)))
                return ExecutableName2016;

            if (File.Exists(Path.Combine(_executableDirectory32, ExecutableName2015)))
                return ExecutableName2015;

            if (File.Exists(Path.Combine(_executableDirectory32, ExecutableNameVanilla)))
                return ExecutableNameVanilla;

            return string.Empty;
        }

        private string DetectExecutableVersion()
        {
            byte[] executable = File.ReadAllBytes(Path.Combine(_executableDirectory32, _executableName));
            string hash = Convert.ToHexString(SHA1.HashData(executable));

            if (VersionDict.TryGetValue(hash, out string version) == false)
                return "Unknown";

            return version;
        }

        private bool CheckExecutableSignature(byte[] executable, byte[] signature)
        {
            bool result = false;

            // Hack: speed this up by starting near the end of the executable where the build config
            // signatures we are looking for should be.
            for (int i = executable.Length - executable.Length / 5; i < executable.Length; i++)
            {
                if (signature.SequenceEqual(executable.Skip(i).Take(signature.Length)))
                    result = true;
            }

            return result;
        }
    }
}
