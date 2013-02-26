﻿using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Deletes old log files
    /// </summary>
    public class DeleteLogFileTask : IScheduledTask
    {
        /// <summary>
        /// Gets or sets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        private IKernel Kernel { get; set; }
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteLogFileTask" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        public DeleteLogFileTask(IKernel kernel, ILogger logger)
        {
            Kernel = kernel;
            Logger = logger;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            var trigger = new DailyTrigger { TimeOfDay = TimeSpan.FromHours(2) }; //2am

            return new[] { trigger };
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return Task.Run(() =>
            {
                // Delete log files more than n days old
                var minDateModified = DateTime.UtcNow.AddDays(-(Kernel.Configuration.LogFileRetentionDays));

                var filesToDelete = new DirectoryInfo(Kernel.ApplicationPaths.LogDirectoryPath).EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
                              .Where(f => f.LastWriteTimeUtc < minDateModified)
                              .ToList();

                var index = 0;

                foreach (var file in filesToDelete)
                {
                    double percent = index;
                    percent /= filesToDelete.Count;

                    progress.Report(100 * percent);

                    cancellationToken.ThrowIfCancellationRequested();

                    File.Delete(file.FullName);

                    index++;
                }

                progress.Report(100);
            });
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Log file cleanup"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return string.Format("Deletes log files that are more than {0} days old.", Kernel.Configuration.LogFileRetentionDays); }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get
            {
                return "Maintenance";
            }
        }
    }
}
