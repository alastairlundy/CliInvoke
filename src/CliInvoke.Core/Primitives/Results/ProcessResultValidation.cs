/*
    Based on Tyrrrz's CliWrap CommandResultValidation.cs
    https://github.com/Tyrrrz/CliWrap/blob/master/CliWrap/CommandResultValidation.cs

     See THIRD_PARTY_NOTICES.txt for a full copy of the MIT LICENSE.
 */

using System;

namespace AlastairLundy.CliInvoke.Core.Primitives.Results
{
    /// <summary>
    /// An enum to represent Result Validation states, and whether Result Validation should be performed. 
    /// </summary>
    [Flags]
    public enum ProcessResultValidation
    {
        /// <summary>
        /// No validation is performed.
        /// </summary>
        None = 0b0,
        /// <summary>
        /// Throws an exception if the Process's Exit code is not zero.
        /// </summary>
        ExitCodeZero = 0b1,
    }
}