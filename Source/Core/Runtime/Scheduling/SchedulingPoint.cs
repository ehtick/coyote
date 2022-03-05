﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides a set of static methods for declaring points in the execution where interleavings
    /// between operations should be explored during testing.
    /// </summary>
    /// <remarks>
    /// These methods are no-op in production.
    /// </remarks>
    public static class SchedulingPoint
    {
        /// <summary>
        /// Explores a possible interleaving with another controlled operation.
        /// </summary>
        public static void Interleave()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.ScheduleNextOperation(SchedulingPointType.Interleave, isSuppressible: false);
            }
        }

        /// <summary>
        /// Yields execution to another controlled operation.
        /// </summary>
        /// <remarks>
        /// Invoking this method can lower the scheduling priority of the currently executing
        /// operation when certain exploration strategies are used.
        /// </remarks>
        public static void Yield()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.ScheduleNextOperation(SchedulingPointType.Yield, isSuppressible: false, isYielding: true);
            }
        }

        /// <summary>
        /// Explores a possible interleaving due to a 'READ' operation on the specified shared state.
        /// </summary>
        public static void Read(string state)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                ControlledOperation op = runtime.GetExecutingOperation();
                op.LastAccessedState = state;
                runtime.ScheduleNextOperation(SchedulingPointType.Read, isSuppressible: false);
                op.LastAccessedState = string.Empty;
            }
        }

        /// <summary>
        /// Explores a possible interleaving due to a 'WRITE' operation on the specified shared state.
        /// </summary>
        public static void Write(string state)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                ControlledOperation op = runtime.GetExecutingOperation();
                op.LastAccessedState = state;
                runtime.ScheduleNextOperation(SchedulingPointType.Write, isSuppressible: false);
                op.LastAccessedState = string.Empty;
            }
        }

        /// <summary>
        /// Suppresses interleavings until <see cref="Resume"/> is invoked.
        /// </summary>
        /// <remarks>
        /// This method does not suppress interleavings that happen when an operation is waiting
        /// some other operation to complete, when an operation completes and the scheduler
        /// switches to a new operation, or interleavings from uncontrolled concurrency.
        /// </remarks>
        public static void Suppress()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.SuppressScheduling();
            }
        }

        /// <summary>
        /// Resumes interleavings that were suppressed by invoking <see cref="Suppress"/>.
        /// </summary>
        public static void Resume()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.ResumeScheduling();
            }
        }

        /// <summary>
        /// Move to the next phase (for debugging).
        /// </summary>
        public static void NextPhase(int phase)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.MoveNextPhase(phase);
            }
        }
    }
}
