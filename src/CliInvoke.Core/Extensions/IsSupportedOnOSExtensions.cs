


#if NET5_0_OR_GREATER
using System;
#else
using System.Runtime.InteropServices;
#endif

using AlastairLundy.CliInvoke.Core.Primitives;

namespace AlastairLundy.CliInvoke.Core.Extensions
{
    public static class IsSupportedOnOsExtensions
    {
        /// <summary>
        /// Returns whether UserCredential is supported on the currently running Operating System.
        /// </summary>
        /// <param name="userCredential"></param>
        /// <returns>True if supported; false otherwise.</returns>
        public static bool IsSupportedOnCurrentOS(this UserCredential userCredential)
        {
#if NET5_0_OR_GREATER
            return OperatingSystem.IsWindows();
#else
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
        }
    
    
    }
}