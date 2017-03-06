; This is an Inno Setup configuration file
; http://www.jrsoftware.org/isinfo.php

#define ApplicationVersion GetFileVersion('..\bin\MASIC.exe')

[CustomMessages]
AppName=MASIC

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.
; Example with multiple lines:
; WelcomeLabel2=Welcome message%n%nAdditional sentence
[Files]
Source: ..\bin\MASIC.exe                      ; DestDir: {app}
Source: ..\bin\MASIC.pdb                      ; DestDir: {app}
Source: ..\bin\alglibnet2.dll                 ; DestDir: {app}
Source: ..\bin\DataFilter.dll                 ; DestDir: {app}
Source: ..\bin\MASICPeakFinder.dll            ; DestDir: {app}
Source: ..\bin\MatrixDecompositionUtility.dll ; DestDir: {app}
Source: ..\bin\MSDataFileReader.dll           ; DestDir: {app}
Source: ..\bin\NetCDFReader.dll               ; DestDir: {app}
Source: ..\bin\PNNLOmics.dll                  ; DestDir: {app}
Source: ..\bin\PRISM.dll                      ; DestDir: {app}
Source: ..\bin\ShFolderBrowser.dll            ; DestDir: {app}
Source: ..\bin\ProgressFormNET.dll            ; DestDir: {app}
Source: ..\bin\SavGolCS.dll                   ; DestDir: {app}
Source: ..\bin\SharedVBNetRoutines.dll        ; DestDir: {app}
Source: ..\bin\SpectraTypeClassifier.dll      ; DestDir: {app}
Source: ..\bin\ThermoRawFileReader.dll        ; DestDir: {app}

Source: ..\MASICBrowser\bin\MASICBrowser.exe               ; DestDir: {app}
Source: ..\MASICBrowser\bin\AxInterop.CWUIControlsLib.dll  ; DestDir: {app}
Source: ..\MASICBrowser\bin\AxInterop.MSComctlLib.dll      ; DestDir: {app}
Source: ..\MASICBrowser\bin\CWSpectrumDLLNET.dll           ; DestDir: {app}
Source: ..\MASICBrowser\bin\Interop.CWUIControlsLib.dll    ; DestDir: {app}
Source: ..\MASICBrowser\bin\Interop.MSComctlLib.dll        ; DestDir: {app}
Source: ..\MASICBrowser\bin\stdole.dll                     ; DestDir: {app}

Source: "C:\Program Files (x86)\National Instruments\Shared\Mesa\mesa.dll"    ; DestDir: {app}
Source: c:\windows\syswow64\cwui.ocx                                          ; DestDir: {app}

Source: ..\Readme.txt                           ; DestDir: {app}
Source: ..\RevisionHistory.txt                  ; DestDir: {app}
Source: Images\delete_16x.ico                   ; DestDir: {app}

Source: ..\bin\MASICParameters.xml              ; DestDir: {app}
Source: ..\bin\QC_Standards_Excerpt.mzXML       ; DestDir: {app}
Source: ..\bin\CustomMZList_AcqTime.txt         ; DestDir: {app}
Source: ..\bin\QC_Standards_Excerpt_SICs.xml    ; DestDir: {app}
Source: ..\bin\CustomMZList.txt                 ; DestDir: {app}

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
AppPublisherURL=http://omics.pnl.gov/software
AppSupportURL=http://omics.pnl.gov/software
AppUpdatesURL=http://omics.pnl.gov/software
DefaultDirName={pf}\MASIC
DefaultGroupName=PAST Toolkit
AppCopyright=© PNNL
;LicenseFile=.\License.rtf
PrivilegesRequired=poweruser
OutputBaseFilename=MASIC_Installer
VersionInfoVersion={#ApplicationVersion}
VersionInfoCompany=PNNL
VersionInfoDescription=MASIC
VersionInfoCopyright=PNNL
DisableFinishedPage=true
ShowLanguageDialog=no
ChangesAssociations=false
EnableDirDoesntExistWarning=false
AlwaysShowDirOnReadyPage=true
UninstallDisplayIcon={app}\delete_16x.ico
ShowTasksTreeLines=true
OutputDir=.\Output

[Registry]
;Root: HKCR; Subkey: MyAppFile; ValueType: string; ValueName: ; ValueDataMyApp File; Flags: uninsdeletekey
;Root: HKCR; Subkey: MyAppSetting\DefaultIcon; ValueType: string; ValueData: {app}\wand.ico,0; Flags: uninsdeletevalue

[UninstallDelete]
Name: {app}; Type: filesandordirs
