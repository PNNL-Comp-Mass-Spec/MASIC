Imports MASIC.clsMASIC
Imports PRISM

Public MustInherit Class clsMasicEventNotifier
    Inherits EventNotifier

    Private mLastPercentComplete As Short = 0

#Region "Events"

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
    Public Event UpdateBaseClassErrorCodeEvent(eNewErrorCode As ProcessFilesErrorCodes)

    ''' <summary>
    ''' Update the code associated with an error
    ''' </summary>
    ''' <param name="eNewErrorCode"></param>
    ''' <param name="leaveExistingErrorCodeUnchanged"></param>
    Public Event UpdateErrorCodeEvent(eNewErrorCode As eMasicErrorCodes, leaveExistingErrorCodeUnchanged As Boolean)

#End Region

    Private Sub OnUpdateCacheStats(cacheEventCount As Integer, unCacheEventCount As Integer)
        RaiseEvent UpdateCacheStatsEvent(cacheEventCount, unCacheEventCount)
    End Sub

    Private Sub OnUpdateBaseClassErrorCode(eNewErrorCode As ProcessFilesErrorCodes)
        RaiseEvent UpdateBaseClassErrorCodeEvent(eNewErrorCode)
    End Sub

    Private Sub OnUpdateErrorCode(eNewErrorCode As eMasicErrorCodes, leaveExistingErrorCodeUnchanged As Boolean)
        RaiseEvent UpdateErrorCodeEvent(eNewErrorCode, leaveExistingErrorCodeUnchanged)
    End Sub

    Protected Overloads Sub RegisterEvents(oClass As clsMasicEventNotifier)
        MyBase.RegisterEvents(oClass)

        AddHandler oClass.UpdateCacheStatsEvent, AddressOf UpdatedCacheStatsEventHandler
        AddHandler oClass.UpdateBaseClassErrorCodeEvent, AddressOf UpdateBaseClassErrorCodeEventHandler
        AddHandler oClass.UpdateErrorCodeEvent, AddressOf UpdateErrorCodeEventHandler
    End Sub

    Protected Sub ReportMessage(message As String)
        OnStatusEvent(message)
    End Sub

    Protected Sub ReportError(message As String,
                              Optional eNewErrorCode As eMasicErrorCodes = eMasicErrorCodes.NoError)

        If eNewErrorCode <> eMasicErrorCodes.NoError Then
            OnUpdateErrorCode(eNewErrorCode, False)
        End If

        OnErrorEvent(message)
    End Sub

    Protected Sub ReportError(message As String,
                              ex As Exception,
                              Optional eNewErrorCode As eMasicErrorCodes = eMasicErrorCodes.NoError)

        If eNewErrorCode <> eMasicErrorCodes.NoError Then
            OnUpdateErrorCode(eNewErrorCode, False)
        End If

        OnErrorEvent(message, ex)
    End Sub

    <Obsolete("Source, allowInformUser, and allowThrowException are no longer supported")>
    Protected Sub ReportError(
      source As String,
      message As String,
      ex As Exception,
      allowInformUser As Boolean,
      Optional allowThrowException As Boolean = True,
      Optional eNewErrorCode As eMasicErrorCodes = eMasicErrorCodes.NoError)

        ReportError(message, ex, eNewErrorCode)
    End Sub

    Protected Sub ReportWarning(message As String)
        OnWarningEvent(message)
    End Sub

    Protected Sub SetBaseClassErrorCode(eNewErrorCode As ProcessFilesErrorCodes)
        OnUpdateBaseClassErrorCode(eNewErrorCode)
    End Sub

    Protected Sub SetLocalErrorCode(eNewErrorCode As eMasicErrorCodes, Optional leaveExistingErrorCodeUnchanged As Boolean = False)
        OnUpdateErrorCode(eNewErrorCode, leaveExistingErrorCodeUnchanged)
    End Sub

    Protected Sub UpdateCacheStats(objSpectraCache As clsSpectraCache)
        OnUpdateCacheStats(objSpectraCache.CacheEventCount, objSpectraCache.UnCacheEventCount)
    End Sub

    ''' <summary>
    ''' Update the progress of a given subtask
    ''' </summary>
    ''' <param name="percentComplete"></param>
    Protected Sub UpdateProgress(percentComplete As Short)
        OnProgressUpdate("", percentComplete)
    End Sub

    Protected Sub UpdateProgress(progressMessage As String)
        OnProgressUpdate(progressMessage, mLastPercentComplete)
    End Sub

    Protected Sub UpdateProgress(percentComplete As Short, progressMessage As String)
        mLastPercentComplete = percentComplete
        OnProgressUpdate(progressMessage, percentComplete)
    End Sub


#Region "Event Handlers"

    Private Sub UpdatedCacheStatsEventHandler(cacheEventCount As Integer, unCacheEventCount As Integer)
        OnUpdateCacheStats(cacheEventCount, unCacheEventCount)
    End Sub

    Private Sub UpdateBaseClassErrorCodeEventHandler(eErrorCode As ProcessFilesErrorCodes)
        SetBaseClassErrorCode(eErrorCode)
    End Sub

    Private Sub UpdateErrorCodeEventHandler(eErrorCode As eMasicErrorCodes, leaveExistingErrorCodeUnchanged As Boolean)
        SetLocalErrorCode(eErrorCode, leaveExistingErrorCodeUnchanged)
    End Sub
#End Region

End Class
