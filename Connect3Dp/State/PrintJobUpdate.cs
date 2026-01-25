using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace Connect3Dp.State
{
    /// <summary>
    /// Represents a partial update to a machine print job.
    /// Only properties that are explicitly set will be applied to the job.
    /// </summary>
    internal class PrintJobUpdate
    {
        internal string? Name { get; private set; }
        internal string? Stage { get; private set; }
        internal bool StageSet { get; private set; }
        internal int? PercentageComplete { get; private set; }
        internal TimeSpan? RemainingTime { get; private set; }
        internal TimeSpan? TotalTime { get; private set; }
        internal MachineFile? File { get; private set; }
        internal bool FileSet { get; private set; }
        internal MachineFile? Thumbnail { get; private set; }
        internal bool ThumbnailSet { get; private set; }


        public PrintJobUpdate SetName(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            return this;
        }

        public PrintJobUpdate SetFile(MachineFile? machineFile)
        {
            File = machineFile;
            FileSet = true;
            return this;
        }

        public PrintJobUpdate SetThumbnail(MachineFile? machineFile)
        {
            Thumbnail = machineFile;
            ThumbnailSet = true;
            return this;
        }

        public PrintJobUpdate SetStage(string? stage)
        {
            Stage = stage;
            StageSet = true;
            return this;
        }

        public PrintJobUpdate SetPercentageComplete(int percentage)
        {
            if (percentage < 0 || percentage > 100)
                throw new ArgumentException("Percentage must be between 0 and 100", nameof(percentage));

            PercentageComplete = percentage;
            return this;
        }

        public PrintJobUpdate SetRemainingTime(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero)
                throw new ArgumentException("Remaining time cannot be negative", nameof(remaining));

            RemainingTime = remaining;
            return this;
        }

        public PrintJobUpdate SetTotalTime(TimeSpan total)
        {
            if (total < TimeSpan.Zero)
                throw new ArgumentException("Total time cannot be negative", nameof(total));

            TotalTime = total;
            return this;
        }

        public bool TryConstructBase([NotNullWhen(true)] out PrintJob? printJob)
        {
            // Only if we have enough information, do construct the original type.

            if (this.Name != null && this.TotalTime.HasValue && this.RemainingTime.HasValue && this.PercentageComplete.HasValue)
            {
                printJob = new PrintJob
                {
                    Name = this.Name,
                    PercentageComplete = this.PercentageComplete.Value,
                    RemainingTime = this.RemainingTime.Value,
                    TotalTime = this.TotalTime.Value
                };

                printJob.ApplyUpdate(this);
            }
            else
            {
                printJob = null;
            }

            return printJob != null;
        }
    }

    internal partial class PrintJob
    {
        /// <summary>
        /// Applies a partial update to this print job.
        /// Only properties that were explicitly set in the update will be modified.
        /// </summary>
        internal void ApplyUpdate(PrintJobUpdate update)
        {
            if (update == null)
                throw new ArgumentNullException(nameof(update));

            if (update.Name != null)
                Name = update.Name;

            if (update.FileSet)
                File = update.File;

            if (update.ThumbnailSet)
                Thumbnail = update.Thumbnail;

            if (update.StageSet)
                Stage = update.Stage;

            if (update.PercentageComplete.HasValue)
                PercentageComplete = update.PercentageComplete.Value;

            if (update.RemainingTime.HasValue)
                RemainingTime = update.RemainingTime.Value;

            if (update.TotalTime.HasValue)
                TotalTime = update.TotalTime.Value;
        }
    }
}