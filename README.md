MASIC
=====

MASIC generates selected ion chromatograms (SICs) for all of the parent ions chosen for fragmentation in an LC-MS/MS analysis. The SICs are generated using the LC-MS data, then each SIC is processed using a peak finding algorithm to characterize the chromatographic peaks, providing peak statistics including elution time of the peak apex, peak area, and peak signal/noise.

MASIC can read Finnigan .Raw files, mzXML files, mzData files or .cdf/.mgf combo files. Results are outputted both as flat files (.txt) and in an XML file that can be read using the accompanying graphical results browser. The browser provides a fast, graphical method for browsing the SICs identified by MASIC, allowing the user to sort and filter the SIC list as desired. MASIC has been in routine use in the PRISM pipeline since 2004 and has been used to process over 40,000 datasets. MASIC has also been updated to provide basic support for MRM datasets.

The application note describing MASIC is Monroe ME et. al. 2008. Computational Biology and Chemistry 32(3):215-217 
