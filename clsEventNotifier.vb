Public MustInherit Class clsEventNotifier

    Private mLastPercentComplete As Short = 0

#Region "Events"

    ''' <summary>
    ''' Describes an error event
    ''' </summary>
    ''' <param name="source"></param>
    ''' <param name="message"></param>
    ''' <param name="ex">Exception; allowed to be nothing</param>
    ''' <param name="allowInformUser"></param>
    ''' <param name="allowThrowException"></param>
    ''' <param name="eNewErrorCode"></param>
    Public Event ErrorEvent(
      source As String,
      message As String,
      ex As Exception,
      allowInformUser As Boolean,
      allowThrowException As Boolean,
      eNewErrorCode As clsMASIC.eMasicErrorCodes)

    ''' <summary>
    ''' Describes a status message
    ''' </summary>
    ''' <param name="message"></param>
    Public Event MessageEvent(message As String)

    ''' <summary>
    ''' Describes a progress update
    ''' </summary>
    ''' <param name="percentComplete">Value between 0 and 100</param>
    ''' <param name="progressMessage">Progress message</param>
    ''' <remarks>progressMessage can be an empty string if only updating the percent complete</remarks>
    Public Event ProgressUpdate(percentComplete As Short, progressMessage As String)

    ''' <summary>
    ''' Provides information on the number of cache and uncache events in objSpectraCache
    ''' </summary>
    ''' <param name="cacheEventCount"></param>
    ''' <param name="unCacheEventCount"></param>
    Public Event UpdateCacheStatsEvent(cacheEventCount As Integer, unCacheEventCount As Integer)

    ''' <summary>
    ''' Update the code associated with an error
    ''' </summary>
    ''' <param name="eNewErrorCode"></param>
    ''' <param name="leaveExistingErrorCodeUnchanged"></param>
    Public Event UpdateErrorCodeEvent(eNewErrorCode As clsMASIC.eMasicErrorCodes, leaveExistingErrorCodeUnchanged As Boolean)

    ''' <summary>
    ''' Describes a warning event
    ''' </summary>
    ''' <param name="message">Error message</param>
    Public Event WarningEvent(source As String, message As String)

#End Region

    ''' <summary>
    ''' Report an error
    ''' </summary>
    ''' <param name="source"></param>
    ''' <param name="message"></param>
    ''' <param name="ex"></param>
    ''' <param name="allowInformUser"></param>
    ''' <param name="allowThrowException"></param>
    ''' <param name="eNewErrorCode"></param>
    Private Sub OnErrorEvent(
      source As String,
      message As String,
      ex As Exception,
      allowInformUser As Boolean,
      allowThrowException As Boolean,
      eNewErrorCode As clsMASIC.eMasicErrorCodes)
        RaiseEvent ErrorEvent(source, message, ex, allowInformUser, allowThrowException, eNewErrorCode)
    End Sub

    ''' <summary>
    ''' Report a status message
    ''' </summary>
    ''' <param name="message"></param>
    Private Sub OnMessageEvent(message As String)
        RaiseEvent MessageEvent(message)
    End Sub

    ''' <summary>
    ''' Progress udpate
    ''' </summary>
    ''' <param name="percentComplete">Value between 0 and 100</param>
    ''' <param name="progressMessage">Progress message</param>
    Private Sub OnProgressUpdate(percentComplete As Short, progressMessage As String)
        RaiseEvent ProgressUpdate(percentComplete, progressMessage)
    End Sub

    Private Sub OnUpdateCacheStats(cacheEventCount As Integer, unCacheEventCount As Integer)
        RaiseEvent UpdateCacheStatsEvent(cacheEventCount, unCacheEventCount)
    End Sub

    ''' <summary>
    ''' Report a warning
    ''' </summary>
    ''' <param name="source"></param>
    ''' <param name="message"></param>
    Private Sub OnWarningEvent(source As String, message As String)
        RaiseEvent WarningEvent(source, message)
    End Sub

    Private Sub OnUpdateErrorCode(eNewErrorCode As clsMASIC.eMasicErrorCodes, leaveExistingErrorCodeUnchanged As Boolean)
        RaiseEvent UpdateErrorCodeEvent(eNewErrorCode, leaveExistingErrorCodeUnchanged)
    End Sub

    Protected Sub RegisterEvents(oClass As clsEventNotifier)
        AddHandler oClass.MessageEvent, AddressOf MessageEventHandler
        AddHandler oClass.ErrorEvent, AddressOf ErrorEventHandler
        AddHandler oClass.WarningEvent, AddressOf WarningEventHandler
        AddHandler oClass.ProgressUpdate, AddressOf ProgressUpdateHandler
        AddHandler oClass.UpdateCacheStatsEvent, AddressOf UpdatedCacheStatsEventHandler
        AddHandler oClass.UpdateErrorCodeEvent, AddressOf UpdateErrorCodeEventHandler
    End Sub

    Protected Sub ReportMessage(message As String)
        OnMessageEvent(message)
    End Sub

    Protected Sub ReportError(
      source As String,
      message As String)

        ReportError(source, message, Nothing)

    End Sub

    Protected Sub ReportError(
      source As String,
      message As String,
      ex As Exception,
      Optional allowInformUser As Boolean = True,
      Optional allowThrowException As Boolean = True,
      Optional eNewErrorCode As clsMASIC.eMasicErrorCodes = clsMASIC.eMasicErrorCodes.NoError)

        OnErrorEvent(source, message, ex, allowInformUser, allowThrowException, eNewErrorCode)

    End Sub

    Protected Sub ReportWarning(source As String, message As String)
        OnWarningEvent(source, message)
    End Sub

    Protected Sub SetLocalErrorCode(eNewErrorCode As clsMASIC.eMasicErrorCodes, Optional leaveExistingErrorCodeUnchanged As Boolean = False)
        OnUpdateErrorCode(eNewErrorCode, leaveExistingErrorCodeUnchanged)
    End Sub

    Protected Sub UpdateCacheStats(objSpectraCache As clsSpectraCache)
        OnUpdateCacheStats(objSpectraCache.CacheEventCount, objSpectraCache.UnCacheEventCount)
    End Sub

    Protected Sub UpdateProgress(percentComplete As Short)
        OnProgressUpdate(percentComplete, "")
    End Sub

    Protected Sub UpdateProgress(progressMessage As String)
        OnProgressUpdate(mLastPercentComplete, progressMessage)
    End Sub

    Protected Sub UpdateProgress(percentComplete As Short, progressMessage As String)
        mLastPercentComplete = percentComplete
        OnProgressUpdate(percentComplete, progressMessage)
    End Sub


#Region "Event Handlers"

    Private Sub MessageEventHandler(message As String)
        ReportMessage(message)
    End Sub

    Private Sub ErrorEventHandler(
      source As String,
      message As String,
      ex As Exception,
      allowInformUser As Boolean,
      allowThrowException As Boolean,
      eNewErrorCode As clsMASIC.eMasicErrorCodes)
        ReportError(source, message, ex, allowInformUser, allowThrowException, eNewErrorCode)
    End Sub

    Private Sub WarningEventHandler(source As String, message As String)
        ReportWarning(source, message)
    End Sub

    Private Sub ProgressUpdateHandler(percentComplete As Short, progressMessage As String)
        UpdateProgress(percentComplete, progressMessage)
    End Sub

    Private Sub UpdatedCacheStatsEventHandler(cacheEventCount As Integer, unCacheEventCount As Integer)
        OnUpdateCacheStats(cacheEventCount, unCacheEventCount)
    End Sub

    Private Sub UpdateErrorCodeEventHandler(eErrorCode As clsMASIC.eMasicErrorCodes, leaveExistingErrorCodeUnchanged As Boolean)
        SetLocalErrorCode(eErrorCode, leaveExistingErrorCodeUnchanged)
    End Sub
#End Region


End Class
