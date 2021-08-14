using System;
using PRISM;

namespace MASIC
{
    public abstract class clsMasicEventNotifier : EventNotifier
    {
        // Ignore Spelling: uncache

        private short mLastPercentComplete;

        /// <summary>
        /// Provides information on the number of cache and uncache events in spectraCache
        /// </summary>
        public event UpdateCacheStatsEventEventHandler UpdateCacheStatsEvent;

        public delegate void UpdateCacheStatsEventEventHandler(int cacheEventCount, int unCacheEventCount, int spectraPoolHitEventCount);

        /// <summary>
        /// Update the code associated with an error
        /// </summary>
        public event UpdateBaseClassErrorCodeEventEventHandler UpdateBaseClassErrorCodeEvent;

        public delegate void UpdateBaseClassErrorCodeEventEventHandler(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes eNewErrorCode);

        /// <summary>
        /// Update the code associated with an error
        /// </summary>
        public event UpdateErrorCodeEventEventHandler UpdateErrorCodeEvent;

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

        protected void RegisterEvents(clsMasicEventNotifier sourceClass)
        {
            base.RegisterEvents(sourceClass);

            sourceClass.UpdateCacheStatsEvent += UpdatedCacheStatsEventHandler;
            sourceClass.UpdateBaseClassErrorCodeEvent += UpdateBaseClassErrorCodeEventHandler;
            sourceClass.UpdateErrorCodeEvent += UpdateErrorCodeEventHandler;
        }

        protected void ReportMessage(string message)
        {
            OnStatusEvent(message);
        }

        protected void ReportError(string message,
                                   clsMASIC.MasicErrorCodes eNewErrorCode = clsMASIC.MasicErrorCodes.NoError)
        {
            if (eNewErrorCode != clsMASIC.MasicErrorCodes.NoError)
            {
                OnUpdateErrorCode(eNewErrorCode, false);
            }

            OnErrorEvent(message);
        }

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

        protected void ReportWarning(string message)
        {
            OnWarningEvent(message);
        }

        protected void SetBaseClassErrorCode(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes eNewErrorCode)
        {
            OnUpdateBaseClassErrorCode(eNewErrorCode);
        }

        protected void SetLocalErrorCode(clsMASIC.MasicErrorCodes eNewErrorCode, bool leaveExistingErrorCodeUnchanged = false)
        {
            OnUpdateErrorCode(eNewErrorCode, leaveExistingErrorCodeUnchanged);
        }

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

        protected void UpdateProgress(string progressMessage)
        {
            OnProgressUpdate(progressMessage, mLastPercentComplete);
        }

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
