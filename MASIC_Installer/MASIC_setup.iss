; This is an Inno Setup configuration file
; https://jrsoftware.org/isinfo.php

#define ApplicationVersion GetFileVersion('..\bin\Release\MASIC.exe')

[CustomMessages]
AppName=MASIC

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.
; Example with multiple lines:
; WelcomeLabel2=Welcome message%n%nAdditional sentence

[Files]
Source: ..\bin\Release\MASIC.exe                                                 ; DestDir: {app}
Source: ..\bin\Release\MASIC.pdb                                                 ; DestDir: {app}
Source: ..\bin\Console\Release\MASIC_Console.exe                                 ; DestDir: {app}
Source: ..\bin\Console\Release\MASIC_Console.pdb                                 ; DestDir: {app}
Source: ..\bin\Release\Azure.Core.dll                                            ; DestDir: {app}
Source: ..\bin\Release\Azure.Identity.dll                                        ; DestDir: {app}
Source: ..\bin\Release\DataFilter.dll                                            ; DestDir: {app}
Source: ..\bin\Release\InterDetect.dll                                           ; DestDir: {app}
Source: ..\bin\Release\MASICPeakFinder.dll                                       ; DestDir: {app}
Source: ..\bin\Release\MathNet.Numerics.dll                                      ; DestDir: {app}
Source: ..\bin\Release\MatrixDecompositionUtility.dll                            ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Bcl.AsyncInterfaces.dll                         ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Bcl.HashCode.dll                                ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Data.SqlClient.dll                              ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Data.SqlClient.SNI.arm64.dll                    ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Data.SqlClient.SNI.x64.dll                      ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Data.SqlClient.SNI.x86.dll                      ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Extensions.DependencyInjection.Abstractions.dll ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Extensions.Logging.Abstractions.dll             ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Identity.Client.dll                             ; DestDir: {app}
Source: ..\bin\Release\Microsoft.Identity.Client.Extensions.Msal.dll             ; DestDir: {app}
Source: ..\bin\Release\Microsoft.IdentityModel.Abstractions.dll                  ; DestDir: {app}
Source: ..\bin\Release\Microsoft.IdentityModel.JsonWebTokens.dll                 ; DestDir: {app}
Source: ..\bin\Release\Microsoft.IdentityModel.Logging.dll                       ; DestDir: {app}
Source: ..\bin\Release\Microsoft.IdentityModel.Protocols.dll                     ; DestDir: {app}
Source: ..\bin\Release\Microsoft.IdentityModel.Protocols.OpenIdConnect.dll       ; DestDir: {app}
Source: ..\bin\Release\Microsoft.IdentityModel.Tokens.dll                        ; DestDir: {app}
Source: ..\bin\Release\MSDataFileReader.dll                                      ; DestDir: {app}
Source: ..\bin\Release\netcdf.dll                                                ; DestDir: {app}
Source: ..\bin\Release\NetCDFReader.dll                                          ; DestDir: {app}
Source: ..\bin\Release\Npgsql.dll                                                ; DestDir: {app}
Source: ..\bin\Release\OxyPlot.dll                                               ; DestDir: {app}
Source: ..\bin\Release\OxyPlot.Wpf.dll                                           ; DestDir: {app}
Source: ..\bin\Release\OxyPlot.Wpf.Shared.dll                                    ; DestDir: {app}
Source: ..\bin\Release\PRISM.dll                                                 ; DestDir: {app}
Source: ..\bin\Release\PRISMDatabaseUtils.dll                                    ; DestDir: {app}
Source: ..\bin\Release\PRISMWin.dll                                              ; DestDir: {app}
Source: ..\bin\Release\ProgressFormNET.dll                                       ; DestDir: {app}
Source: ..\bin\Release\PSI_Interface.dll                                         ; DestDir: {app}
Source: ..\bin\Release\ShFolderBrowser.dll                                       ; DestDir: {app}
Source: ..\bin\Release\SpectraTypeClassifier.dll                                 ; DestDir: {app}
Source: ..\bin\Release\System.Buffers.dll                                        ; DestDir: {app}
Source: ..\bin\Release\System.ClientModel.dll                                    ; DestDir: {app}
Source: ..\bin\Release\System.Collections.Immutable.dll                          ; DestDir: {app}
Source: ..\bin\Release\System.Configuration.ConfigurationManager.dll             ; DestDir: {app}
Source: ..\bin\Release\System.Diagnostics.DiagnosticSource.dll                   ; DestDir: {app}
Source: ..\bin\Release\System.IdentityModel.Tokens.Jwt.dll                       ; DestDir: {app}
Source: ..\bin\Release\System.IO.FileSystem.AccessControl.dll                    ; DestDir: {app}
Source: ..\bin\Release\System.Memory.Data.dll                                    ; DestDir: {app}
Source: ..\bin\Release\System.Memory.dll                                         ; DestDir: {app}
Source: ..\bin\Release\System.Numerics.Vectors.dll                               ; DestDir: {app}
Source: ..\bin\Release\System.Runtime.CompilerServices.Unsafe.dll                ; DestDir: {app}
Source: ..\bin\Release\System.Security.AccessControl.dll                         ; DestDir: {app}
Source: ..\bin\Release\System.Security.Cryptography.ProtectedData.dll            ; DestDir: {app}
Source: ..\bin\Release\System.Security.Permissions.dll                           ; DestDir: {app}
Source: ..\bin\Release\System.Security.Principal.Windows.dll                     ; DestDir: {app}
Source: ..\bin\Release\System.Text.Encodings.Web.dll                             ; DestDir: {app}
Source: ..\bin\Release\System.Text.Json.dll                                      ; DestDir: {app}
Source: ..\bin\Release\System.Threading.Channels.dll                             ; DestDir: {app}
Source: ..\bin\Release\System.Threading.Tasks.Extensions.dll                     ; DestDir: {app}
Source: ..\bin\Release\System.ValueTuple.dll                                     ; DestDir: {app}

Source: ..\bin\Release\ThermoFisher.CommonCore.BackgroundSubtraction.dll         ; DestDir: {app}
Source: ..\bin\Release\ThermoFisher.CommonCore.Data.dll                          ; DestDir: {app}
Source: ..\bin\Release\ThermoFisher.CommonCore.MassPrecisionEstimator.dll        ; DestDir: {app}
Source: ..\bin\Release\ThermoFisher.CommonCore.RawFileReader.dll                 ; DestDir: {app}
Source: ..\bin\Release\ThermoRawFileReader.dll                                   ; DestDir: {app}

Source: ..\MASICBrowser\bin\Release\MASICBrowser.exe                             ; DestDir: {app}
Source: ..\MASICBrowser\bin\Release\C5.dll                                       ; DestDir: {app}
Source: ..\MASICBrowser\bin\Release\ExCSS.dll                                    ; DestDir: {app}
Source: ..\MASICBrowser\bin\Release\OxyDataPlotter.dll                           ; DestDir: {app}
Source: ..\MASICBrowser\bin\Release\OxyPlot.WindowsForms.dll                     ; DestDir: {app}
Source: ..\MASICBrowser\bin\Release\Svg.dll                                      ; DestDir: {app}
Source: ..\MASICBrowser\bin\Release\System.CodeDom.dll                           ; DestDir: {app}
Source: ..\MASICBrowser\bin\Release\System.Data.Odbc.dll                         ; DestDir: {app}
Source: ..\MASICBrowser\bin\Release\System.Data.SqlClient.dll                    ; DestDir: {app}
Source: ..\MASICBrowser\bin\Release\System.Diagnostics.PerformanceCounter.dll    ; DestDir: {app}

Source: ..\Readme.md                                                             ; DestDir: {app}
Source: ..\RevisionHistory.txt                                                   ; DestDir: {app}

Source: ..\Python\MASIC_Plotter.py                                               ; DestDir: {app}
Source: ..\Python\Python_Setup.txt                                               ; DestDir: {app}

Source: Images\delete_16x.ico                                                    ; DestDir: {app}

Source: ..\ExampleData\MASICParameters.xml                                             ; DestDir: {app}
Source: ..\ExampleData\LTQ_Example\Default_2008-08-22.xml                              ; DestDir: {app}\LTQ_Example
Source: ..\ExampleData\LTQ_Example\index.html                                          ; DestDir: {app}\LTQ_Example
Source: ..\ExampleData\LTQ_Example\QC_Standards_Excerpt.mzXML                          ; DestDir: {app}\LTQ_Example
Source: ..\ExampleData\LTQ_Example\QC_Standards_Excerpt_SICs.xml                       ; DestDir: {app}\LTQ_Example
Source: ..\ExampleData\LTQ_Example\QC_Standards_Excerpt_ScanStats.txt                  ; DestDir: {app}\LTQ_Example
Source: ..\ExampleData\LTQ_Example\QC_Standards_Excerpt_SICstats.txt                   ; DestDir: {app}\LTQ_Example
Source: ..\ExampleData\LTQ_Example\QC_Standards_Excerpt_DatasetInfo.xml                ; DestDir: {app}\LTQ_Example
Source: ..\ExampleData\LTQ_Example\QC_Standards_Excerpt_PeakAreaHistogram.png          ; DestDir: {app}\LTQ_Example
Source: ..\ExampleData\LTQ_Example\QC_Standards_Excerpt_PeakWidthHistogram.png         ; DestDir: {app}\LTQ_Example

Source: ..\ExampleData\QExactive_Example\index.html                                    ; DestDir: {app}\QExactive_Example
Source: ..\ExampleData\QExactive_Example\LTQ-FT_10ppm_2014-08-06.xml                   ; DestDir: {app}\QExactive_Example
Source: ..\ExampleData\QExactive_Example\QC_Shew_18_02_Excerpt.mzXML                   ; DestDir: {app}\QExactive_Example
Source: ..\ExampleData\QExactive_Example\QC_Shew_18_02_Excerpt_ScanStats.txt           ; DestDir: {app}\QExactive_Example
Source: ..\ExampleData\QExactive_Example\QC_Shew_18_02_Excerpt_SICs.xml                ; DestDir: {app}\QExactive_Example
Source: ..\ExampleData\QExactive_Example\QC_Shew_18_02_Excerpt_SICstats.txt            ; DestDir: {app}\QExactive_Example
Source: ..\ExampleData\QExactive_Example\QC_Shew_18_02_Excerpt_DatasetInfo.xml         ; DestDir: {app}\QExactive_Example
Source: ..\ExampleData\QExactive_Example\QC_Shew_18_02_Excerpt_MS_scans.csv            ; DestDir: {app}\QExactive_Example
Source: ..\ExampleData\QExactive_Example\QC_Shew_18_02_Excerpt_MSMS_scans.csv          ; DestDir: {app}\QExactive_Example
Source: ..\ExampleData\QExactive_Example\QC_Shew_18_02_Excerpt_PeakAreaHistogram.png   ; DestDir: {app}\QExactive_Example
Source: ..\ExampleData\QExactive_Example\QC_Shew_18_02_Excerpt_PeakWidthHistogram.png  ; DestDir: {app}\QExactive_Example

Source: ..\ExampleData\Orbitrap_Example\index.html                                     ; DestDir: {app}\Orbitrap_Example
Source: ..\ExampleData\Orbitrap_Example\Example_Orbitrap_Data_SICs.xml                 ; DestDir: {app}\Orbitrap_Example
Source: ..\ExampleData\Orbitrap_Example\Example_Orbitrap_Data_SICstats.txt             ; DestDir: {app}\Orbitrap_Example
Source: ..\ExampleData\Orbitrap_Example\Example_Orbitrap_Data_DatasetInfo.xml          ; DestDir: {app}\Orbitrap_Example                                  
Source: ..\ExampleData\Orbitrap_Example\Example_Orbitrap_Data_MS_scans.csv             ; DestDir: {app}\Orbitrap_Example
Source: ..\ExampleData\Orbitrap_Example\Example_Orbitrap_Data_MSMS_scans.csv           ; DestDir: {app}\Orbitrap_Example
Source: ..\ExampleData\Orbitrap_Example\Example_Orbitrap_Data_PeakAreaHistogram.png    ; DestDir: {app}\Orbitrap_Example
Source: ..\ExampleData\Orbitrap_Example\Example_Orbitrap_Data_PeakWidthHistogram.png   ; DestDir: {app}\Orbitrap_Example
Source: ..\ExampleData\Orbitrap_Example\Example_Orbitrap_Data_ScanStats.txt            ; DestDir: {app}\Orbitrap_Example
Source: ..\ExampleData\Orbitrap_Example\LTQ-FT_10ppm_2014-08-06.xml                    ; DestDir: {app}\Orbitrap_Example

Source: ..\ExampleData\Parameter_Files\ITRAQ_LTQ-FT_10ppm_ReporterTol0.015Da_2014-08-06.xml     ; DestDir: {app}\Parameter_Files
Source: ..\ExampleData\Parameter_Files\ITRAQ8_LTQ-FT_10ppm_ReporterTol0.015Da_2014-08-06.xml    ; DestDir: {app}\Parameter_Files
Source: ..\ExampleData\Parameter_Files\LTQ-FT_10ppm_2014-08-06.xml                              ; DestDir: {app}\Parameter_Files
Source: ..\ExampleData\Parameter_Files\TMT10_LTQ-FT_10ppm_ReporterTol0.003Da_2014-08-06.xml     ; DestDir: {app}\Parameter_Files
Source: ..\ExampleData\Parameter_Files\TMT11_LTQ-FT_10ppm_ReporterTol0.003Da_2017-03-17.xml     ; DestDir: {app}\Parameter_Files
Source: ..\ExampleData\Parameter_Files\TMT11_LTQ-FT_10ppm_ReporterTol0.003Da_SaveUncorrectedIntensities_2018-06-28.xml     ; DestDir: {app}\Parameter_Files
Source: ..\ExampleData\Parameter_Files\TMT16_10ppm_ReporterTol0.003Da_2019-10-07.xml            ; DestDir: {app}\Parameter_Files
Source: ..\ExampleData\Parameter_Files\TMT6_LTQ-FT_10ppm_ReporterTol0.015Da_2014-08-06.xml      ; DestDir: {app}\Parameter_Files

Source: ..\ExampleData\TMT_Example\index.html                                                   ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\ProcessData.bat                                              ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14.mzML                                      ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_DatasetInfo.xml                           ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_MS_scans.csv                              ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_MSMS_scans.csv                            ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_PeakAreaHistogram.png                     ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_PeakWidthHistogram.png                    ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_RepIonObsRate.png                         ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_RepIonObsRate.txt                         ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_RepIonObsRateHighAbundance.png            ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_RepIonStats.png                           ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_RepIonStats.txt                           ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_RepIonStatsHighAbundance.png              ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_ReporterIons.txt                          ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_ScanStats.txt                             ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_SICs.xml                                  ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT_QCPool_23Jun14_SICstats.txt                              ; DestDir: {app}\TMT_Example
Source: ..\ExampleData\TMT_Example\TMT10_LTQ-FT_10ppm_ReporterTol0.003Da_2014-08-06.xml         ; DestDir: {app}\TMT_Example

Source: ..\ExampleData\CustomMZList_AcqTime.txt                                 ; DestDir: {app}
Source: ..\ExampleData\CustomMZList.txt                                         ; DestDir: {app}

Source: ..\Lib\netcdf.dll                                                       ; DestDir: {app}
Source: ..\RawFileReaderLicense.doc                                             ; DestDir: {app}

[Dirs]
Name: {commonappdata}\MASIC; Flags: uninsalwaysuninstall

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked
; Name: quicklaunchicon; Description: {cm:CreateQuickLaunchIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Icons]
; Name: {commondesktop}\MASIC; Filename: {app}\MASIC.exe; Tasks: desktopicon; Comment: MASIC
Name: {group}\MASIC; Filename: {app}\MASIC.exe; Comment: MASIC
Name: {group}\MASIC Browser; Filename: {app}\MASICBrowser.exe; Comment: MASIC

[Setup]
AppName=MASIC
AppVersion={#ApplicationVersion}
;AppVerName=MASIC
AppID=MASICId
AppPublisher=Pacific Northwest National Laboratory
AppPublisherURL=https://omics.pnl.gov/software
AppSupportURL=https://omics.pnl.gov/software
AppUpdatesURL=https://github.com/PNNL-Comp-Mass-Spec/MASIC
ArchitecturesAllowed=x64 x86
ArchitecturesInstallIn64BitMode=x64
DefaultDirName={autopf}\MASIC
DefaultGroupName=PAST Toolkit
AppCopyright=© PNNL
;LicenseFile=.\License.rtf
PrivilegesRequired=admin
OutputBaseFilename=MASIC_Installer
VersionInfoVersion={#ApplicationVersion}
VersionInfoCompany=PNNL
VersionInfoDescription=MASIC
VersionInfoCopyright=PNNL
DisableFinishedPage=yes
DisableWelcomePage=no
ShowLanguageDialog=no
ChangesAssociations=no
WizardStyle=modern
EnableDirDoesntExistWarning=no
AlwaysShowDirOnReadyPage=yes
UninstallDisplayIcon={app}\delete_16x.ico
ShowTasksTreeLines=yes
OutputDir=.\Output

[Registry]
;Root: HKCR; Subkey: MyAppFile; ValueType: string; ValueName: ; ValueDataMyApp File; Flags: uninsdeletekey
;Root: HKCR; Subkey: MyAppSetting\DefaultIcon; ValueType: string; ValueData: {app}\wand.ico,0; Flags: uninsdeletevalue

[UninstallDelete]
Name: {app}; Type: filesandordirs
