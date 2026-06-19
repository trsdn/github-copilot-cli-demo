using System.Text.Json;
using System.Net.Http.Json;
using InvoiceDropper.Core.Extraction;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace InvoiceDropper_WinUI;

/// <summary>
/// The main content page displayed inside the application window.
/// Add your UI logic, event handlers, and data binding here.
/// </summary>
public sealed partial class MainPage : Page
{
    private readonly string _sampleRoot;
    private readonly LegacyRegexInvoiceExtractionProvider _regexProvider;
    private InvoiceExtractionResult? _lastResult;

    public MainPage()
    {
        InitializeComponent();
        _sampleRoot = FindSampleRoot();
        _regexProvider = new LegacyRegexInvoiceExtractionProvider();
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            SetStatus(InfoBarSeverity.Warning, "Unsupported drop", "Drop a file from File Explorer.");
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        if (items.FirstOrDefault() is StorageFile file)
        {
            await ExtractFileAsync(file);
        }
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private async void LoadSample_Click(object sender, RoutedEventArgs e)
    {
        var samplePath = Path.Combine(_sampleRoot, "contoso-office-supplies.txt");
        await ExtractPathAsync(samplePath);
    }

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastResult is null)
        {
            return;
        }

        var dataPackage = new DataPackage();
        dataPackage.SetText(_lastResult.ToClipboardText());
        Clipboard.SetContent(dataPackage);
        SetStatus(InfoBarSeverity.Success, "Copied", "Summary and normalized JSON were copied to the Windows clipboard.");
        await Task.CompletedTask;
    }

    private async Task ExtractFileAsync(StorageFile file)
    {
        var path = file.Path;
        if (string.IsNullOrWhiteSpace(path))
        {
            using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream.AsStreamForRead());
            var text = await reader.ReadToEndAsync();
            await ExtractContentAsync(file.Name, file.Name, text);
            return;
        }

        await ExtractPathAsync(path);
    }

    private async Task ExtractPathAsync(string path)
    {
        var fileName = Path.GetFileName(path);
        string text;
        try
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            text = extension is ".txt" or ".json" ? await File.ReadAllTextAsync(path) : $"Dropped file: {fileName}. Companion fixture will be used for deterministic demo mode.";
        }
        catch (Exception ex)
        {
            SetStatus(InfoBarSeverity.Error, "Could not read file", ex.Message);
            return;
        }
        await ExtractContentAsync(path, fileName, text);
    }

    private async Task ExtractContentAsync(string sourcePath, string fileName, string text)
    {
        SetStatus(InfoBarSeverity.Informational, "Extracting", $"Processing {fileName}...");
        FileLabel.Text = fileName;

        try
        {
            var request = new InvoiceExtractionRequest(sourcePath, fileName, text, "default");
            _lastResult = await _regexProvider.ExtractAsync(request);
            RenderResult(_lastResult);
            SetStatus(InfoBarSeverity.Success, _lastResult.Provider, $"Extracted invoice {_lastResult.InvoiceNumber} in {_lastResult.Elapsed.TotalMilliseconds:N0} ms.");
        }
        catch (Exception ex)
        {
            SetStatus(InfoBarSeverity.Error, "Extraction failed", ex.Message);
        }
    }

    private void RenderResult(InvoiceExtractionResult result)
    {
        FieldsList.Items.Clear();
        FieldsList.Items.Add($"Invoice: {result.InvoiceNumber}");
        FieldsList.Items.Add($"Vendor: {result.Vendor.Name}");
        FieldsList.Items.Add($"Customer: {result.Customer.Name}");
        FieldsList.Items.Add($"Due: {result.DueDate:yyyy-MM-dd}");
        FieldsList.Items.Add($"PO: {result.PurchaseOrderNumber}");
        FieldsList.Items.Add($"Total: {result.Total:N2} {result.Currency}");
        FieldsList.Items.Add($"Confidence: {result.Confidence:P0}");
        FieldsList.Items.Add($"Provider: {result.Provider}");
        FieldsList.Items.Add($"Model: {result.Model}");
        RawJsonBox.Text = JsonSerializer.Serialize(result, InvoiceExtractionResult.JsonOptions);
        CopyButton.IsEnabled = true;
    }

    private void SetStatus(InfoBarSeverity severity, string title, string message)
    {
        StatusInfo.Severity = severity;
        StatusInfo.Title = title;
        StatusInfo.Message = message;
        StatusInfo.IsOpen = true;
    }

    private static string FindSampleRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "InvoiceDropperDemo.sln")))
        {
            directory = directory.Parent;
        }

        var repoRoot = directory?.FullName ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "sample-data", "invoices");
    }
}
