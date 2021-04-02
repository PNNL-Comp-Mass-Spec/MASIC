using System;

static class modMASICBrowswer
{

    // private CWSpectrumDLL.SpectrumClass objSpectrum;

    public static void Main()
    {
        frmBrowser objBrowserForm;

        try
        {
            // objSpectrum = new CWSpectrumDLL.SpectrumClass();
            // objSpectrum.ShowSpectrum();

            objBrowserForm = new frmBrowser();
            objBrowserForm.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Unable to initialize the CW Spectrum DLL.  Ending program." + Environment.NewLine + ex.Message, "Missing DLL", MessageBoxButtons.Ok, MessageBoxIcons.Exclamation);
        }
    }

    public static void ShowSICSpectrum()
    {
        // objSpectrum.ShowSpectrum();
    }
}
