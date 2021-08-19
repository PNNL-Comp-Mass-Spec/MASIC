using System;
using PRISM;

namespace MASIC
{
    /// <summary>
    /// MASIC event notifier
    /// </summary>
    public abstract class clsMasicEventNotifier : EventNotifier
    {
        // Ignore Spelling: uncache

        private short mLastPercentComplete;

        /// <summary>
        /// Provides information on the number of cache and uncache events in spectraCache
        /// </summary>
        public event UpdateCacheStatsEventEventHandler UpdateCacheStatsEvent;

        /// <summary>
        /// Delegate for the update cache stats event
        /// </summary>
        /// <param name="cacheEventCount"></param>
        /// <param name="unCacheEventCount"></param>
        /// <param name="spectraPoolHitEventCount"></param>
        public delegate void UpdateCacheStatsEventEventHandler(int cacheEventCount, int unCacheEventCount, int spectraPoolHitEventCount);

        /// <summary>
        /// Update the code associated with an error
        /// </summary>
        public event UpdateBaseClassErrorCodeEventEventHandler UpdateBaseClassErrorCodeEvent;

        /// <summary>
        /// Delegate for the base class error code event
        /// </summary>
        /// <param name="eNewErrorCode"></param>
        public delegate void UpdateBaseClassErrorCodeEventEventHandler(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes eNewErrorCode);

        /// <summary>
        /// Update the code associated with an error
        /// </summary>
        public event UpdateErrorCodeEventEventHandler UpdateErrorCodeEvent;

        /// <summary>
        /// Delete fro the update error code event
        /// </summary>
        /// <param name="eNewErrorCode"></param>
        /// <param name="leaveExistingErrorCodeUnchanged"></param>
        public delegate void UpdateErrorCodeEventEventHandler(clsMASIC.MasicErrorCodes eNewErrorCode, bool leaveExistingErrorCodeUnchanged);

        private void OnUpdateCacheStats(int cacheEventCount, int unCacheEventCount, int spectraPoolHitEventCount)
        {
            UpdateCacheStatsEvent?.Invoke(cacheEventCount, unCacheEventCount, spectraPoolHitEventCount);
        }

        private void OnUpdateBaseClassErrorCode(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes eNewErrorCode)
        {
            UpdateBaseClassErrorCodeEvent?.Invoke(eNewErrorCode);
        }

        private void OnUpdateErrorCode(clsMASIC.MasicErrorCodes eNewErrorCode, bool leaveExistingErrorCodeUnchanged)
        {
            UpdateErrorCodeEvent?.Invoke(eNewErrorCode, leaveExistingErrorCodeUnchanged);
        }

        /// <summary>
        /// Register events
        /// </summary>
        /// <param name="sourceClass"></param>
        protected void RegisterEvents(clsMasicEventNotifier sourceClass)
        {
            base.RegisterEvents(sourceClass);

            sourceClass.UpdateCacheStatsEvent += UpdatedCacheStatsEventHandler;
            sourceClass.UpdateBaseClassErrorCodeEvent += UpdateBaseClassErrorCodeEventHandler;
            sourceClass.UpdateErrorCodeEvent += UpdateErrorCodeEventHandler;
        }

        /// <summary>
        /// Report a status message
        /// </summary>
        /// <param name="message"></param>
        protected void ReportMessage(string message)
        {
            OnStatusEvent(message);
        }

        /// <summary>
        /// Report an error message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="eNewErrorCode"></param>
        protected void ReportError(string message,
                                   clsMASIC.MasicErrorCodes eNewErrorCode = clsMASIC.MasicErrorCodes.NoError)
        {
            if (eNewErrorCode != clsMASIC.MasicErrorCodes.NoError)
            {
                OnUpdateErrorCode(eNewErrorCode, false);
            }

            OnErrorEvent(message);
        }

        /// <summary>
        /// Report an error message, including an exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        /// <param name="eNewErrorCode"></param>
        protected void ReportError(string message,
                                   Exception ex,
                                   clsMASIC.MasicErrorCodes eNewErrorCode = clsMASIC.MasicErrorCodes.NoError)
        {
            if (eNewErrorCode != clsMASIC.MasicErrorCodes.NoError)
            {
                OnUpdateErrorCode(eNewErrorCode, false);
            }

            OnErrorEvent(message, ex);
        }

        /// <summary>
        /// Report a warning message
        /// </summary>
        /// <param name="message"></param>
        protected void ReportWarning(string message)
        {
            OnWarningEvent(message);
        }

        /// <summary>
        /// Set the base class error code
        /// </summary>
        /// <param name="eNewErrorCode"></param>
        protected void SetBaseClassErrorCode(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes eNewErrorCode)
        {
            OnUpdateBaseClassErrorCode(eNewErrorCode);
        }

        /// <summary>
        /// Set a local error code
        /// </summary>
        /// <param name="eNewErrorCode"></param>
        /// <param name="leaveExistingErrorCodeUnchanged"></param>
        protected void SetLocalErrorCode(clsMASIC.MasicErrorCodes eNewErrorCode, bool leaveExistingErrorCodeUnchanged = false)
        {
            OnUpdateErrorCode(eNewErrorCode, leaveExistingErrorCodeUnchanged);
        }

        /// <summary>
        /// Update cache stats
        /// </summary>
        /// <param name="spectraCache"></param>
        protected void UpdateCacheStats(clsSpectraCache spectraCache)
        {
            OnUpdateCacheStats(spectraCache.CacheEventCount, spectraCache.UnCacheEventCount, spectraCache.SpectraPoolHitEventCount);
        }

        /// <summary>
        /// Update the progress of a given subtask
        /// </summary>
        /// <param name="percentComplete"></param>
        protected void UpdateProgress(short percentComplete)
        {
            OnProgressUpdate(string.Empty, percentComplete);
        }

        /// <summary>
        /// Update progress
        /// </summary>
        /// <param name="progressMessage"></param>
        protected void UpdateProgress(string progressMessage)
        {
            OnProgressUpdate(progressMessage, mLastPercentComplete);
        }

        /// <summary>
        /// Update progress
        /// </summary>
        /// <param name="percentComplete"></param>
        /// <param name="progressMessage"></param>
        protected void UpdateProgress(short percentComplete, string progressMessage)
        {
            mLastPercentComplete = percentComplete;
            OnProgressUpdate(progressMessage, percentComplete);
        }

        private void UpdatedCacheStatsEventHandler(int cacheEventCount, int unCacheEventCount, int spectraPoolHitEventCount)
        {
            OnUpdateCacheStats(cacheEventCount, unCacheEventCount, spectraPoolHitEventCount);
        }

        private void UpdateBaseClassErrorCodeEventHandler(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes eErrorCode)
        {
            SetBaseClassErrorCode(eErrorCode);
        }

        private void UpdateErrorCodeEventHandler(clsMASIC.MasicErrorCodes eErrorCode, bool leaveExistingErrorCodeUnchanged)
        {
            SetLocalErrorCode(eErrorCode, leaveExistingErrorCodeUnchanged);
        }
    }
}
