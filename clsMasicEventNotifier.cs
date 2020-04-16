using System;
using PRISM;

namespace MASIC
{
    public abstract class clsMasicEventNotifier : EventNotifier
    {
        private short mLastPercentComplete = 0;

        #region // TODO
        /// <summary>
    /// Provides information on the number of cache and uncache events in spectraCache
    /// </summary>
    /// <param name="cacheEventCount"></param>
    /// <param name="unCacheEventCount"></param>
        public event UpdateCacheStatsEventEventHandler UpdateCacheStatsEvent;

        public delegate void UpdateCacheStatsEventEventHandler(int cacheEventCount, int unCacheEventCount);

        /// <summary>
    /// Update the code associated with an error
    /// </summary>
    /// <param name="eNewErrorCode"></param>
        public event UpdateBaseClassErrorCodeEventEventHandler UpdateBaseClassErrorCodeEvent;

        public delegate void UpdateBaseClassErrorCodeEventEventHandler(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes eNewErrorCode);

        /// <summary>
    /// Update the code associated with an error
    /// </summary>
    /// <param name="eNewErrorCode"></param>
    /// <param name="leaveExistingErrorCodeUnchanged"></param>
        public event UpdateErrorCodeEventEventHandler UpdateErrorCodeEvent;

        public delegate void UpdateErrorCodeEventEventHandler(clsMASIC.eMasicErrorCodes eNewErrorCode, bool leaveExistingErrorCodeUnchanged);

        #endregion
        private void OnUpdateCacheStats(int cacheEventCount, int unCacheEventCount)
        {
            UpdateCacheStatsEvent?.Invoke(cacheEventCount, unCacheEventCount);
        }

        private void OnUpdateBaseClassErrorCode(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes eNewErrorCode)
        {
            UpdateBaseClassErrorCodeEvent?.Invoke(eNewErrorCode);
        }

        private void OnUpdateErrorCode(clsMASIC.eMasicErrorCodes eNewErrorCode, bool leaveExistingErrorCodeUnchanged)
        {
            UpdateErrorCodeEvent?.Invoke(eNewErrorCode, leaveExistingErrorCodeUnchanged);
        }

        protected void RegisterEvents(clsMasicEventNotifier oClass)
        {
            base.RegisterEvents(oClass);
            oClass.UpdateCacheStatsEvent += UpdatedCacheStatsEventHandler;
            oClass.UpdateBaseClassErrorCodeEvent += UpdateBaseClassErrorCodeEventHandler;
            oClass.UpdateErrorCodeEvent += UpdateErrorCodeEventHandler;
        }

        protected void ReportMessage(string message)
        {
            OnStatusEvent(message);
        }

        protected void ReportError(string message, clsMASIC.eMasicErrorCodes eNewErrorCode = clsMASIC.eMasicErrorCodes.NoError)
        {
            if (eNewErrorCode != clsMASIC.eMasicErrorCodes.NoError)
            {
                OnUpdateErrorCode(eNewErrorCode, false);
            }

            OnErrorEvent(message);
        }

        protected void ReportError(string message, Exception ex, clsMASIC.eMasicErrorCodes eNewErrorCode = clsMASIC.eMasicErrorCodes.NoError)
        {
            if (eNewErrorCode != clsMASIC.eMasicErrorCodes.NoError)
            {
                OnUpdateErrorCode(eNewErrorCode, false);
            }

            OnErrorEvent(message, ex);
        }

        [Obsolete("Source, allowInformUser, and allowThrowException are no longer supported")]
        protected void ReportError(string source, string message, Exception ex, bool allowInformUser, bool allowThrowException = true, clsMASIC.eMasicErrorCodes eNewErrorCode = clsMASIC.eMasicErrorCodes.NoError)
        {
            ReportError(message, ex, eNewErrorCode);
        }

        protected void ReportWarning(string message)
        {
            OnWarningEvent(message);
        }

        protected void SetBaseClassErrorCode(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes eNewErrorCode)
        {
            OnUpdateBaseClassErrorCode(eNewErrorCode);
        }

        protected void SetLocalErrorCode(clsMASIC.eMasicErrorCodes eNewErrorCode, bool leaveExistingErrorCodeUnchanged = false)
        {
            OnUpdateErrorCode(eNewErrorCode, leaveExistingErrorCodeUnchanged);
        }

        protected void UpdateCacheStats(clsSpectraCache spectraCache)
        {
            OnUpdateCacheStats(spectraCache.CacheEventCount, spectraCache.UnCacheEventCount);
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


        #region // TODO
        private void UpdatedCacheStatsEventHandler(int cacheEventCount, int unCacheEventCount)
        {
            OnUpdateCacheStats(cacheEventCount, unCacheEventCount);
        }

        private void UpdateBaseClassErrorCodeEventHandler(PRISM.FileProcessor.ProcessFilesBase.ProcessFilesErrorCodes eErrorCode)
        {
            SetBaseClassErrorCode(eErrorCode);
        }

        private void UpdateErrorCodeEventHandler(clsMASIC.eMasicErrorCodes eErrorCode, bool leaveExistingErrorCodeUnchanged)
        {
            SetLocalErrorCode(eErrorCode, leaveExistingErrorCodeUnchanged);
        }
        #endregion
    }
}