using System.IO;
using System.Runtime.InteropServices;

namespace Örnek.Services;

public sealed class EingangsrechnungenScanService
{
    private const int ScannerDeviceType = 1;
    private const int IntentColor = 1;
    private const int BiasMaximizeQuality = 0;
    private const string JpegFormatId = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";

    public string AcquireScanToTempFile()
    {
        Type? dialogType = Type.GetTypeFromProgID("WIA.CommonDialog");
        if (dialogType == null)
            throw new InvalidOperationException("WIA ist auf diesem System nicht verfügbar.");

        object? image = null;
        object? dialog = null;

        try
        {
            dialog = Activator.CreateInstance(dialogType);
            if (dialog == null)
                throw new InvalidOperationException("Der Windows-Scandialog konnte nicht initialisiert werden.");

            image = dialogType.InvokeMember(
                "ShowAcquireImage",
                System.Reflection.BindingFlags.InvokeMethod,
                null,
                dialog,
                new object[] { ScannerDeviceType, IntentColor, BiasMaximizeQuality, JpegFormatId, true, true, false });

            if (image == null)
                throw new OperationCanceledException("Der Scanvorgang wurde abgebrochen.");

            var tempPath = Path.Combine(Path.GetTempPath(), $"scan_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.jpg");
            var imageType = image.GetType();
            imageType.InvokeMember(
                "SaveFile",
                System.Reflection.BindingFlags.InvokeMethod,
                null,
                image,
                new object[] { tempPath });

            if (!File.Exists(tempPath))
                throw new IOException("Die gescannte Datei konnte nicht gespeichert werden.");

            return tempPath;
        }
        catch (COMException ex) when ((uint)ex.HResult == 0x80210015)
        {
            throw new InvalidOperationException("Es wurde kein WIA-kompatibler Scanner gefunden.", ex);
        }
        catch (COMException ex) when ((uint)ex.HResult == 0x80210064)
        {
            throw new InvalidOperationException("Der Scanner ist derzeit nicht verfügbar oder wird von Windows blockiert.", ex);
        }
        finally
        {
            if (image != null && Marshal.IsComObject(image))
                Marshal.FinalReleaseComObject(image);

            if (dialog != null && Marshal.IsComObject(dialog))
                Marshal.FinalReleaseComObject(dialog);
        }
    }
}
