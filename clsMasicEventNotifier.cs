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
        /// <param name="newErrorCode"></param>
        public delegate void UpdateBaseClassErrorCodeEventEventHandler(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes newErrorCode);

        /// <summary>
        /// Update the code associated with an error
        /// </summary>
        public event UpdateErrorCodeEventEventHandler UpdateErrorCodeEvent;

        /// <summary>
        /// Delete fro the update error code event
        /// </summary>
        /// <param name="newErrorCode"></param>
        /// <param name="leaveExistingErrorCodeUnchanged"></param>
        public delegate void UpdateErrorCodeEventEventHandler(clsMASIC.MasicErrorCodes newErrorCode, bool leaveExistingErrorCodeUnchanged);

        private void OnUpdateCacheStats(int cacheEventCount, int unCacheEventCount, int spectraPoolHitEventCount)
        {
            UpdateCacheStatsEvent?.Invoke(cacheEventCount, unCacheEventCount, spectraPoolHitEventCount);
        }

        private void OnUpdateBaseClassErrorCode(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes newErrorCode)
        {
            UpdateBaseClassErrorCodeEvent?.Invoke(newErrorCode);
        }

        private void OnUpdateErrorCode(clsMASIC.MasicErrorCodes newErrorCode, bool leaveExistingErrorCodeUnchanged)
        {
            UpdateErrorCodeEvent?.Invoke(newErrorCode, leaveExistingErrorCodeUnchanged);
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
        /// <param name="newErrorCode"></param>
        protected void ReportError(string message,
                                   clsMASIC.MasicErrorCodes newErrorCode = clsMASIC.MasicErrorCodes.NoError)
        {
            if (newErrorCode != clsMASIC.MasicErrorCodes.NoError)
            {
                OnUpdateErrorCode(newErrorCode, false);
            }

            OnErrorEvent(message);
        }

        /// <summary>
        /// Report an error message, including an exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        /// <param name="newErrorCode"></param>
        protected void ReportError(string message,
                                   Exception ex,
                                   clsMASIC.MasicErrorCodes newErrorCode = clsMASIC.MasicErrorCodes.NoError)
        {
            if (newErrorCode != clsMASIC.MasicErrorCodes.NoError)
            {
                OnUpdateErrorCode(newErrorCode, false);
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
        /// <param name="newErrorCode"></param>
        protected void SetBaseClassErrorCode(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes newErrorCode)
        {
            OnUpdateBaseClassErrorCode(newErrorCode);
        }

        /// <summary>
        /// Set a local error code
        /// </summary>
        /// <param name="newErrorCode"></param>
        /// <param name="leaveExistingErrorCodeUnchanged"></param>
        protected void SetLocalErrorCode(clsMASIC.MasicErrorCodes newErrorCode, bool leaveExistingErrorCodeUnchanged = false)
        {
            OnUpdateErrorCode(newErrorCode, leaveExistingErrorCodeUnchanged);
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

        private void UpdateBaseClassErrorCodeEventHandler(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes errorCode)
        {
            SetBaseClassErrorCode(errorCode);
        }

        private void UpdateErrorCodeEventHandler(clsMASIC.MasicErrorCodes errorCode, bool leaveExistingErrorCodeUnchanged)
        {
            SetLocalErrorCode(errorCode, leaveExistingErrorCodeUnchanged);
        }
    }
}
