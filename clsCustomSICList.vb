Imports PRISM

Public Class clsCustomSICList
    Inherits clsEventNotifier

#Region "Constants and Enums"

    Public Const CUSTOM_SIC_TYPE_ABSOLUTE As String = "Absolute"
    Public Const CUSTOM_SIC_TYPE_RELATIVE As String = "Relative"
    Public Const CUSTOM_SIC_TYPE_ACQUISITION_TIME As String = "AcquisitionTime"


    Public Enum eCustomSICScanTypeConstants
        Absolute = 0            ' Absolute scan number
        Relative = 1            ' Relative scan number (ranging from 0 to 1, where 0 is the first scan and 1 is the last scan)
        AcquisitionTime = 2     ' The scan's acquisition time (aka elution time if using liquid chromatography)
        Undefined = 3
    End Enum

#End Region

#Region "Properties"

    Public Property CustomSICListFileName As String
        Get
            If mCustomSICListFileName Is Nothing Then Return String.Empty
            Return mCustomSICListFileName
        End Get
        Set(value As String)
            If value Is Nothing Then
                mCustomSICListFileName = String.Empty
            Else
                mCustomSICListFileName = value.Trim()
            End If
        End Set
    End Property

    Public Property ScanToleranceType As eCustomSICScanTypeConstants

    ''' <summary>
    ''' This is an Integer if ScanToleranceType = eCustomSICScanTypeConstants.Absolute
    ''' It is a Single if ScanToleranceType = .Relative or ScanToleranceType = .AcquisitionTime
    ''' Set to 0 to search the entire file for the given mass
    ''' </summary>
    Public Property ScanOrAcqTimeTolerance As Single

    Public ReadOnly Property CustomMZSearchValues As List(Of clsCustomMZSearchSpec)

    ''' <summary>
    ''' When True, then will only search for the m/z values listed in the custom m/z list
    ''' </summary>
    Public Property LimitSearchToCustomMZList As Boolean

    Public Property RawTextMZList As String
    Public Property RawTextMZToleranceDaList As String
    Public Property RawTextScanOrAcqTimeCenterList As String
    Public Property RawTextScanOrAcqTimeToleranceList As String

#End Region

#Region "Classwide variables"
    Private mCustomSICListFileName As String
#End Region
    ''' <summary>
    ''' Constructor
    ''' </summary>
    Public Sub New()
        CustomMZSearchValues = New List(Of clsCustomMZSearchSpec)
    End Sub

    Public Sub AddCustomSICValues(
      scanList As clsScanList,
      dblDefaultSICTolerance As Double,
      blnSICToleranceIsPPM As Boolean,
      sngDefaultScanOrAcqTimeTolerance As Single)

        Dim intScanOrAcqTimeSumCount = 0
        Dim sngScanOrAcqTimeSumForAveraging As Single = 0

        Try
            If CustomMZSearchValues.Count = 0 Then
                Exit Sub
            End If

            ReDim Preserve scanList.ParentIons(scanList.ParentIonInfoCount + CustomMZSearchValues.Count - 1)

            Dim scanNumScanConverter = New clsScanNumScanTimeConversion()
            RegisterEvents(scanNumScanConverter)

            For Each customMzSearchValue In CustomMZSearchValues

                ' Add a new parent ion entry to .ParentIons() for this custom MZ value
                Dim currentParentIon = New clsParentIonInfo(customMzSearchValue.MZ)

                If customMzSearchValue.ScanOrAcqTimeCenter < Single.Epsilon Then
                    ' Set the SurveyScanIndex to the center of the analysis
                    currentParentIon.SurveyScanIndex = scanNumScanConverter.FindNearestSurveyScanIndex(
                        scanList, 0.5, eCustomSICScanTypeConstants.Relative)
                Else
                    currentParentIon.SurveyScanIndex = scanNumScanConverter.FindNearestSurveyScanIndex(
                        scanList, customMzSearchValue.ScanOrAcqTimeCenter, ScanToleranceType)
                End If

                ' Find the next MS2 scan that occurs after the survey scan (parent scan)
                Dim surveyScanNumberAbsolute = 0
                If currentParentIon.SurveyScanIndex < scanList.SurveyScans.Count Then
                    surveyScanNumberAbsolute = scanList.SurveyScans(currentParentIon.SurveyScanIndex).ScanNumber + 1
                End If

                If scanList.MasterScanOrderCount = 0 Then
                    ReDim currentParentIon.FragScanIndices(0)
                    currentParentIon.FragScanIndices(0) = 0
                Else
                    Dim fragScanIndexMatch = clsBinarySearch.BinarySearchFindNearest(scanList.MasterScanNumList, surveyScanNumberAbsolute, scanList.MasterScanOrderCount, clsBinarySearch.eMissingDataModeConstants.ReturnClosestPoint)

                    While fragScanIndexMatch < scanList.MasterScanOrderCount AndAlso scanList.MasterScanOrder(fragScanIndexMatch).ScanType = clsScanList.eScanTypeConstants.SurveyScan
                        fragScanIndexMatch += 1
                    End While

                    If fragScanIndexMatch = scanList.MasterScanOrderCount Then
                        ' Did not find the next frag scan; find the previous frag scan
                        fragScanIndexMatch -= 1
                        While fragScanIndexMatch > 0 AndAlso scanList.MasterScanOrder(fragScanIndexMatch).ScanType = clsScanList.eScanTypeConstants.SurveyScan
                            fragScanIndexMatch -= 1
                        End While
                        If fragScanIndexMatch < 0 Then fragScanIndexMatch = 0
                    End If

                    ' This is a custom SIC-based parent ion
                    ' Prior to August 2014, we set .FragScanIndices(0) = 0, which made it appear that the fragmentation scan was the first MS2 spectrum in the dataset for all custom SICs
                    ' This caused undesirable display results in MASIC browser, so we now set it to the next MS2 scan that occurs after the survey scan (parent scan)
                    ReDim currentParentIon.FragScanIndices(0)
                    If scanList.MasterScanOrder(fragScanIndexMatch).ScanType = clsScanList.eScanTypeConstants.FragScan Then
                        currentParentIon.FragScanIndices(0) = scanList.MasterScanOrder(fragScanIndexMatch).ScanIndexPointer
                    Else
                        currentParentIon.FragScanIndices(0) = 0
                    End If
                End If

                currentParentIon.FragScanIndexCount = 1
                currentParentIon.CustomSICPeak = True
                currentParentIon.CustomSICPeakComment = customMzSearchValue.Comment
                currentParentIon.CustomSICPeakMZToleranceDa = customMzSearchValue.MZToleranceDa
                currentParentIon.CustomSICPeakScanOrAcqTimeTolerance = customMzSearchValue.ScanOrAcqTimeTolerance

                If currentParentIon.CustomSICPeakMZToleranceDa < Double.Epsilon Then
                    If blnSICToleranceIsPPM Then
                        currentParentIon.CustomSICPeakMZToleranceDa = clsUtilities.PPMToMass(dblDefaultSICTolerance, currentParentIon.MZ)
                    Else
                        currentParentIon.CustomSICPeakMZToleranceDa = dblDefaultSICTolerance
                    End If
                End If

                If currentParentIon.CustomSICPeakScanOrAcqTimeTolerance < Single.Epsilon Then
                    currentParentIon.CustomSICPeakScanOrAcqTimeTolerance = sngDefaultScanOrAcqTimeTolerance
                Else
                    sngScanOrAcqTimeSumForAveraging += currentParentIon.CustomSICPeakScanOrAcqTimeTolerance
                    intScanOrAcqTimeSumCount += 1
                End If


                If currentParentIon.SurveyScanIndex < scanList.SurveyScans.Count Then
                    currentParentIon.OptimalPeakApexScanNumber =
                        scanList.SurveyScans(currentParentIon.SurveyScanIndex).ScanNumber
                Else
                    currentParentIon.OptimalPeakApexScanNumber = 1
                End If

                currentParentIon.PeakApexOverrideParentIonIndex = -1

                scanList.ParentIons(scanList.ParentIonInfoCount) = currentParentIon

                scanList.ParentIonInfoCount += 1


            Next

            If intScanOrAcqTimeSumCount = CustomMZSearchValues.Count AndAlso sngScanOrAcqTimeSumForAveraging > 0 Then
                ' All of the entries had a custom scan or acq time tolerance defined
                ' Update mScanOrAcqTimeTolerance to the average of the values
                ScanOrAcqTimeTolerance = CSng(Math.Round(sngScanOrAcqTimeSumForAveraging / intScanOrAcqTimeSumCount, 4))
            End If

        Catch ex As Exception
            OnErrorEvent("Error in AddCustomSICValues", ex)
        End Try

    End Sub

    Public Sub AddMzSearchTarget(mzSearchSpec As clsCustomMZSearchSpec)

        If CustomMZSearchValues.Count > 0 Then
            RawTextMZList &= ","c
            RawTextMZToleranceDaList &= ","c
            RawTextScanOrAcqTimeCenterList &= ","c
            RawTextScanOrAcqTimeToleranceList &= ","c
        End If

        RawTextMZList &= mzSearchSpec.MZ.ToString()
        RawTextMZToleranceDaList &= mzSearchSpec.MZToleranceDa.ToString()
        RawTextScanOrAcqTimeCenterList &= mzSearchSpec.ScanOrAcqTimeCenter.ToString()
        RawTextScanOrAcqTimeToleranceList &= mzSearchSpec.ScanOrAcqTimeTolerance.ToString()

        CustomMZSearchValues.Add(mzSearchSpec)

    End Sub

    Public Function ParseCustomSICList(
      strMZList As String,
      strMZToleranceDaList As String,
      strScanCenterList As String,
      strScanToleranceList As String,
      strScanCommentList As String) As Boolean

        Dim strDelimList = New Char() {","c, ControlChars.Tab}

        ' Trim any trailing tab characters
        strMZList = strMZList.TrimEnd(ControlChars.Tab)
        strMZToleranceDaList = strMZToleranceDaList.TrimEnd(ControlChars.Tab)
        strScanCenterList = strScanCenterList.TrimEnd(ControlChars.Tab)
        strScanCommentList = strScanCommentList.TrimEnd(strDelimList)

        Dim lstMZs = strMZList.Split(strDelimList).ToList()
        Dim lstMZToleranceDa = strMZToleranceDaList.Split(strDelimList).ToList()
        Dim lstScanCenters = strScanCenterList.Split(strDelimList).ToList()
        Dim lstScanTolerances = strScanToleranceList.Split(strDelimList).ToList()
        Dim lstScanComments As List(Of String)

        If strScanCommentList.Length > 0 Then
            lstScanComments = strScanCommentList.Split(strDelimList).ToList()
        Else
            lstScanComments = New List(Of String)
        End If

        ResetMzSearchValues()

        If lstMZs.Count <= 0 Then
            ' Nothing to parse; return true
            Return True
        End If

        For intIndex = 0 To lstMZs.Count - 1

            Dim targetMz As Double

            If Not Double.TryParse(lstMZs(intIndex), targetMz) Then
                Continue For
            End If

            Dim mzSearchSpec = New clsCustomMZSearchSpec(targetMz) With {
                .MZToleranceDa = 0,
                .ScanOrAcqTimeCenter = 0,                 ' Set to 0 to indicate that the entire file should be searched
                .ScanOrAcqTimeTolerance = 0
            }

            If lstScanCenters.Count > intIndex Then
                If clsUtilities.IsNumber(lstScanCenters(intIndex)) Then
                    If ScanToleranceType = eCustomSICScanTypeConstants.Absolute Then
                        mzSearchSpec.ScanOrAcqTimeCenter = CInt(lstScanCenters(intIndex))
                    Else
                        ' Includes .Relative and .AcquisitionTime
                        mzSearchSpec.ScanOrAcqTimeCenter = CSng(lstScanCenters(intIndex))
                    End If
                End If
            End If

            If lstScanTolerances.Count > intIndex Then
                If clsUtilities.IsNumber(lstScanTolerances(intIndex)) Then
                    If ScanToleranceType = eCustomSICScanTypeConstants.Absolute Then
                        mzSearchSpec.ScanOrAcqTimeTolerance = CInt(lstScanTolerances(intIndex))
                    Else
                        ' Includes .Relative and .AcquisitionTime
                        mzSearchSpec.ScanOrAcqTimeTolerance = CSng(lstScanTolerances(intIndex))
                    End If
                End If
            End If

            If lstMZToleranceDa.Count > intIndex Then
                If clsUtilities.IsNumber(lstMZToleranceDa(intIndex)) Then
                    mzSearchSpec.MZToleranceDa = CDbl(lstMZToleranceDa(intIndex))
                End If
            End If

            If lstScanComments.Count > intIndex Then
                mzSearchSpec.Comment = lstScanComments(intIndex)
            Else
                mzSearchSpec.Comment = String.Empty
            End If

            AddMzSearchTarget(mzSearchSpec)

        Next

        Return True

    End Function

    Public Sub Reset()

        ScanToleranceType = eCustomSICScanTypeConstants.Absolute
        ScanOrAcqTimeTolerance = 1000

        ResetMzSearchValues()

    End Sub

    Public Sub ResetMzSearchValues()

        CustomMZSearchValues.Clear()

        RawTextMZList = String.Empty
        RawTextMZToleranceDaList = String.Empty
        RawTextScanOrAcqTimeCenterList = String.Empty
        RawTextScanOrAcqTimeToleranceList = String.Empty

    End Sub

    <Obsolete("Use SetCustomSICListValues that takes List(Of clsCustomMZSearchSpec)")>
    Public Function SetCustomSICListValues(
      eScanType As eCustomSICScanTypeConstants,
      dblMZToleranceDa As Double,
      sngScanOrAcqTimeTolerance As Single,
      dblMZList() As Double,
      dblMZToleranceList() As Double,
      sngScanOrAcqTimeCenterList() As Single,
      sngScanOrAcqTimeToleranceList() As Single,
      strScanComments() As String) As Boolean

        ' Returns True if sucess

        Dim intIndex As Integer

        If dblMZToleranceList.Length > 0 AndAlso dblMZToleranceList.Length <> dblMZList.Length Then
            ' Invalid Custom SIC comment list; number of entries doesn't match
            Return False
        ElseIf sngScanOrAcqTimeCenterList.Length > 0 AndAlso sngScanOrAcqTimeCenterList.Length <> dblMZList.Length Then
            ' Invalid Custom SIC scan center list; number of entries doesn't match
            Return False
        ElseIf sngScanOrAcqTimeToleranceList.Length > 0 AndAlso sngScanOrAcqTimeToleranceList.Length <> dblMZList.Length Then
            ' Invalid Custom SIC scan center list; number of entries doesn't match
            Return False
        ElseIf strScanComments.Length > 0 AndAlso strScanComments.Length <> dblMZList.Length Then
            ' Invalid Custom SIC comment list; number of entries doesn't match
            Return False
        End If

        ResetMzSearchValues()

        ScanToleranceType = eScanType

        ' This value is used if sngScanOrAcqTimeToleranceList is blank or for any entries in sngScanOrAcqTimeToleranceList() that are zero
        ScanOrAcqTimeTolerance = sngScanOrAcqTimeTolerance

        If dblMZList.Length = 0 Then
            Return True
        End If

        For intIndex = 0 To dblMZList.Length - 1

            Dim mzSearchSpec = New clsCustomMZSearchSpec(dblMZList(intIndex))
            With mzSearchSpec

                If dblMZToleranceList.Length > intIndex AndAlso dblMZToleranceList(intIndex) > 0 Then
                    .MZToleranceDa = dblMZToleranceList(intIndex)
                Else
                    .MZToleranceDa = dblMZToleranceDa
                End If

                If sngScanOrAcqTimeCenterList.Length > intIndex Then
                    .ScanOrAcqTimeCenter = sngScanOrAcqTimeCenterList(intIndex)
                Else
                    .ScanOrAcqTimeCenter = 0         ' Set to 0 to indicate that the entire file should be searched
                End If

                If sngScanOrAcqTimeToleranceList.Length > intIndex AndAlso sngScanOrAcqTimeToleranceList(intIndex) > 0 Then
                    .ScanOrAcqTimeTolerance = sngScanOrAcqTimeToleranceList(intIndex)
                Else
                    .ScanOrAcqTimeTolerance = sngScanOrAcqTimeTolerance
                End If

                If strScanComments.Length > 0 AndAlso strScanComments.Length > intIndex Then
                    .Comment = strScanComments(intIndex)
                Else
                    .Comment = String.Empty
                End If
            End With

            AddMzSearchTarget(mzSearchSpec)

        Next

        ValidateCustomSICList()

        Return True

    End Function

    Public Function SetCustomSICListValues(
      eScanType As eCustomSICScanTypeConstants,
      sngScanOrAcqTimeTolerance As Single,
      mzSearchSpecs As List(Of clsCustomMZSearchSpec)) As Boolean

        ' Returns True if success

        ResetMzSearchValues()

        ScanToleranceType = eScanType

        ' This value is used if sngScanOrAcqTimeToleranceList is blank or for any entries in sngScanOrAcqTimeToleranceList() that are zero
        ScanOrAcqTimeTolerance = sngScanOrAcqTimeTolerance

        If mzSearchSpecs.Count = 0 Then
            Return True
        End If

        For Each mzSearchSpec In mzSearchSpecs
            AddMzSearchTarget(mzSearchSpec)
        Next

        ValidateCustomSICList()

        Return True

    End Function

    Public Sub ValidateCustomSICList()

        If CustomMZSearchValues Is Nothing OrElse
           CustomMZSearchValues.Count = 0 Then
            Return
        End If

        ' Check whether all of the values are between 0 and 1
        ' If they are, then auto-switch .ScanToleranceType to "Relative"

        Dim intCountBetweenZeroAndOne = 0
        Dim intCountOverOne = 0

        For Each customMzValue In CustomMZSearchValues
            If customMzValue.ScanOrAcqTimeCenter > 1 Then
                intCountOverOne += 1
            Else
                intCountBetweenZeroAndOne += 1
            End If
        Next

        If intCountOverOne = 0 And intCountBetweenZeroAndOne > 0 Then
            If ScanToleranceType = eCustomSICScanTypeConstants.Absolute Then
                ' No values were greater than 1 but at least one value is between 0 and 1
                ' Change the ScanToleranceType mode from Absolute to Relative
                ScanToleranceType = eCustomSICScanTypeConstants.Relative
            End If
        End If

        If intCountOverOne > 0 And intCountBetweenZeroAndOne = 0 Then
            If ScanToleranceType = eCustomSICScanTypeConstants.Relative Then
                ' The ScanOrAcqTimeCenter values cannot be relative
                ' Change the ScanToleranceType mode from Relative to Absolute
                ScanToleranceType = eCustomSICScanTypeConstants.Absolute
            End If
        End If

    End Sub

    Public Overrides Function ToString() As String
        If CustomMZSearchValues Is Nothing OrElse CustomMZSearchValues.Count = 0 Then
            Return "0 custom m/z search values"
        Else
            Return CustomMZSearchValues.Count & " custom m/z search values"
        End If
    End Function

End Class
