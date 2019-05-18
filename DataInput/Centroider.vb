Imports System.Runtime.InteropServices

Public Class Centroider

    ''' <summary>
    ''' Centroid a profile mode spectrum using the ThermoFisher.CommonCore.Data centroiding logic
    ''' </summary>
    ''' <param name="scanInfo"></param>
    ''' <param name="masses"></param>
    ''' <param name="intensities"></param>
    ''' <param name="centroidedPrecursorIonsMz"></param>
    ''' <param name="centroidedPrecursorIonsIntensity"></param>
    ''' <returns></returns>
    Public Function CentroidData(scanInfo As clsScanInfo,
        masses As Double(),
        intensities As Double(),
        <Out> ByRef centroidedPrecursorIonsMz As Double(),
        <Out> ByRef centroidedPrecursorIonsIntensity As Double()) As Boolean

        Const massResolution As Double = 10000

        Return CentroidData(scanInfo, masses, intensities, massResolution, centroidedPrecursorIonsMz, centroidedPrecursorIonsIntensity)

    End Function

    ''' <summary>
    ''' Centroid a profile mode spectrum using the ThermoFisher.CommonCore.Data centroiding logic
    ''' </summary>
    ''' <param name="scanInfo"></param>
    ''' <param name="masses"></param>
    ''' <param name="intensities"></param>
    ''' <param name="centroidedPrecursorIonsMz"></param>
    ''' <param name="centroidedPrecursorIonsIntensity"></param>
    ''' <returns></returns>
    Public Function CentroidData(
       scanInfo As clsScanInfo,
       masses As Double(),
       intensities As Double(),
       massResolution As Double,
       <Out> ByRef centroidedPrecursorIonsMz As Double(),
       <Out> ByRef centroidedPrecursorIonsIntensity As Double()) As Boolean

        Try

            Dim segmentedScan = ThermoFisher.CommonCore.Data.Business.SegmentedScan.FromMassesAndIntensities(masses, intensities)

            Dim scanStats = New ThermoFisher.CommonCore.Data.Business.ScanStatistics With {
                    .PacketType = 2 + (2 << 16),
                    .ScanNumber = scanInfo.ScanNumber,
                    .StartTime = scanInfo.ScanTime,
                    .BasePeakIntensity = scanInfo.BasePeakIonIntensity,
                    .BasePeakMass = scanInfo.BasePeakIonMZ,
                    .LowMass = masses.First(),
                    .HighMass = masses.Last(),
                    .TIC = scanInfo.TotalIonIntensity
                }

            Dim scan = New ThermoFisher.CommonCore.Data.Business.Scan With {
                    .MassResolution = massResolution,
                    .ScanType = scanInfo.ScanTypeName,
                    .ToleranceUnit = ThermoFisher.CommonCore.Data.Business.ToleranceMode.Ppm,     ' Options are None, Amu, Mmu, Ppm
                    .ScanStatistics = scanStats,
                    .SegmentedScan = segmentedScan
                }

            Dim centroidScan = ThermoFisher.CommonCore.Data.Business.Scan.ToCentroid(scan)

            centroidedPrecursorIonsMz = centroidScan.PreferredMasses
            centroidedPrecursorIonsIntensity = centroidScan.PreferredIntensities

            Return True

        Catch ex As Exception
            ReDim centroidedPrecursorIonsMz(0)
            ReDim centroidedPrecursorIonsIntensity(0)
            Return False
        End Try

    End Function

    Public Function EstimateResolution(
      mass As Double,
      defaultMassTolerance As Double,
      isOrbitrapData As Boolean) As Double

        Dim toleranceFactor = GetDefaultToleranceFactor(isOrbitrapData)
        Dim toleranceUnit = ThermoFisher.CommonCore.Data.Business.ToleranceMode.Amu

        Return EstimateResolution(mass, toleranceFactor, defaultMassTolerance, isOrbitrapData, toleranceUnit)
    End Function

    ''' <summary>Calculates the mass tolerance for the profile peak</summary>
    ''' <param name="mass">current mass tolerance value</param>
    ''' <param name="toleranceFactor">tolerance factor</param>
    ''' <param name="defaultMassTolerance">previous mass tolerance value</param>
    ''' <param name="isOrbitrapData">True if processing LTQ-FT Or Orbitrap data</param>
    ''' <param name="toleranceUnit">tolerance unit</param>
    ''' <returns>The calculated mass resolution for the profile peak</returns>
    Public Function EstimateResolution(
      mass As Double,
      toleranceFactor As Double,
      defaultMassTolerance As Double,
      isOrbitrapData As Boolean,
      toleranceUnit As ThermoFisher.CommonCore.Data.Business.ToleranceMode) As Double

        Dim massResolution As Double

        If toleranceUnit = ThermoFisher.CommonCore.Data.Business.ToleranceMode.Ppm OrElse
           toleranceUnit = ThermoFisher.CommonCore.Data.Business.ToleranceMode.Mmu Then

            Dim massToleranceDa As Double
            If toleranceUnit = ThermoFisher.CommonCore.Data.Business.ToleranceMode.Ppm Then
                massToleranceDa = mass * 0.000001 * defaultMassTolerance
            Else
                massToleranceDa = mass * 0.001 * defaultMassTolerance
            End If

            Dim deltaM As Double = mass * mass * toleranceFactor

            If deltaM > massToleranceDa Then
                massResolution = deltaM
            Else
                massResolution = massToleranceDa
            End If

        Else
            If isOrbitrapData Then
                massResolution = mass * Math.Sqrt(mass) * toleranceFactor
            Else
                massResolution = mass * mass * toleranceFactor
            End If

        End If

        Return massResolution

    End Function

    Private Function GetDefaultToleranceFactor(isOrbitrapData As Boolean) As Double

        If isOrbitrapData Then
            Return 0.0000001
        Else
            Return 0.000002
        End If

    End Function

End Class
