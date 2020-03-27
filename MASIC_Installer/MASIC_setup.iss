; This is an Inno Setup configuration file
; https://jrsoftware.org/isinfo.php

#define ApplicationVersion GetFileVersion('..\bin\MASIC.exe')

[CustomMessages]
AppName=MASIC

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.
; Example with multiple lines:
; WelcomeLabel2=Welcome message%n%nAdditional sentence

[Files]
Source: ..\bin\MASIC.exe                       ; DestDir: {app}
Source: ..\bin\MASIC.pdb                       ; DestDir: {app}
Source: ..\bin\Console\Debug\MASIC_Console.exe ; DestDir: {app}
Source: ..\bin\Console\Debug\MASIC_Console.pdb ; DestDir: {app}
Source: ..\bin\DataFilter.dll                  ; DestDir: {app}
Source: ..\bin\InterDetect.dll                 ; DestDir: {app}
Source: ..\bin\MASICPeakFinder.dll             ; DestDir: {app}
Source: ..\bin\MatrixDecompositionUtility.dll  ; DestDir: {app}
Source: ..\bin\MSDataFileReader.dll            ; DestDir: {app}
Source: ..\bin\NetCDFReader.dll                ; DestDir: {app}
Source: ..\bin\PRISM.dll                       ; DestDir: {app}
Source: ..\bin\PRISMWin.dll                    ; DestDir: {app}
Source: ..\bin\ProgressFormNET.dll             ; DestDir: {app}
Source: ..\bin\PSI_Interface.dll               ; DestDir: {app}
Source: ..\bin\ShFolderBrowser.dll             ; DestDir: {app}
Source: ..\bin\SpectraTypeClassifier.dll       ; DestDir: {app}
Source: ..\bin\ThermoFisher.CommonCore.BackgroundSubtraction.dll    ; DestDir: {app}
Source: ..\bin\ThermoFisher.CommonCore.Data.dll                     ; DestDir: {app}
Source: ..\bin\ThermoFisher.CommonCore.MassPrecisionEstimator.dll   ; DestDir: {app}
Source: ..\bin\ThermoFisher.CommonCore.RawFileReader.dll            ; DestDir: {app}
Source: ..\bin\ThermoRawFileReader.dll                              ; DestDir: {app}

Source: ..\MASICBrowser\bin\MASICBrowser.exe                   ; DestDir: {app}
Source: ..\MASICBrowser\bin\OxyDataPlotter.dll                 ; DestDir: {app}
Source: ..\MASICBrowser\bin\OxyPlot.dll                        ; DestDir: {app}
Source: ..\MASICBrowser\bin\OxyPlot.WindowsForms.dll           ; DestDir: {app}
Source: ..\MASICBrowser\bin\Svg.dll                            ; DestDir: {app}

Source: ..\Readme.md                                           ; DestDir: {app}
Source: ..\RevisionHistory.txt                                 ; DestDir: {app}
Source: Images\delete_16x.ico                                  ; DestDir: {app}

Source: ..\Docs\MASICParameters.xml                            ; DestDir: {app}
Source: ..\Docs\LTQ_Example\Default_2008-08-22.xml             ; DestDir: {app}\LTQ_Example
Source: ..\Docs\LTQ_Example\QC_Standards_Excerpt.mzXML         ; DestDir: {app}\LTQ_Example
Source: ..\Docs\LTQ_Example\QC_Standards_Excerpt_SICs.xml      ; DestDir: {app}\LTQ_Example
Source: ..\Docs\LTQ_Example\QC_Standards_Excerpt_ScanStats.txt ; DestDir: {app}\LTQ_Example
Source: ..\Docs\LTQ_Example\QC_Standards_Excerpt_SICstats.txt  ; DestDir: {app}\LTQ_Example

Source: ..\Docs\QExactive_Example\LTQ-FT_10ppm_2014-08-06.xml            ; DestDir: {app}\QExactive_Example
Source: ..\Docs\QExactive_Example\QC_Shew_18_02_Excerpt.mzXML            ; DestDir: {app}\QExactive_Example
Source: ..\Docs\QExactive_Example\QC_Shew_18_02_Excerpt_ScanStats.txt    ; DestDir: {app}\QExactive_Example
Source: ..\Docs\QExactive_Example\QC_Shew_18_02_Excerpt_SICs.xml         ; DestDir: {app}\QExactive_Example
Source: ..\Docs\QExactive_Example\QC_Shew_18_02_Excerpt_SICstats.txt     ; DestDir: {app}\QExactive_Example

Source: ..\docs\Orbitrap_Example\Example_Orbitrap_Data_SICs.xml          ; DestDir: {app}\Orbitrap_Example
Source: ..\docs\Orbitrap_Example\Example_Orbitrap_Data_SICstats.txt      ; DestDir: {app}\Orbitrap_Example

Source: ..\Docs\CustomMZList_AcqTime.txt                       ; DestDir: {app}
Source: ..\Docs\CustomMZList.txt                               ; DestDir: {app}

Source: ..\Lib\netcdf.dll                                      ; DestDir: {app}
Source: ..\Lib\RawFileReaderLicense.doc                        ; DestDir: {app}

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
